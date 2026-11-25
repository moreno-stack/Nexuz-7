using UnityEngine;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.Player;
using Nexus7.UI;
using Nexus7.Controls;

namespace Nexus7.Combat
{
    /// <summary>
    /// Armas del jugador: lee la entrada, pregunta al WeaponModel si puede disparar y lanza proyectiles del pool.
    ///   Clic izquierdo  disparar (mantener con el rifle)     Clic derecho  apuntar (zoom y menos dispersión)
    ///   R               recargar                             Q                cambiar de arma (en ciclo)
    /// Mando (tipo Xbox): RT disparar · LT apuntar · cruceta ↓ recargar · cruceta ←/→ arma anterior/siguiente.
    /// Los ejes del mando están en el Input Manager (GamepadAxes); si faltan, el mando simplemente no dispara.
    /// El punto de mira es el centro de la cámara (rayo hasta el alcance); el proyectil sale del cañón hacia ese punto
    /// con la dispersión del arma. Cada disparo hace ruido (los robots lo oyen y van a investigar).
    /// No dispara con la entrada bloqueada (hackeos, consolas, cinemáticas, reaparición), con un menú abierto
    /// (cursor libre: el primer clic solo lo captura) ni durante las cinemáticas.
    ///
    /// Colocar en: el GameObject raíz "Player" (lo añade CombatInstaller si falta).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerWeapon : MonoBehaviour
    {
        public static PlayerWeapon Instance { get; private set; }
        /// <summary>Con un arma en la mano el clic izquierdo es para disparar (DeviceQuickbar deja de usarlo).</summary>
        public static bool ClaimsMouse => Instance != null && Instance.isActiveAndEnabled;

        [Header("Controles")]
        [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private KeyCode nextWeaponKey = KeyCode.Q;

        [Header("Apuntar")]
        [SerializeField] private float aimFov = 52f;
        [SerializeField] private float aimSpeed = 8f;

        [Header("Penalización al caer")]
        [Tooltip("Fracción de la munición de reserva que se pierde al caer (la rebaja el director si vas mal).")]
        [SerializeField, Range(0f, 1f)] private float respawnAmmoLoss = 0.25f;

        public ArsenalModel Arsenal { get; private set; }
        public bool IsAiming { get; private set; }
        /// <summary>Dispersión actual en grados (la mira del HUD se abre con ella).</summary>
        public float CurrentSpread { get; private set; }
        public float LastShotTime { get; private set; } = -999f;

        /// <summary>Multiplicador del ruido de los disparos (lo ajusta el director; 1 = normal).</summary>
        public static float NoiseMultiplier = 1f;
        /// <summary>Multiplicador de la pérdida de munición al caer (el director la rebaja si el jugador lo pasa mal).</summary>
        public static float RespawnLossMultiplier = 1f;

        private PlayerController controller;
        private Camera cam;
        private float baseFov = -1f;
        private Transform muzzle;
        private Light muzzleFlash;
        private float flashUntil;
        private WeaponViewModel view;
        private WeaponState lastState;
        private float lastEmptyClick = -999f;
        private Vector3 lastPosition;
        private int selfMask = ~(1 << 2);
        private readonly TriggerLatch fireTrigger = new TriggerLatch();
        private readonly TriggerLatch aimTrigger = new TriggerLatch();
        private readonly DpadEdges dpad = new DpadEdges();

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            Instance = this;
            Arsenal = CombatSession.Load();
            Arsenal.Switched += OnSwitched;
        }

        private void OnEnable() => PlayerRespawn.PlayerRespawned += OnRespawned;
        private void OnDisable() => PlayerRespawn.PlayerRespawned -= OnRespawned;

        private void OnDestroy()
        {
            CombatSession.Store(Arsenal);   // la siguiente fase empieza con la misma munición
            if (Instance == this) Instance = null;
            if (cam != null && baseFov > 0f) cam.fieldOfView = baseFov;
        }

        private void Start()
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[PlayerWeapon] No hay cámara del jugador: las armas quedan desactivadas.", this); enabled = false; return; }
            baseFov = cam.fieldOfView;

            muzzle = new GameObject("CanonArma").transform;
            muzzle.SetParent(cam.transform, false);
            muzzle.localPosition = new Vector3(0.22f, -0.2f, 0.75f);
            var flashGo = new GameObject("DestelloCanon");
            flashGo.transform.SetParent(muzzle, false);
            muzzleFlash = flashGo.AddComponent<Light>();
            muzzleFlash.type = LightType.Point;
            muzzleFlash.range = 4f;
            muzzleFlash.intensity = 3f;
            muzzleFlash.enabled = false;

            view = WeaponViewModel.Build(cam.transform, GetComponent<CameraModeSwitcher>());
            RefreshView();
            lastPosition = transform.position;
        }

        private void Update()
        {
            float t = Time.time, dt = Time.deltaTime;
            foreach (var weapon in Arsenal.Weapons) weapon.Tick(t, dt);   // las que no están en la mano también se enfrían
            var w = Arsenal.Current;
            if (w == null || cam == null) return;

            bool moving = (transform.position - lastPosition).sqrMagnitude > 0.0004f;
            lastPosition = transform.position;
            if (muzzleFlash != null && muzzleFlash.enabled && t > flashUntil) muzzleFlash.enabled = false;

            // El mando se lee siempre, para que las pulsaciones y los gatillos no se queden «a medias» tras un menú
            bool padFire = fireTrigger.Update(GamepadAxes.Read(GamepadAxes.RightTrigger));
            bool padAim = aimTrigger.Update(GamepadAxes.Read(GamepadAxes.LeftTrigger));
            dpad.Update(GamepadAxes.Read(GamepadAxes.DpadX), GamepadAxes.Read(GamepadAxes.DpadY));

            bool canAct = controller.InputEnabled && !controller.InputReleasedThisFrame && !EnemyPatrol.Paused && !GameHud.Hidden
                          && GameInput.PointerCaptured && Time.timeScale > 0f;
            if (!canAct)
            {
                w.TryFire(t, false);
                SetAim(false, dt);
                CurrentSpread = SpreadModel.Current(w.Spec, false, moving, w.Bloom);
                return;
            }

            if (GameInput.GetKeyDown(nextWeaponKey) || dpad.Right) Arsenal.Cycle(1, t);
            else if (dpad.Left) Arsenal.Cycle(-1, t);
            if (GameInput.GetKeyDown(reloadKey) || dpad.Down) w.StartReload(t);
            w = Arsenal.Current;

            SetAim(GameInput.GetKey(aimKey) || padAim, dt);
            CurrentSpread = SpreadModel.Current(w.Spec, IsAiming, moving, w.Bloom);

            if (w.State == WeaponState.Reloading && lastState != WeaponState.Reloading) CombatSfx.Play(CombatSound.Reload, 0.6f);
            if (w.State == WeaponState.Overheated && lastState != WeaponState.Overheated) CombatSfx.Play(CombatSound.Overheat, 0.7f);
            lastState = w.State;

            if (Arsenal.IsSwitching(t)) { w.TryFire(t, false); return; }

            bool trigger = GameInput.GetKey(fireKey) || padFire;
            var result = w.TryFire(t, trigger);
            if (result == FireResult.Fired) Shoot(w, moving);
            else if (result == FireResult.Empty && t - lastEmptyClick > 0.3f)
            {
                lastEmptyClick = t;
                CombatSfx.Play(CombatSound.Empty, 0.6f);
                if (w.IsEmpty) PickupNotificationUI.Notify($"{w.Spec.DisplayName}: SIN MUNICIÓN. Cambia de arma con [Q].", new Color(1f, 0.45f, 0.35f));
            }
        }

        private void SetAim(bool aim, float dt)
        {
            IsAiming = aim;
            if (cam == null || baseFov <= 0f) return;
            float target = aim ? aimFov : baseFov;
            cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, target, Mathf.Abs(baseFov - aimFov) * aimSpeed * dt);
        }

        private void Shoot(WeaponModel w, bool moving)
        {
            var spec = w.Spec;
            int shotId = CombatEvents.NextShotId();
            LastShotTime = Time.time;
            Transform camT = cam.transform;
            Vector3 camPos = camT.position;

            // Punto de mira: lo primero que toca el rayo del centro de la cámara (o el alcance)
            Vector3 aimPoint = camPos + camT.forward * spec.Range;
            if (CombatRaycast.First(camPos, camT.forward, spec.Range, transform, selfMask, out RaycastHit hit)) aimPoint = hit.point;

            // Cañón metido en una pared → sale desde la cámara (no atraviesa la pared)
            Vector3 spawn = muzzle.position;
            if (CombatRaycast.First(camPos, (spawn - camPos).normalized, Vector3.Distance(camPos, spawn), transform, selfMask, out _)) spawn = camPos;
            Vector3 baseDir = (aimPoint - spawn).normalized;

            var info = new DamageInfo { Origin = transform.position, Source = gameObject, WeaponId = spec.Id, ShotId = shotId };
            float spread = SpreadModel.Current(spec, IsAiming, moving, w.Bloom);
            for (int i = 0; i < spec.Pellets; i++)
            {
                float yaw, pitch;
                if (spec.Pellets > 1) SpreadModel.PelletOffset(i, spec.Pellets, spread, Random.value, Random.value, out yaw, out pitch);
                else SpreadModel.Offset(spread, Random.value, Random.value, out yaw, out pitch);
                Vector3 dir = Quaternion.AngleAxis(yaw, camT.up) * (Quaternion.AngleAxis(-pitch, camT.right) * baseDir);
                ProjectilePool.Instance.Get().Launch(spawn, dir, spec, info, transform);
            }

            NoiseSystem.Emit(transform.position, spec.NoiseRadius * NoiseMultiplier, gameObject);
            CombatSfx.PlayShot(spec.Id);
            if (muzzleFlash != null)
            {
                muzzleFlash.color = new Color(spec.ColorR, spec.ColorG, spec.ColorB);
                muzzleFlash.enabled = true;
                flashUntil = Time.time + 0.05f;
            }
            if (view != null) view.Kick(spec.Recoil);
            CombatEvents.RaiseShot(shotId, spec.Id, spec.Pellets);
        }

        private void OnSwitched(WeaponModel w)
        {
            CombatSfx.Play(CombatSound.Switch, 0.6f);
            if (view != null) view.Show(w?.Spec, w?.Mods);
            lastState = w != null ? w.State : WeaponState.Ready;
        }

        /// <summary>Vuelve a montar el arma de la mano con sus accesorios.</summary>
        private void RefreshView()
        {
            if (view != null) view.Show(Arsenal.Current?.Spec, Arsenal.Current?.Mods);
        }

        /// <summary>Paquete de munición recogido: va al arma que menos tiene. Devuelve false si todo está lleno.</summary>
        public bool GiveAmmoPack(float packs)
        {
            var w = Arsenal.NeediestWeapon();
            if (w == null) return false;
            int added = w.AddReserve(Mathf.Max(1, Mathf.RoundToInt(w.Spec.AmmoPerPickup * packs)));
            if (added <= 0) return false;
            CombatSfx.Play(CombatSound.AmmoPickup, 0.5f);
            PickupNotificationUI.Notify($"+{added} munición · {w.Spec.DisplayName}", new Color(0.55f, 0.95f, 1f));
            return true;
        }

        /// <summary>
        /// Módulo de accesorio recogido: va al arma de la mano si le sirve (ArsenalModel.ModTarget). Si ninguna arma lo
        /// admite, devuelve false y el módulo se queda en el suelo.
        /// </summary>
        public bool GiveMod(WeaponMod mod)
        {
            var w = Arsenal.ModTarget(mod);
            if (w == null) return false;
            var result = w.InstallMod(mod, out WeaponMod replaced);
            if (result != ModInstall.Installed && result != ModInstall.Replaced) return false;
            CombatSfx.Play(CombatSound.Switch, 0.8f);
            RefreshView();   // el accesorio se ve montado en el arma
            string text = $"{WeaponModModel.Name(mod)} en {w.Spec.DisplayName}: {WeaponModModel.Effect(mod)}";
            if (result == ModInstall.Replaced) text += $" (sustituye al {WeaponModModel.Name(replaced).ToLowerInvariant()})";
            PickupNotificationUI.Notify(text, new Color(0.75f, 0.6f, 1f));
            return true;
        }

        private void OnRespawned()
        {
            int lost = Arsenal.ApplyRespawnPenalty(respawnAmmoLoss * RespawnLossMultiplier);
            foreach (var w in Arsenal.Weapons) w.ReleaseTrigger();
            if (lost > 0) PickupNotificationUI.Notify($"Munición perdida al caer: {lost}", new Color(1f, 0.7f, 0.35f));
        }
    }

    /// <summary>Rayo que devuelve el impacto más cercano que no sea del propio tirador.</summary>
    public static class CombatRaycast
    {
        private static readonly RaycastHit[] buffer = new RaycastHit[16];

        public static bool First(Vector3 origin, Vector3 dir, float distance, Transform ignore, int mask, out RaycastHit result)
        {
            result = default(RaycastHit);
            if (distance <= 0f) return false;
            int n = Physics.RaycastNonAlloc(origin, dir, buffer, distance, mask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var t = buffer[i].transform;
                if (t == null || (ignore != null && t.IsChildOf(ignore))) continue;
                if (buffer[i].distance < best) { best = buffer[i].distance; result = buffer[i]; found = true; }
            }
            return found;
        }
    }
}

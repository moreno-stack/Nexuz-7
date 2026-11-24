using UnityEngine;
using Nexus7.Combat;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Director
{
    /// <summary>
    /// IA analizadora / director dinámico. Adaptador de Unity de PlayerPerformanceModel + DirectorModel:
    ///   1. Escucha eventos que ya existen (disparos e impactos de CombatEvents, daño y caídas de PlayerHealth,
    ///      detecciones de EnemyPatrol) — ningún sistema sabe que el director existe.
    ///   2. Cada 0,5 s muestrea la posición y la vida, actualiza el modelo y aplica las perillas
    ///      (AdaptiveModifiers, vida de los robots, munición) y las acciones ante patrones.
    /// Todo es local y heurístico: unas decenas de operaciones cada medio segundo, sin impacto en los FPS.
    /// [F3] muestra el panel del director con las métricas en vivo.
    ///
    /// Colocar en: el jugador (se añade solo al cargar cada fase).
    /// </summary>
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private KeyCode overlayKey = KeyCode.F3;
        [SerializeField] private float messageDuration = 3f;

        public PlayerPerformanceModel Performance { get; private set; }
        public DirectorModel Model { get; private set; }
        public string LastAction { get; private set; } = "—";

        private PlayerHealth health;
        private float timer;
        private bool overlay;
        private GUIStyle style;
        private static Texture2D white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.GetComponent<PlayerWeapon>() != null) Attach(player);
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<GameDirector>() == null) player.AddComponent<GameDirector>();
        }

        private void Awake()
        {
            Instance = this;
            health = GetComponent<PlayerHealth>();
            Performance = new PlayerPerformanceModel(Time.time);
            Model = new DirectorModel((int)DifficultySettings.Current, DirectorSession.HasTension ? DirectorSession.Tension : -1f);
            EnemyHealth.WeaponDamageModifier = id => Model.WeaponDamageFactor(id);
            Apply(Model.Knobs);
        }

        private void OnEnable()
        {
            CombatEvents.ShotFired += OnShot;
            CombatEvents.HitLanded += OnHit;
            CombatEvents.EnemyKilled += OnKill;
            PlayerHealth.PlayerDied += OnDied;
            EnemyPatrol.PlayerSpotted += OnSpotted;
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            CombatEvents.ShotFired -= OnShot;
            CombatEvents.HitLanded -= OnHit;
            CombatEvents.EnemyKilled -= OnKill;
            PlayerHealth.PlayerDied -= OnDied;
            EnemyPatrol.PlayerSpotted -= OnSpotted;
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void OnDestroy()
        {
            if (Model != null) DirectorSession.Store(Model.Tension);
            if (Instance != this) return;
            Instance = null;
            AdaptiveModifiers.Reset();
            EnemyHealth.WeaponDamageModifier = null;
            AmmoPickup.DropMultiplier = 1f;
            PlayerWeapon.RespawnLossMultiplier = 1f;
            EnemyGun.DirectorExtraShooters = 0;
            EnemyGun.DirectorSpreadMultiplier = 1f;
            EnemyTactics.DirectorTempo = 1f;
        }

        private void OnShot(int shotId, string weaponId, int pellets) => Performance.RecordShot(Time.time, shotId, weaponId);
        private void OnHit(DamageInfo info, bool killed) => Performance.RecordHit(Time.time, info.ShotId);
        private void OnKill(EnemyHealth enemy) => Performance.RecordKill(Time.time);
        private void OnDamaged(float amount) => Performance.RecordDamageTaken(Time.time, amount);
        private void OnSpotted(EnemyPatrol by) => Performance.RecordDetected(Time.time);

        private void OnDied()
        {
            Performance.RecordDeath(Time.time);
            Model.OnPlayerDeath();
            Apply(Model.Knobs);
            LastAction = "Caída: respiro de " + Model.ReliefDuration.ToString("0") + " s";
        }

        private void Update()
        {
            if (Input.GetKeyDown(overlayKey)) overlay = !overlay;
            timer += Time.deltaTime;
            if (timer < tickInterval) return;
            float dt = timer;
            timer = 0f;

            Performance.SamplePosition(Time.time, transform.position.x, transform.position.z);
            if (health != null) Performance.HealthFraction = health.Normalized;
            var action = Model.Tick(dt, Performance);
            Apply(Model.Knobs);
            Handle(action);
        }

        private void Apply(DirectorKnobs k)
        {
            AdaptiveModifiers.Set(k.EnemySpeed, k.Detection, k.EnemyDamage, k.PatrolWait);
            if (Mathf.Abs(k.EnemyHealth - EnemyHealth.HealthMultiplier) > 0.03f) EnemyHealth.SetHealthMultiplier(k.EnemyHealth);
            AmmoPickup.DropMultiplier = k.AmmoDrop;
            PlayerWeapon.RespawnLossMultiplier = k.RespawnAmmoLoss;
            EnemyGun.DirectorExtraShooters = k.ExtraShooters;
            EnemyGun.DirectorSpreadMultiplier = k.EnemySpread;
            EnemyTactics.DirectorTempo = k.TacticTempo;
        }

        private void Handle(DirectorAction action)
        {
            switch (action)
            {
                case DirectorAction.FlushCamper:
                    Performance.CampCenter(out float x, out float z);
                    var target = new Vector3(x, transform.position.y, z);
                    var unit = NearestFreeRobot(target);
                    if (unit == null) return;
                    unit.RespondToSquad(target);
                    Say("IA NEXUS-7: patrón detectado. Llevas un rato en la misma zona: una unidad viene a comprobarlo.");
                    LastAction = "Acampada → " + unit.name + " investiga";
                    break;
                case DirectorAction.AdaptToWeapon:
                    var spec = PlayerWeaponCatalog.Get(Model.AdaptedWeapon);
                    string weaponName = spec != null ? spec.DisplayName : Model.AdaptedWeapon;
                    Say($"IA NEXUS-7: los robots han analizado tu {weaponName.ToLowerInvariant()} y refuerzan su blindaje. Varía de arma.");
                    LastAction = "Blindaje adaptado a " + weaponName;
                    break;
                case DirectorAction.StopAdapting:
                    Say("IA NEXUS-7: blindaje adaptativo desactivado.");
                    LastAction = "Fin del blindaje adaptativo";
                    break;
                case DirectorAction.Assist:
                    AmmoPickup.Spawn(transform.position + transform.forward * 2f + Vector3.up * 0.4f, 2f);
                    Say("TRAJE: suministro de emergencia liberado delante de ti. Respira y busca otra ruta.");
                    LastAction = "Ayuda: munición y menos tensión";
                    break;
            }
        }

        /// <summary>El robot activo más cercano a 'point' que no esté ya persiguiendo al jugador.</summary>
        private static EnemyPatrol NearestFreeRobot(Vector3 point)
        {
            EnemyPatrol best = null;
            float bestDist = float.MaxValue;
            foreach (var e in EnemyPatrol.All)
            {
                if (e == null || !e.isActiveAndEnabled || e.CurrentState == EnemyPatrol.EnemyState.Chase || !e.CanMove) continue;
                float d = (e.transform.position - point).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        private void Say(string text)
        {
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(text, messageDuration);
        }

        // ------------------------- Panel [F3] -------------------------
        private void OnGUI()
        {
            if (!overlay) return;
            if (white == null) { white = new Texture2D(1, 1, TextureFormat.RGBA32, false); white.SetPixel(0, 0, Color.white); white.Apply(); }
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            var p = Performance;
            var k = Model.Knobs;
            float x = 16f, y = 16f, w = 360f, h = 470f;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(x, y, w, h), white);
            GUI.color = Color.white;
            guiX = x + 12f; guiW = w - 24f; cy = y + 8f;

            L("<b>DIRECTOR ADAPTATIVO</b>   [F3] ocultar");
            L($"Tensión <b>{Model.Tension:0.00}</b>  → objetivo {Model.Target:0.00}  (base {Model.Baseline:0.00}){(Model.InRelief ? "  <color=#7cf>RESPIRO</color>" : "")}");
            GUI.color = new Color(1f, 1f, 1f, 0.15f); GUI.DrawTexture(new Rect(x + 12f, cy, w - 24f, 8f), white);
            GUI.color = Color.Lerp(new Color(0.3f, 0.8f, 1f), new Color(1f, 0.3f, 0.2f), Model.Tension);
            GUI.DrawTexture(new Rect(x + 12f, cy, (w - 24f) * Model.Tension, 8f), white);
            GUI.color = Color.white; cy += 14f;
            L($"Rendimiento <b>{p.Performance:+0.00;-0.00}</b>");
            L($"Precisión {p.Accuracy * 100f:0}%  ({p.HitsInWindow}/{p.ShotsInWindow} disparos, 60 s)");
            L($"Bajas/min {p.KillsPerMinute:0.0}   Daño/min {p.DamagePerMinute:0}");
            L($"Caídas recientes {p.RecentDeaths:0.0}  (total {p.TotalDeaths})   Sobrevivido {p.TimeSinceDeath:0} s");
            L($"Detecciones/min {p.DetectionsPerMinute:0.0}   Vida {p.HealthFraction * 100f:0}%");
            float dom = p.WeaponDominance(out string wid);
            L($"Acampada {p.CampingScore * 100f:0}%   Arma dominante {(wid ?? "—")} {dom * 100f:0}%");
            cy += 6f;
            L("<b>Perillas</b> (1 = diseño)");
            L($"Velocidad robots ×{k.EnemySpeed:0.00}   Detección ×{k.Detection:0.00}");
            L($"Daño robots ×{k.EnemyDamage:0.00}   Vida robots ×{k.EnemyHealth:0.00}");
            L($"Munición paquetes ×{k.AmmoDrop:0.00}   Pérdida al caer ×{k.RespawnAmmoLoss:0.00}");
            L($"Espera en waypoints ×{k.PatrolWait:0.00}   Refuerzos {k.Reinforcements}");
            L($"Tiradores extra {k.ExtraShooters}   Dispersión robots ×{k.EnemySpread:0.00}   Turnos {EnemyGun.Tokens.InUse}/{EnemyGun.Tokens.Capacity}");
            L($"Tempo táctico ×{k.TacticTempo:0.00}   {EnemyTactics.Summary()}");
            L(EncounterDirector.Summary());
            L($"Blindaje adaptado: {(Model.AdaptedWeapon ?? "no")}");
            cy += 6f;
            L($"Última acción: {LastAction}");
            ExtraOverlay?.Invoke(x + 12f, cy, w - 24f, style);
        }

        private float guiX, guiW, cy;
        private void L(string text) { GUI.Label(new Rect(guiX, cy, guiW, 20f), text, style); cy += 20f; }

        /// <summary>Líneas extra del panel (las añade el sistema de respawn dinámico).</summary>
        public static event System.Action<float, float, float, GUIStyle> ExtraOverlay;
    }
}

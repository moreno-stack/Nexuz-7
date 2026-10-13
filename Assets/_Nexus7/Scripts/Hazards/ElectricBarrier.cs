using UnityEngine;
using Nexus7.Core;
using Nexus7.Player;
using Nexus7.Security;

namespace Nexus7.Hazards
{
    /// <summary>
    /// Barrera eléctrica (valla de haces). Bloquea el paso y daña al acercarse.
    /// Se puede desactivar TEMPORALMENTE con un HackTerminal (implementa IHackable).
    /// Antes de reactivarse, los haces parpadean como aviso; si el jugador está dentro, espera a que salga
    /// (para no atraparlo dentro del collider).
    ///
    /// Estructura:
    ///   Raíz  : ElectricBarrier + BoxCollider "Is Trigger" algo más grueso (zona de descarga), capa Ignore Raycast
    ///   Hijo  : "Bloqueo" con BoxCollider sólido (el muro invisible)
    ///   Hijos : haces con ElectricArc (jitter bajo) + Light
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ElectricBarrier : MonoBehaviour, IHackable, IEmpTarget
    {
        [SerializeField] private string hackLabel = "Barrera eléctrica";
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private ElectricArc[] beams;
        [SerializeField] private Light barrierLight;
        [SerializeField] private float lightIntensity = 2.5f;
        [SerializeField] private float contactDamage = 30f;
        [SerializeField] private float hitInterval = 0.8f;
        [Tooltip("Segundos de parpadeo antes de reactivarse.")]
        [SerializeField] private float reactivationWarning = 4f;
        [SerializeField] private AudioSource humSource;

        private Collider zone;
        private PlayerHealth player;
        private CharacterController playerCollider;
        private bool hacked;
        private float disabledUntil;
        private float nextHit;

        public string HackLabel => hackLabel;
        public bool IsDisabled => hacked;
        public float RemainingDisabledTime => hacked ? Mathf.Max(0f, disabledUntil - Time.time) : 0f;

        private void Awake() => zone = GetComponent<Collider>();

        public Vector3 EmpPosition => transform.position;
        public void OnEmp(float duration) => Disable(duration);
        private void OnEnable() => EmpRegistry.Register(this);
        private void OnDisable() => EmpRegistry.Unregister(this);

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.GetComponent<PlayerHealth>();
                playerCollider = go.GetComponent<CharacterController>();
            }
            if (humSource != null) humSource.Play();
        }

        private void Update()
        {
            bool inside = playerCollider != null && playerCollider.enabled && zone.bounds.Intersects(playerCollider.bounds);

            if (hacked)
            {
                float left = disabledUntil - Time.time;
                if (left <= 0f)
                {
                    if (inside) disabledUntil = Time.time + 0.5f; // espera a que el jugador salga
                    else Reactivate();
                }
                else if (left < reactivationWarning)
                {
                    bool blink = Mathf.Repeat(Time.time * 4f, 1f) < 0.5f; // aviso: parpadeo cada vez más evidente
                    SetBeams(blink);
                    if (barrierLight != null) barrierLight.intensity = blink ? lightIntensity * 0.5f : 0f;
                }
                return;
            }

            if (barrierLight != null) barrierLight.intensity = lightIntensity * (0.8f + 0.2f * Random.value);

            if (inside && player != null && Time.time >= nextHit)
            {
                nextHit = Time.time + hitInterval;
                player.TakeDamage(contactDamage);
                NoiseSystem.Emit(transform.position, 6f, gameObject);
            }
        }

        public void Disable(float duration)
        {
            hacked = true;
            disabledUntil = Time.time + duration;
            if (blockingCollider != null) blockingCollider.enabled = false;
            SetBeams(false);
            if (barrierLight != null) barrierLight.intensity = 0f;
            if (humSource != null) humSource.Stop();
        }

        private void Reactivate()
        {
            hacked = false;
            if (blockingCollider != null) blockingCollider.enabled = true;
            SetBeams(true);
            if (humSource != null) humSource.Play();
        }

        private void SetBeams(bool visible)
        {
            if (beams == null) return;
            foreach (var b in beams) if (b != null) b.SetVisible(visible);
        }
    }
}

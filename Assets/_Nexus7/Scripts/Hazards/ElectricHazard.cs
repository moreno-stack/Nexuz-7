using UnityEngine;
using Nexus7.Core;
using Nexus7.Player;
using Nexus7.Power;

namespace Nexus7.Hazards
{
    /// <summary>
    /// Circuito dañado que actúa como trampa.
    ///   Efecto  : Damage (resta integridad) o Stun (daño leve + bloquea el control unos segundos).
    ///   Patrón  : Constant (siempre activo) o Intermittent (encendido/apagado con aviso de chispas antes de descargar).
    ///   Energía : 'Required Grid Power' > 0 → solo está vivo cuando la red tiene ese nivel
    ///             (¡restaurar la energía puede activar trampas nuevas!).
    ///   Repair(): lo desactiva para siempre (lo llama RepairPanel gastando chatarra).
    /// Cada descarga hace ruido y atrae a los drones.
    ///
    /// Colocar en: un GameObject con BoxCollider "Is Trigger" (capa Ignore Raycast) que cubra la zona peligrosa.
    /// La detección usa los límites del collider (robusta aunque el jugador reaparezca de golpe).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ElectricHazard : MonoBehaviour
    {
        public enum HazardEffect { Damage, Stun }
        public enum HazardPattern { Constant, Intermittent }

        [SerializeField] private HazardEffect effect = HazardEffect.Damage;
        [SerializeField] private HazardPattern pattern = HazardPattern.Intermittent;
        [SerializeField] private float damage = 25f;
        [Tooltip("Segundos mínimos entre dos descargas al jugador.")]
        [SerializeField] private float hitInterval = 0.8f;
        [SerializeField] private float stunDuration = 1.6f;

        [Header("Ciclo (Intermittent)")]
        [SerializeField] private float onTime = 2f;
        [SerializeField] private float offTime = 2.5f;
        [Tooltip("Chispas de aviso antes de cada descarga.")]
        [SerializeField] private float warningTime = 0.8f;
        [Tooltip("Desfase para que varias trampas no vayan sincronizadas.")]
        [SerializeField] private float timeOffset = 0f;

        [Header("Energía")]
        [SerializeField, Min(0)] private int requiredGridPower = 0;

        [Header("Ruido y visuales")]
        [SerializeField] private float zapNoiseRadius = 6f;
        [SerializeField] private ElectricArc[] arcs;
        [SerializeField] private SparkEmitter sparks;
        [SerializeField] private Light arcLight;
        [SerializeField] private float arcLightIntensity = 3f;
        [SerializeField] private AudioSource humSource;

        public bool IsRepaired { get; private set; }
        public bool IsActive { get; private set; }

        private Collider zone;
        private PlayerHealth player;
        private CharacterController playerCollider;
        private float nextHit;

        private void Awake() => zone = GetComponent<Collider>();

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2; // Ignore Raycast
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.GetComponent<PlayerHealth>();
                playerCollider = go.GetComponent<CharacterController>();
            }
        }

        private void Update()
        {
            bool live = !IsRepaired && PowerGrid.Power >= requiredGridPower;
            bool on = false, warning = false;

            if (live)
            {
                if (pattern == HazardPattern.Constant) on = true;
                else
                {
                    float cycle = onTime + offTime;
                    float t = Mathf.Repeat(Time.time + timeOffset, cycle);
                    on = t < onTime;
                    warning = !on && t > cycle - warningTime;
                }
            }
            IsActive = on;

            // ---- Visuales ----
            if (arcs != null) foreach (var a in arcs) if (a != null) a.SetVisible(on);
            if (arcLight != null)
                arcLight.intensity = on ? arcLightIntensity * (0.6f + 0.4f * Random.value)
                                   : warning ? arcLightIntensity * 0.3f * Random.value : 0f;
            if (sparks != null)
            {
                if (warning && Random.value < 10f * Time.deltaTime) sparks.Burst(4);
                if (on && Random.value < 5f * Time.deltaTime) sparks.Burst();
            }
            if (humSource != null)
            {
                if (on && !humSource.isPlaying) humSource.Play();
                else if (!on && humSource.isPlaying) humSource.Stop();
            }

            // ---- Daño ----
            if (on && player != null && Time.time >= nextHit && PlayerInside())
            {
                nextHit = Time.time + hitInterval;
                Zap();
            }
        }

        private bool PlayerInside() =>
            playerCollider != null && playerCollider.enabled && zone.bounds.Intersects(playerCollider.bounds);

        private void Zap()
        {
            if (effect == HazardEffect.Stun) player.Stun(stunDuration, damage);
            else player.TakeDamage(damage);
            NoiseSystem.Emit(transform.position, zapNoiseRadius, gameObject);
            if (sparks != null) sparks.Burst(30);
        }

        /// <summary>Aísla el circuito: deja de ser peligroso para siempre.</summary>
        public void Repair()
        {
            IsRepaired = true;
            IsActive = false;
            if (arcs != null) foreach (var a in arcs) if (a != null) a.SetVisible(false);
            if (arcLight != null) arcLight.intensity = 0f;
            if (humSource != null) humSource.Stop();
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}

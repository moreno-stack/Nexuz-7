using UnityEngine;
using Nexus7.Stealth;

namespace Nexus7.Enemies
{
    /// <summary>
    /// "Ojos" del enemigo. Cono de visión (distancia + ángulo) con comprobación de línea de visión
    /// (las cajas y estanterías tapan). Acumula un medidor de ALERTA de 0 a 1:
    ///   - sube más rápido cuanto más cerca y más visible esté el jugador (PlayerStealth)
    ///   - baja poco a poco cuando deja de verlo
    /// EnemyPatrol lee este valor para decidir si investigar (≥ umbral) o perseguir (= 1).
    /// La luz foco del enemigo muestra su cono y cambia de color según la alerta.
    ///
    /// Colocar en: la raíz del enemigo (junto a EnemyPatrol). "Eye" = hijo a la altura de la cabeza.
    /// </summary>
    public class EnemyVision : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [SerializeField] private float viewDistance = 12f;
        [Range(10f, 180f)]
        [SerializeField] private float viewAngle = 80f;
        [Tooltip("A esta distancia te detecta aunque estés fuera del cono (te 'siente').")]
        [SerializeField] private float proximityRadius = 1.6f;

        [Header("Medidor de alerta")]
        [Tooltip("Alerta por segundo con visibilidad 1 a media distancia.")]
        [SerializeField] private float detectionSpeed = 1.1f;
        [SerializeField] private float decaySpeed = 0.25f;

        [Tooltip("Capas que bloquean la visión. Excluye 'Ignore Raycast' (triggers).")]
        [SerializeField] private LayerMask obstacleMask = ~(1 << 2);

        [Header("Feedback visual (opcional)")]
        [SerializeField] private Light visionLight;
        [SerializeField] private Color calmColor = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color suspiciousColor = new Color(1f, 0.8f, 0.1f);
        [SerializeField] private Color alertColor = new Color(1f, 0.1f, 0.05f);

        /// <summary>Todos los "ojos" activos: drones y cámaras (lo usa el indicador de sigilo).</summary>
        public static readonly System.Collections.Generic.List<EnemyVision> All = new System.Collections.Generic.List<EnemyVision>();

        public float Awareness { get; private set; }
        public bool CanSeePlayer { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }
        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;

        private float boostMultiplier = 1f;
        private float boostUntil = -999f;
        private bool blinded;

        /// <summary>true mientras está "receloso" (tras un señuelo, un PEM o un banco manipulado).</summary>
        public bool IsWary => Time.time < boostUntil;
        public bool IsBlinded => blinded;

        private Transform player;
        private CharacterController playerController;
        private PlayerStealth playerStealth;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            if (eye == null) eye = transform;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.transform;
                playerController = go.GetComponent<CharacterController>();
                playerStealth = go.GetComponent<PlayerStealth>();
            }
            if (visionLight != null)
            {
                visionLight.type = LightType.Spot;
                visionLight.spotAngle = viewAngle;
                visionLight.range = viewDistance;
            }
        }

        private void Update()
        {
            if (blinded) { CanSeePlayer = false; return; }

            CanSeePlayer = player != null && CheckLineOfSight(out float distance);

            if (CanSeePlayer)
            {
                LastSeenPosition = player.position;
                float visibility = playerStealth != null ? playerStealth.Visibility : 1f;
                float distanceFactor = Mathf.Lerp(2.5f, 0.4f, distance / viewDistance); // de cerca se detecta mucho antes
                float wary = IsWary ? boostMultiplier : 1f;
                Awareness += detectionSpeed * visibility * distanceFactor * wary * Time.deltaTime;
            }
            else
            {
                Awareness -= decaySpeed * Time.deltaTime;
            }
            Awareness = Mathf.Clamp01(Awareness);

            if (visionLight != null)
            {
                visionLight.color = Awareness >= 1f
                    ? alertColor
                    : Color.Lerp(calmColor, suspiciousColor, Mathf.InverseLerp(0f, 0.6f, Awareness));
            }
        }

        private bool CheckLineOfSight(out float distance)
        {
            Vector3 eyePos = eye.position;
            float height = playerController != null ? playerController.height : 1.8f;
            Vector3 chest = player.position + Vector3.up * (height * 0.5f);
            Vector3 head = player.position + Vector3.up * (height * 0.9f);

            Vector3 toPlayer = chest - eyePos;
            distance = toPlayer.magnitude;
            if (distance > viewDistance) return false;

            bool inProximity = distance <= proximityRadius;
            if (!inProximity && Vector3.Angle(eye.forward, toPlayer) > viewAngle * 0.5f) return false;

            // Oculto en la sombra: solo se le detecta muy de cerca.
            if (playerStealth != null && playerStealth.IsHidden && distance > proximityRadius * 1.5f) return false;

            // Basta con ver la cabeza o el pecho.
            return HasClearLine(eyePos, head) || HasClearLine(eyePos, chest);
        }

        private bool HasClearLine(Vector3 from, Vector3 to)
        {
            if (!Physics.Linecast(from, to, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        /// <summary>Fuerza la alerta máxima (p. ej. al tropezar con el jugador).</summary>
        public void ForceAlert()
        {
            Awareness = 1f;
            if (player != null) LastSeenPosition = player.position;
        }

        /// <summary>Multiplica temporalmente la velocidad de detección (el dron está "receloso").</summary>
        public void BoostDetection(float multiplier, float duration)
        {
            boostMultiplier = Mathf.Max(1f, multiplier);
            boostUntil = Mathf.Max(boostUntil, Time.time + duration);
        }

        /// <summary>Ceguera temporal (PEM): no ve nada y su foco se apaga.</summary>
        public void SetBlinded(bool value)
        {
            blinded = value;
            if (value) ResetAwareness();
            if (visionLight != null) visionLight.enabled = !value;
        }

        public void ResetAwareness()
        {
            Awareness = 0f;
            CanSeePlayer = false;
        }

        private void OnDrawGizmosSelected()
        {
            Transform e = eye != null ? eye : transform;
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Vector3 left = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up) * e.forward;
            Vector3 right = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up) * e.forward;
            Gizmos.DrawRay(e.position, left * viewDistance);
            Gizmos.DrawRay(e.position, right * viewDistance);
            Gizmos.DrawWireSphere(transform.position, proximityRadius);
        }
    }
}

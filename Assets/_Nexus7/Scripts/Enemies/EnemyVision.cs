using UnityEngine;
using Nexus7.Stealth;
using Nexus7.Environment;

namespace Nexus7.Enemies
{
    /// <summary>
    /// "Ojos" del enemigo, con reglas estrictas (PerceptionModel):
    ///   1. Cono y distancia: solo ve dentro de su cono (75–90° en los robots) y de su alcance. Fuera, está totalmente ciego.
    ///      Alcance = base (12 m) × alerta de la nave × sigilo del jugador, con un tope duro de 15 m (6 m si hay humo).
    ///   2. Oclusión: lanza 5 rayos desde el ojo (cabeza, pecho, cadera y hombros del jugador). Paredes, cajas,
    ///      estanterías y el humo denso (VisionSmoke) los tapan. Si no llega ninguno, deja de verte en ese fotograma.
    ///   3. Sigilo: agachado en sombra o con camuflaje → alcance ×0,6; corriendo o bajo luz directa → ×1,3 (PlayerStealth).
    ///   4. Memoria: guarda la última posición y velocidad con que te vio; EnemyPatrol va allí a buscarte.
    /// Acumula un medidor de ALERTA de 0 a 1: sube más rápido cuanto más cerca, más visible y más rayos te alcancen,
    /// y baja poco a poco cuando deja de verte. La luz foco muestra su cono y cambia de color según la alerta.
    ///
    /// Colocar en: la raíz del enemigo (junto a EnemyPatrol) o de la cámara. "Eye" = hijo a la altura de la cabeza/lente.
    /// </summary>
    public class EnemyVision : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [Tooltip("Alcance base en metros (pasillos: 12–15). Lo modifican la alerta y el sigilo, sin pasar nunca del tope.")]
        [SerializeField] private float viewDistance = 12f;
        [Tooltip("Apertura total del cono. En los robots se limita a 75–90°; las cámaras pueden ser más estrechas.")]
        [Range(30f, 120f)]
        [SerializeField] private float viewAngle = 80f;
        [Tooltip("Robots: diferencia máxima de inclinación con la mirada (el ángulo del cono se mide en horizontal).")]
        [SerializeField] private float verticalHalfAngle = 60f;
        [Tooltip("Tope duro: ningún multiplicador permite ver más lejos.")]
        [SerializeField] private float maxViewDistance = 15f;
        [Tooltip("Alcance máximo si el robot o el jugador están dentro del humo.")]
        [SerializeField] private float smokeViewDistance = 6f;
        [Tooltip("Metros de humo denso que tapan un rayo.")]
        [SerializeField] private float smokeBlockLength = 1.5f;

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
        /// <summary>Dónde vio al jugador por última vez (no cambia mientras no lo vea).</summary>
        public Vector3 LastSeenPosition { get; private set; }
        /// <summary>Velocidad con la que se movía el jugador la última vez que lo vio (para buscar hacia donde huía).</summary>
        public Vector3 LastSeenVelocity { get; private set; }
        /// <summary>Cuántos de los 5 rayos llegaron al jugador en el último fotograma.</summary>
        public int VisibleRays { get; private set; }
        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;
        public float MaxViewDistance => maxViewDistance;

        /// <summary>Alcance contra el jugador en este momento (alerta + sigilo + humo, con el tope).</summary>
        public float EffectiveViewDistance { get; private set; }

        /// <summary>Alcance sin contar el sigilo del jugador ni el humo (lo que muestra el foco).</summary>
        public float ConeDistance =>
            PerceptionModel.EffectiveRange(viewDistance, Nexus7.Core.AlertSystem.Profile.viewDistance, 1f, false, maxViewDistance, smokeViewDistance);

        private float boostMultiplier = 1f;
        private float boostUntil = -999f;
        private bool blinded;

        /// <summary>true mientras está "receloso" (tras un señuelo, un PEM o un banco manipulado).</summary>
        public bool IsWary => Time.time < boostUntil;
        public bool IsBlinded => blinded;

        private Transform player;
        private CharacterController playerController;
        private PlayerStealth playerStealth;
        private readonly Vector3[] samplePoints = new Vector3[PerceptionModel.SamplePointCount];
        private bool isRobot;   // robots: cono horizontal + límite vertical · cámaras: cono 3D

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            if (eye == null) eye = transform;
            // Los robots (con EnemyPatrol) tienen el cono entre 75° y 90°; las cámaras conservan el suyo.
            isRobot = GetComponent<EnemyPatrol>() != null;
            if (isRobot) viewAngle = PerceptionModel.ClampViewAngle(viewAngle);
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.transform;
                playerController = go.GetComponent<CharacterController>();
                playerStealth = go.GetComponent<PlayerStealth>();
            }
            EffectiveViewDistance = ConeDistance;
            if (visionLight != null)
            {
                visionLight.type = LightType.Spot;
                visionLight.spotAngle = viewAngle;
                visionLight.range = ConeDistance;
            }
        }

        private void Update()
        {
            if (blinded || EnemyPatrol.Paused) { CanSeePlayer = false; VisibleRays = 0; return; }

            // Margen tras reaparecer: evita capturas en cadena justo al recuperar el control.
            // (distance se inicializa aquí: el compilador de Unity no deduce que solo se usa cuando CheckLineOfSight se ejecutó)
            float distance = 0f;
            VisibleRays = 0;
            CanSeePlayer = player != null && !Nexus7.Gameplay.PlayerRespawn.InGrace && CheckLineOfSight(out distance);

            if (CanSeePlayer)
            {
                LastSeenPosition = player.position;
                LastSeenVelocity = playerController != null ? playerController.velocity : Vector3.zero;
                float visibility = playerStealth != null ? playerStealth.Visibility : 1f;
                float range = Mathf.Max(0.01f, EffectiveViewDistance);
                float distanceFactor = Mathf.Lerp(2.5f, 0.4f, distance / range);   // de cerca se detecta mucho antes
                float rays = PerceptionModel.DetectionFactor(VisibleRays, PerceptionModel.SamplePointCount);
                float wary = IsWary ? boostMultiplier : 1f;
                float alert = Nexus7.Core.AlertSystem.Profile.detection * Nexus7.Core.DifficultySettings.DetectionMultiplier;
                Awareness += detectionSpeed * visibility * distanceFactor * rays * wary * alert * Time.deltaTime;
            }
            else
            {
                Awareness -= decaySpeed * Time.deltaTime;
            }
            Awareness = Mathf.Clamp01(Awareness);

            if (visionLight != null)
            {
                visionLight.range = ConeDistance;
                visionLight.color = Awareness >= 1f
                    ? alertColor
                    : Color.Lerp(calmColor, suspiciousColor, Mathf.InverseLerp(0f, 0.6f, Awareness));
            }
        }

        private bool CheckLineOfSight(out float distance)
        {
            Vector3 eyePos = eye.position;
            float height = playerController != null ? playerController.height : 1.8f;
            Vector3 chest = player.position + Vector3.up * (height * 0.6f);

            // 1. Alcance: base × alerta × sigilo, con tope duro (y de humo si el robot o el jugador están en humo)
            bool inSmoke = VisionSmoke.Contains(eyePos) || VisionSmoke.Contains(chest);
            float stealth = playerStealth != null ? playerStealth.VisionRangeMultiplier : 1f;
            EffectiveViewDistance = PerceptionModel.EffectiveRange(viewDistance, Nexus7.Core.AlertSystem.Profile.viewDistance,
                                                                   stealth, inSmoke, maxViewDistance, smokeViewDistance);
            Vector3 toPlayer = chest - eyePos;
            distance = toPlayer.magnitude;
            if (distance > EffectiveViewDistance) return false;

            // 2. Cono estricto: fuera del ángulo no ve nada
            bool inCone = isRobot ? PerceptionModel.InViewCone(eye.forward, toPlayer, viewAngle, verticalHalfAngle)
                                  : PerceptionModel.InCone(eye.forward, toPlayer, viewAngle);
            if (!inCone) return false;

            // 3. Oclusión: 5 rayos al cuerpo; basta con uno, pero cuantos más lleguen, más rápido detecta
            int n = PerceptionModel.SamplePoints(player.position, height, eyePos, samplePoints);
            int clear = 0;
            for (int i = 0; i < n; i++)
                if (HasClearLine(eyePos, samplePoints[i])) clear++;
            VisibleRays = clear;
            return clear > 0;
        }

        private bool HasClearLine(Vector3 from, Vector3 to)
        {
            if (VisionSmoke.Blocks(from, to, smokeBlockLength)) return false;   // humo denso de por medio
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
            VisibleRays = 0;
        }

        private void OnDrawGizmosSelected()
        {
            Transform e = eye != null ? eye : transform;
            float half = viewAngle * 0.5f;
            // Cono con el alcance base (amarillo) y el tope duro (rojo)
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawRay(e.position, Quaternion.AngleAxis(-half, Vector3.up) * e.forward * viewDistance);
            Gizmos.DrawRay(e.position, Quaternion.AngleAxis(half, Vector3.up) * e.forward * viewDistance);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.4f);
            Gizmos.DrawRay(e.position, Quaternion.AngleAxis(-half, Vector3.up) * e.forward * maxViewDistance);
            Gizmos.DrawRay(e.position, Quaternion.AngleAxis(half, Vector3.up) * e.forward * maxViewDistance);
            if (Application.isPlaying && player != null)
            {
                Gizmos.color = CanSeePlayer ? Color.red : Color.gray;
                for (int i = 0; i < samplePoints.Length; i++) Gizmos.DrawLine(e.position, samplePoints[i]);
            }
        }
    }
}

using UnityEngine;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.UI;

namespace Nexus7.Security
{
    /// <summary>
    /// Cámara de seguridad: barre de lado a lado, sigue al jugador cuando lo ve y, si su medidor de
    /// alerta (EnemyVision) llega a 1, dispara una ALARMA: los drones en el radio acuden corriendo.
    /// La cámara no persigue ni captura: su peligro es "llamar" a los drones.
    /// Un HackTerminal puede desactivarla temporalmente (IHackable): baja la cabeza y apaga el foco.
    ///
    /// Estructura:
    ///   Raíz (soporte, sin rotación) → "Pivote" (gira en Y, inclinado en X) → "Lente" (Eye de EnemyVision) → Spot Light
    ///   EnemyVision va en la raíz con Eye = Lente y Vision Light = el foco.
    /// </summary>
    public class SecurityCamera : MonoBehaviour, IHackable, IEmpTarget
    {
        [SerializeField] private string hackLabel = "Cámara de seguridad";
        [SerializeField] private Transform pivot;
        [SerializeField] private EnemyVision vision;
        [SerializeField] private Light visionLight;

        [Header("Barrido")]
        [Tooltip("Grados a cada lado de la orientación inicial del pivote.")]
        [SerializeField] private float sweepAngle = 50f;
        [SerializeField] private float sweepSpeed = 22f;
        [SerializeField] private float pauseAtEnds = 1.5f;
        [SerializeField] private float trackSpeed = 70f;

        [Header("Alarma")]
        [SerializeField] private float alarmRadius = 30f;
        [SerializeField] private float alarmCooldown = 8f;
        [SerializeField, TextArea] private string alarmMessage =
            "¡ALARMA! Una cámara de seguridad te ha detectado. Los drones acuden a tu posición.";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip alarmClip;

        private float baseYaw, basePitch, offset, pauseTimer, disabledUntil, nextAlarm = -999f;
        private int direction = 1;
        private bool hacked;

        public string HackLabel => hackLabel;
        public bool IsDisabled => hacked;
        public float RemainingDisabledTime => hacked ? Mathf.Max(0f, disabledUntil - Time.time) : 0f;

        public Vector3 EmpPosition => transform.position;
        public void OnEmp(float duration) => Disable(duration);
        private void OnEnable() => EmpRegistry.Register(this);
        private void OnDisable() => EmpRegistry.Unregister(this);

        private void Start()
        {
            if (pivot == null) pivot = transform;
            if (vision == null) vision = GetComponent<EnemyVision>();
            baseYaw = pivot.localEulerAngles.y;
            basePitch = pivot.localEulerAngles.x;
        }

        private void Update()
        {
            if (hacked)
            {
                if (Time.time >= disabledUntil) Reenable();
                else pivot.localRotation = Quaternion.Slerp(pivot.localRotation,
                         Quaternion.Euler(60f, baseYaw + offset, 0f), 3f * Time.deltaTime); // "cabeza gacha"
                return;
            }

            if (vision != null && vision.CanSeePlayer && vision.Awareness > 0.15f) Track();
            else Sweep();

            pivot.localRotation = Quaternion.Euler(basePitch, baseYaw + offset, 0f);

            if (vision != null && vision.Awareness >= 1f && Time.time >= nextAlarm) RaiseAlarm();
            SetStatus(vision != null && vision.Awareness >= 1f ? Color.red
                    : vision != null && vision.Awareness > 0.05f ? new Color(1f, 0.8f, 0.1f) : new Color(0.3f, 0.8f, 1f));
        }

        private void Sweep()
        {
            if (pauseTimer > 0f) { pauseTimer -= Time.deltaTime; return; }
            offset += direction * sweepSpeed * AlertSystem.Profile.sweepSpeed * Time.deltaTime;
            if (Mathf.Abs(offset) >= sweepAngle)
            {
                offset = Mathf.Sign(offset) * sweepAngle;
                direction = -direction;
                pauseTimer = pauseAtEnds;
            }
        }

        private void Track()
        {
            // Dirección al jugador en el espacio del soporte → ángulo de giro deseado
            Vector3 dir = vision.LastSeenPosition - pivot.position;
            if (pivot.parent != null) dir = pivot.parent.InverseTransformDirection(dir);
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float target = Mathf.Clamp(Mathf.DeltaAngle(baseYaw, yaw), -sweepAngle - 20f, sweepAngle + 20f);
            offset = Mathf.MoveTowards(offset, target, trackSpeed * Time.deltaTime);
        }

        private void RaiseAlarm()
        {
            nextAlarm = Time.time + alarmCooldown;
            float radius = alarmRadius * (AlertSystem.Level == AlertLevel.Red ? 1.5f : 1f);
            AlarmSystem.Raise(vision.LastSeenPosition, radius);
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(alarmMessage, 2.5f);
            if (audioSource != null && alarmClip != null) audioSource.PlayOneShot(alarmClip);
        }

        public void Disable(float duration)
        {
            hacked = true;
            disabledUntil = Mathf.Max(disabledUntil, Time.time + duration);   // un PEM no acorta un apagado más largo
            if (vision != null) { vision.ResetAwareness(); vision.enabled = false; }
            if (visionLight != null) visionLight.enabled = false;
            SetStatus(Color.black);
        }

        private void Reenable()
        {
            hacked = false;
            if (vision != null) vision.enabled = true;
            if (visionLight != null) visionLight.enabled = true;
        }

        private void SetStatus(Color c)
        {
            if (statusRenderer == null) return;
            var mat = statusRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 3f);
        }
    }
}

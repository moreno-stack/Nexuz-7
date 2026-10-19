using UnityEngine;
using Nexus7.Core;
using Nexus7.Hazards;
using Nexus7.Stealth;
using Nexus7.UI;

namespace Nexus7.Security
{
    /// <summary>
    /// Sensor láser óptico. Si el jugador atraviesa los haces → ALARMA (los drones acuden).
    /// Formas de superarlo:
    ///   · Si los haces están altos (zona por encima de 1 m) → pasar agachado.
    ///   · Hackearlo con un HackTerminal (IHackable).
    ///   · Un Pulso PEM lo apaga temporalmente (IEmpTarget).
    ///   · Con el CAMUFLAJE ÓPTICO activo el sensor no te detecta.
    ///
    /// Estructura: raíz con BoxCollider "Is Trigger" que cubre SOLO la altura de los haces (capa Ignore Raycast)
    ///             + hijos con ElectricArc (jitter 0) como haces visibles.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LaserTripwire : MonoBehaviour, IHackable, IEmpTarget
    {
        [SerializeField] private string hackLabel = "Sensor láser";
        [SerializeField] private ElectricArc[] beams;
        [SerializeField] private Light beamLight;
        [SerializeField] private float alarmRadius = 25f;
        [SerializeField] private float alarmCooldown = 6f;
        [SerializeField, TextArea] private string alarmMessage = "¡Has cortado un sensor láser! Alarma activada.";
        [SerializeField] private float reactivationWarning = 3f;

        private Collider zone;
        private CharacterController playerCollider;
        private PlayerStealth stealth;
        private bool disabled;
        private float disabledUntil;
        private float nextAlarm = -999f;

        public string HackLabel => hackLabel;
        public bool IsDisabled => disabled;
        public float RemainingDisabledTime => disabled ? Mathf.Max(0f, disabledUntil - Time.time) : 0f;
        public Vector3 EmpPosition => transform.position;

        private void Awake() => zone = GetComponent<Collider>();
        private void OnEnable() => EmpRegistry.Register(this);
        private void OnDisable() => EmpRegistry.Unregister(this);

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            playerCollider = go.GetComponent<CharacterController>();
            stealth = go.GetComponent<PlayerStealth>();
        }

        private void Update()
        {
            if (disabled)
            {
                float left = disabledUntil - Time.time;
                if (left <= 0f) SetDisabled(false);
                else if (left < reactivationWarning) SetBeams(Mathf.Repeat(Time.time * 4f, 1f) < 0.5f); // aviso
                return;
            }

            bool crossing = playerCollider != null && playerCollider.enabled && zone.bounds.Intersects(playerCollider.bounds);
            bool invisible = stealth != null && stealth.IsCloaked;
            if (crossing && !invisible && Time.time >= nextAlarm)
            {
                nextAlarm = Time.time + alarmCooldown;
                AlarmSystem.Raise(playerCollider.transform.position, alarmRadius);
                if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(alarmMessage, 2.5f);
                if (beamLight != null) beamLight.intensity = 6f;
            }
            if (beamLight != null) beamLight.intensity = Mathf.MoveTowards(beamLight.intensity, 1.5f, 8f * Time.deltaTime);
        }

        public void Disable(float duration)
        {
            disabledUntil = Mathf.Max(disabledUntil, Time.time + duration);   // un PEM no acorta un apagado más largo
            SetDisabled(true);
        }

        public void OnEmp(float duration) => Disable(duration);

        private void SetDisabled(bool value)
        {
            disabled = value;
            SetBeams(!value);
            if (beamLight != null) beamLight.enabled = !value;
        }

        private void SetBeams(bool visible)
        {
            if (beams == null) return;
            foreach (var b in beams) if (b != null) b.SetVisible(visible);
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.3f);
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}

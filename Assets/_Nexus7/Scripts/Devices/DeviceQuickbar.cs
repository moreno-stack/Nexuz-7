using System;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.Stealth;
using Nexus7.UI;

namespace Nexus7.Devices
{
    /// <summary>
    /// Uso de los dispositivos seleccionados en la barra rápida 1–9 (Hotbar):
    ///   mantener [G] / clic izquierdo / Y del mando → apuntar (se dibuja la trayectoria) · soltar → lanzar
    ///   pulsar  [G] / clic → activar los de uso propio (camuflaje, kit de reparación)
    /// La selección de casilla la hace Hotbar ([1]–[9], rueda, LB/RB).
    ///
    /// Colocar en: el GameObject raíz "Player". "Throw Origin" = la cámara.
    /// </summary>
    [RequireComponent(typeof(InventorySystem))]
    public class DeviceQuickbar : MonoBehaviour
    {
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private int trajectorySteps = 40;
        [SerializeField] private Hotbar hotbar;

        [Header("Controles")]
        [SerializeField] private KeyCode useKey = KeyCode.G;
        [SerializeField] private KeyCode useMouse = KeyCode.Mouse0;
        [Tooltip("JoystickButton3 = botón Y (LB/RB cambian de casilla).")]
        [SerializeField] private KeyCode padUseButton = KeyCode.JoystickButton3;

        private InventorySystem inventory;
        private PlayerController controller;
        private PlayerStealth stealth;
        private PlayerHealth health;
        private Collider playerCollider;
        private bool aiming;
        private readonly Vector3[] trajectory = new Vector3[64];

        // ---- Compatibilidad con la UI antigua (QuickbarUI): ahora todo sale de la barra rápida ----
        public int SelectedIndex => hotbar != null ? hotbar.Model.Selected : 0;
        public int SlotCount => hotbar != null ? hotbar.Model.Count : 0;
        public event Action SelectionChanged;
        public DeviceItemData GetSlot(int index) => hotbar != null ? hotbar.Model.Get(index) as DeviceItemData : null;
        public int GetCount(int index) => hotbar != null && hotbar.Model.Get(index) != null ? inventory.GetCount(hotbar.Model.Get(index)) : 0;
        public DeviceItemData Selected => hotbar != null ? hotbar.SelectedItem as DeviceItemData : null;
        public bool IsAiming => aiming;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            if (hotbar == null) hotbar = GetComponent<Hotbar>();
            if (hotbar == null) hotbar = gameObject.AddComponent<Hotbar>();   // escenas antiguas sin barra rápida
            stealth = GetComponent<PlayerStealth>();
            playerCollider = GetComponent<CharacterController>();
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }

        private void Update()
        {
            if (controller != null && !controller.InputEnabled) { CancelAim(); return; }

            var device = Selected;
            bool mouseOk = Cursor.lockState == CursorLockMode.Locked;   // el primer clic solo captura el ratón
            bool down = Input.GetKeyDown(useKey) || (mouseOk && Input.GetKeyDown(useMouse)) || Input.GetKeyDown(padUseButton);
            bool held = Input.GetKey(useKey) || (mouseOk && Input.GetKey(useMouse)) || Input.GetKey(padUseButton);

            if (device == null)
            {
                if (down && hotbar != null && hotbar.SelectedItem != null)
                    PickupNotificationUI.Notify($"{hotbar.SelectedItem.DisplayName}: no se puede usar directamente.", new Color(0.8f, 0.85f, 0.9f));
                return;
            }

            if (down && inventory.GetCount(device) <= 0)
            {
                PickupNotificationUI.Notify($"No tienes {device.DisplayName}. Fabrícalo en un banco de trabajo.", new Color(1f, 0.5f, 0.4f));
                return;
            }

            if (device.UseMode == DeviceUseMode.SelfActivate)
            {
                if (down) Activate(device);
                return;
            }

            // Lanzables: mantener para apuntar, soltar para lanzar.
            if (held && inventory.GetCount(device) > 0)
            {
                aiming = true;
                DrawTrajectory(device);
            }
            else if (aiming)
            {
                CancelAim();
                Throw(device);
            }
        }

        public void Select(int index)
        {
            if (hotbar == null) return;
            hotbar.Select(index);
        }

        private void OnEnable() { if (hotbar != null) hotbar.SelectionChanged += OnHotbarChanged; }
        private void OnDisable() { if (hotbar != null) hotbar.SelectionChanged -= OnHotbarChanged; }
        private void OnHotbarChanged() { CancelAim(); SelectionChanged?.Invoke(); }

        // Se lanza desde el pecho del jugador (también en 3ª persona, donde la cámara está detrás) hacia donde mira la cámara.
        private Vector3 LaunchPosition => transform.position + Vector3.up * 1.35f + Flat(throwOrigin.forward) * 0.45f;
        private Vector3 LaunchVelocity(DeviceItemData d) => (throwOrigin.forward + Vector3.up * 0.25f).normalized * d.ThrowForce;
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; }

        /// <summary>Primer impacto del segmento que no sea el propio jugador.</summary>
        private bool LinecastIgnoringPlayer(Vector3 a, Vector3 b, out RaycastHit hit)
        {
            hit = default(RaycastHit);
            Vector3 d = b - a; float len = d.magnitude;
            if (len < 0.0001f) return false;
            var hits = Physics.RaycastAll(new Ray(a, d / len), len, ~(1 << 2), QueryTriggerInteraction.Ignore);
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (!found || hits[i].distance < hit.distance) { hit = hits[i]; found = true; }
            }
            return found;
        }

        private void Throw(DeviceItemData device)
        {
            if (throwOrigin == null || !inventory.TryRemove(device, 1)) return;
            RunStats.RecordDeviceUse((int)device.Type, device.Type != DeviceKind.RepairKit);   // el kit no cuenta para «tipos distintos» (arsenal = 3)
            DeviceFactory.SpawnThrown(device, LaunchPosition, LaunchVelocity(device), transform.position, playerCollider);
        }

        public const float RepairKitHeal = 40f;

        private void Activate(DeviceItemData device)
        {
            if (device.Type == DeviceKind.RepairKit && health != null && health.Current >= health.Max)
            {
                PickupNotificationUI.Notify("Integridad al máximo: guarda el kit para luego.", device.DeviceColor);
                return;
            }
            if (!inventory.TryRemove(device, 1)) return;
            RunStats.RecordDeviceUse((int)device.Type, device.Type != DeviceKind.RepairKit);   // el kit no cuenta para «tipos distintos» (arsenal = 3)
            switch (device.Type)
            {
                case DeviceKind.RepairKit:
                    if (health != null) health.Heal(RepairKitHeal);
                    PickupNotificationUI.Notify($"Kit de reparación: +{RepairKitHeal:0} de integridad", device.DeviceColor);
                    break;
                case DeviceKind.OpticalCamo:
                    if (stealth != null) stealth.ApplyCloak(device.Duration, device.EffectValue);
                    PickupNotificationUI.Notify($"Camuflaje óptico activo ({device.Duration:0} s)", device.DeviceColor);
                    break;
                default:
                    // Un lanzable configurado como "uso propio": se suelta a los pies.
                    DeviceFactory.SpawnThrown(device, transform.position + Vector3.up * 0.5f, Vector3.zero, transform.position, playerCollider);
                    break;
            }
        }

        private void DrawTrajectory(DeviceItemData device)
        {
            if (trajectoryLine == null || throwOrigin == null) return;
            Vector3 p = LaunchPosition, v = LaunchVelocity(device);
            int count = 0;
            trajectory[count++] = p;
            const float dt = 0.05f;
            int steps = Mathf.Min(trajectorySteps, trajectory.Length - 1);
            for (int i = 0; i < steps; i++)
            {
                Vector3 next = p + v * dt;
                v += Physics.gravity * dt;
                if (LinecastIgnoringPlayer(p, next, out RaycastHit hit))
                {
                    trajectory[count++] = hit.point;
                    break;
                }
                trajectory[count++] = next;
                p = next;
            }
            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = count;
            for (int i = 0; i < count; i++) trajectoryLine.SetPosition(i, trajectory[i]);
            trajectoryLine.startColor = trajectoryLine.endColor = device.DeviceColor;
        }

        private void CancelAim()
        {
            aiming = false;
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }
    }
}

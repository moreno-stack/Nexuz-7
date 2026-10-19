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
    /// Barra de acceso rápido de dispositivos y su uso:
    ///   [1] [2] [3]   → seleccionar ranura (LB en el mando = siguiente)
    ///   mantener [G]  → apuntar (se dibuja la trayectoria) · soltar → lanzar     (RB en el mando)
    ///   pulsar [G]    → activar los dispositivos de uso propio (camuflaje)
    /// Las ranuras son fijas (Slots): muestran el dispositivo aunque tengas 0 para que sepas qué fabricar.
    ///
    /// Colocar en: el GameObject raíz "Player". "Throw Origin" = la cámara.
    /// </summary>
    [RequireComponent(typeof(InventorySystem))]
    public class DeviceQuickbar : MonoBehaviour
    {
        [SerializeField] private DeviceItemData[] slots = new DeviceItemData[3];
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private int trajectorySteps = 40;

        [Header("Controles")]
        [SerializeField] private KeyCode useKey = KeyCode.G;
        [SerializeField] private KeyCode[] slotKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
        [SerializeField] private KeyCode padUse = KeyCode.JoystickButton5;   // RB
        [SerializeField] private KeyCode padCycle = KeyCode.JoystickButton4; // LB

        private InventorySystem inventory;
        private PlayerController controller;
        private PlayerStealth stealth;
        private Collider playerCollider;
        private bool aiming;
        private readonly Vector3[] trajectory = new Vector3[64];

        public int SelectedIndex { get; private set; }
        public int SlotCount => slots.Length;
        public event Action SelectionChanged;

        public DeviceItemData GetSlot(int index) => index >= 0 && index < slots.Length ? slots[index] : null;
        public int GetCount(int index) => GetSlot(index) != null ? inventory.GetCount(GetSlot(index)) : 0;
        public DeviceItemData Selected => GetSlot(SelectedIndex);
        public bool IsAiming => aiming;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            controller = GetComponent<PlayerController>();
            stealth = GetComponent<PlayerStealth>();
            playerCollider = GetComponent<CharacterController>();
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }

        private void Update()
        {
            if (controller != null && !controller.InputEnabled) { CancelAim(); return; }

            for (int i = 0; i < slotKeys.Length && i < slots.Length; i++)
                if (Input.GetKeyDown(slotKeys[i])) Select(i);
            if (Input.GetKeyDown(padCycle)) Select((SelectedIndex + 1) % slots.Length);

            var device = Selected;
            bool down = Input.GetKeyDown(useKey) || Input.GetKeyDown(padUse);
            bool held = Input.GetKey(useKey) || Input.GetKey(padUse);

            if (device == null) return;

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
            if (index < 0 || index >= slots.Length || index == SelectedIndex) return;
            SelectedIndex = index;
            CancelAim();
            SelectionChanged?.Invoke();
        }

        private Vector3 LaunchPosition => throwOrigin.position + throwOrigin.forward * 0.5f - throwOrigin.up * 0.15f;
        private Vector3 LaunchVelocity(DeviceItemData d) => (throwOrigin.forward + Vector3.up * 0.25f).normalized * d.ThrowForce;

        private void Throw(DeviceItemData device)
        {
            if (throwOrigin == null || !inventory.TryRemove(device, 1)) return;
            RunStats.RecordDeviceUse((int)device.Type);
            DeviceFactory.SpawnThrown(device, LaunchPosition, LaunchVelocity(device), transform.position, playerCollider);
        }

        private void Activate(DeviceItemData device)
        {
            if (!inventory.TryRemove(device, 1)) return;
            RunStats.RecordDeviceUse((int)device.Type);
            switch (device.Type)
            {
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
                if (Physics.Linecast(p, next, out RaycastHit hit, ~(1 << 2), QueryTriggerInteraction.Ignore))
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

using System;
using System.Collections.Generic;
using UnityEngine;
using Nexus7.Player;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Barra rápida 1–9 sobre el inventario: [1]…[9] seleccionan, la rueda del ratón o LB/RB pasan de casilla.
    /// El objeto seleccionado se ve en la mano (HeldItemView) y, si es un dispositivo, se usa con [G] o clic (DeviceQuickbar).
    ///
    /// Colocar en: el GameObject raíz "Player" (junto a InventorySystem).
    /// </summary>
    [RequireComponent(typeof(InventorySystem))]
    public class Hotbar : MonoBehaviour
    {
        [SerializeField] private KeyCode padPrevious = KeyCode.JoystickButton4; // LB
        [SerializeField] private KeyCode padNext = KeyCode.JoystickButton5;     // RB

        private static readonly KeyCode[] SlotKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };

        private InventorySystem inventory;
        private PlayerController controller;
        private readonly List<ItemData> order = new List<ItemData>();

        public HotbarModel Model { get; private set; } = new HotbarModel();
        public ItemData SelectedItem => Model.SelectedItem;
        public event Action SelectionChanged;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            controller = GetComponent<PlayerController>();
        }

        private void OnEnable() { if (inventory != null) inventory.Changed += Refresh; }
        private void OnDisable() { if (inventory != null) inventory.Changed -= Refresh; }
        private void Start() => Refresh();

        private void Update()
        {
            if (controller != null && !controller.InputEnabled) return;
            for (int i = 0; i < SlotKeys.Length; i++)
                if (Input.GetKeyDown(SlotKeys[i])) { Select(i); return; }
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (wheel > 0.01f || Input.GetKeyDown(padPrevious)) Cycle(-1);
            else if (wheel < -0.01f || Input.GetKeyDown(padNext)) Cycle(1);
        }

        public void Select(int index)
        {
            var before = Model.SelectedItem;
            Model.Select(index);
            if (Model.SelectedItem != before) SelectionChanged?.Invoke();
        }

        public void Cycle(int dir)
        {
            var before = Model.SelectedItem;
            Model.Cycle(dir);
            if (Model.SelectedItem != before) SelectionChanged?.Invoke();
        }

        /// <summary>Reconstruye las casillas a partir del inventario (orden de llegada).</summary>
        public void Refresh()
        {
            if (inventory == null) return;
            var before = Model.SelectedItem;
            order.Clear();
            foreach (var slot in inventory.Slots) if (slot.Item != null) order.Add(slot.Item);
            Model.Rebuild(order);
            if (Model.SelectedItem != before) SelectionChanged?.Invoke();
        }
    }
}

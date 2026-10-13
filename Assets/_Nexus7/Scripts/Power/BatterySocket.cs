using UnityEngine;
using Nexus7.Interaction;
using Nexus7.Inventory;

namespace Nexus7.Power
{
    /// <summary>
    /// Zócalo de batería: consume UNA batería del inventario (p. ej. las traídas del Almacén de Carga)
    /// y aporta su energía a la red. La batería aparece físicamente encajada en el zócalo.
    ///
    /// Colocar en: la carcasa del zócalo (objeto con collider).
    /// </summary>
    public class BatterySocket : PowerSource
    {
        [SerializeField] private ItemData batteryItem;
        [Tooltip("Modelo de la batería encajada (oculto hasta conectarla).")]
        [SerializeField] private GameObject batteryVisual;

        public override string Prompt
        {
            get
            {
                if (batteryItem == null) return "Zócalo de batería";
                var inv = ItemRequirementUtil.PlayerInventory;
                int have = inv != null ? inv.GetCount(batteryItem) : 0;
                return $"Conectar {batteryItem.DisplayName} (llevas {have})";
            }
        }

        protected override void Start()
        {
            base.Start();
            if (batteryVisual != null) batteryVisual.SetActive(false);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var inventory = interactor.GetComponentInParent<InventorySystem>();
            if (batteryItem != null && inventory != null && inventory.TryRemove(batteryItem, 1))
            {
                Activate();
                return;
            }
            Deny($"Zócalo vacío. Necesitas una {(batteryItem != null ? batteryItem.DisplayName : "batería")}.");
        }

        protected override void OnActivated()
        {
            if (batteryVisual != null) batteryVisual.SetActive(true);
        }
    }
}

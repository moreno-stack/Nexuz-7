using System.Collections.Generic;
using UnityEngine;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.UI;

namespace Nexus7.Hazards
{
    /// <summary>
    /// Panel de derivación junto a un circuito dañado: gastando recursos (por defecto 2 de chatarra)
    /// aísla el circuito (ElectricHazard.Repair) y abre esa ruta de forma segura y permanente.
    ///
    /// Colocar en: un objeto con collider cerca de la trampa.
    /// </summary>
    public class RepairPanel : Interactable
    {
        [SerializeField] private ElectricHazard target;
        [SerializeField] private List<ItemStack> cost = new List<ItemStack>();
        [SerializeField] private string actionLabel = "Aislar circuito dañado";
        [SerializeField, TextArea] private string repairedMessage = "Circuito aislado. La zona ya es segura.";
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Color repairedColor = new Color(0.15f, 1f, 0.35f);

        private float lastDeniedTime = -999f;

        public override string Prompt =>
            target != null && target.IsRepaired ? "Circuito aislado"
            : ItemRequirementUtil.HasAny(cost) ? $"{actionLabel} ({ItemRequirementUtil.Describe(cost)})"
            : actionLabel;

        public override bool CanInteract(PlayerInteractor interactor) =>
            target != null && !target.IsRepaired && base.CanInteract(interactor);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var inventory = interactor.GetComponentInParent<InventorySystem>();
            if (inventory == null || !inventory.TryConsume(cost))
            {
                if (Time.time - lastDeniedTime < 1.5f) return;
                lastDeniedTime = Time.time;
                if (HUDManager.Instance != null)
                    HUDManager.Instance.QueueMessage("Necesitas: " + ItemRequirementUtil.Describe(cost, inventory), 2.5f);
                return;
            }

            target.Repair();
            if (HUDManager.Instance != null && !string.IsNullOrEmpty(repairedMessage)) HUDManager.Instance.QueueMessage(repairedMessage);
            if (screenRenderer != null)
            {
                var mat = screenRenderer.material;
                mat.color = repairedColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", repairedColor * 2f);
            }
        }
    }
}

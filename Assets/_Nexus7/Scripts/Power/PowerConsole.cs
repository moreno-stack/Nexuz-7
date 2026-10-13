using System.Collections.Generic;
using UnityEngine;
using Nexus7.Interaction;
using Nexus7.Inventory;

namespace Nexus7.Power
{
    /// <summary>
    /// Consola / interruptor que aporta energía al activarse. Configurable para cubrir varios casos:
    ///   · Interruptor general: sin requisitos.
    ///   · Consola dañada: requiere objetos (p. ej. 1 Circuito de control) que se consumen al repararla.
    ///   · Reactor: requiere que la red ya tenga cierto nivel de energía (Required Grid Power) para arrancar.
    ///
    /// Colocar en: el cuerpo de la consola (objeto con collider).
    /// </summary>
    public class PowerConsole : PowerSource
    {
        [Header("Requisitos")]
        [SerializeField] private string actionLabel = "Activar consola";
        [Tooltip("Energía mínima de la red para poder activarla (0 = ninguna).")]
        [SerializeField, Min(0)] private int requiredGridPower = 0;
        [SerializeField] private List<ItemStack> requiredItems = new List<ItemStack>();
        [SerializeField] private bool consumeItems = true;

        [Header("Efectos al activarse (opcional)")]
        [Tooltip("Renderer que se ilumina (pantalla, núcleo del reactor...).")]
        [SerializeField] private Renderer glowRenderer;
        [SerializeField] private Color glowColor = new Color(0.3f, 0.8f, 1f);

        public override string Prompt
        {
            get
            {
                if (PowerGrid.Power < requiredGridPower)
                    return $"{actionLabel} (energía insuficiente {PowerGrid.Power}/{requiredGridPower})";
                if (ItemRequirementUtil.HasAny(requiredItems))
                    return $"{actionLabel} ({ItemRequirementUtil.Describe(requiredItems)})";
                return actionLabel;
            }
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (PowerGrid.Power < requiredGridPower)
            {
                Deny($"La consola no arranca: necesita {requiredGridPower} bloques de energía en la red (hay {PowerGrid.Power}).");
                return;
            }

            if (ItemRequirementUtil.HasAny(requiredItems))
            {
                var inventory = interactor.GetComponentInParent<InventorySystem>();
                bool ok = inventory != null && (consumeItems ? inventory.TryConsume(requiredItems) : inventory.HasAll(requiredItems));
                if (!ok)
                {
                    Deny("Consola dañada. Necesitas: " + ItemRequirementUtil.Describe(requiredItems, inventory));
                    return;
                }
            }
            Activate();
        }

        protected override void OnActivated()
        {
            if (glowRenderer == null) return;
            var mat = glowRenderer.material;
            mat.color = glowColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * 4f);
        }
    }
}

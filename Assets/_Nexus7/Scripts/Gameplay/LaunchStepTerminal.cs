using System.Collections.Generic;
using UnityEngine;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.Security;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Un paso de la secuencia de lanzamiento. Dos maneras de completarlo:
    ///   · con RECURSOS (Required Items, p. ej. 1 batería como celda de ignición), o
    ///   · con un HACKEO (si Allow Hack está activo): minijuego de la dificultad indicada.
    /// Si llevas los recursos se usan primero (más seguro); si no, se ofrece el hackeo (más arriesgado, sube la alerta si fallas).
    ///
    /// Colocar en: un panel o consola con collider. Arrástralo a LaunchSequence → Steps.
    /// </summary>
    public class LaunchStepTerminal : Interactable
    {
        [SerializeField] private string stepName = "Paso de lanzamiento";
        [SerializeField] private LaunchSequence sequence;
        [SerializeField] private List<ItemStack> requiredItems = new List<ItemStack>();
        [SerializeField] private bool allowHack = true;
        [SerializeField] private HackDifficulty hackDifficulty = HackDifficulty.Medium;
        [SerializeField] private float lockoutTime = 8f;
        [SerializeField] private Renderer statusRenderer;

        private float lockedUntil = -999f;
        private PlayerController lockedController;

        public string StepName => stepName;
        public bool IsCompleted { get; private set; }

        private bool HasItems(InventorySystem inv) => ItemRequirementUtil.HasAny(requiredItems) && inv != null && inv.HasAll(requiredItems);

        public override string Prompt
        {
            get
            {
                if (IsCompleted) return $"{stepName}: completado";
                if (Time.time < lockedUntil) return $"{stepName}: bloqueado ({Mathf.CeilToInt(lockedUntil - Time.time)} s)";
                if (HasItems(ItemRequirementUtil.PlayerInventory)) return $"{stepName} ({ItemRequirementUtil.Describe(requiredItems)})";
                if (allowHack)
                {
                    string d = hackDifficulty == HackDifficulty.Easy ? "fácil" : hackDifficulty == HackDifficulty.Medium ? "media" : "difícil";
                    string alt = ItemRequirementUtil.HasAny(requiredItems) ? $" — sin {ItemRequirementUtil.DescribeCost(requiredItems)}: hackear ({d})" : $" — hackear ({d})";
                    return stepName + alt;
                }
                return $"{stepName} (necesitas {ItemRequirementUtil.Describe(requiredItems)})";
            }
        }

        public override bool CanInteract(PlayerInteractor interactor) =>
            !IsCompleted && Time.time >= lockedUntil && base.CanInteract(interactor);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var inventory = interactor.GetComponentInParent<InventorySystem>();
            if (HasItems(inventory) && inventory.TryConsume(requiredItems)) { Complete(); return; }

            if (!allowHack || HackMinigameUI.Instance == null)
            {
                if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage("Necesitas: " + ItemRequirementUtil.Describe(requiredItems, inventory), 2.5f);
                return;
            }

            lockedController = interactor.GetComponentInParent<PlayerController>();
            if (lockedController != null) lockedController.AddInputLock(this);
            HackMinigameUI.Instance.Begin(hackDifficulty, stepName, transform.position, success =>
            {
                if (lockedController != null) lockedController.RemoveInputLock(this);
                if (success) Complete();
                else
                {
                    lockedUntil = Time.time + lockoutTime;
                    PickupNotificationUI.Notify($"{stepName}: hackeo fallido", new Color(1f, 0.35f, 0.3f));
                }
            });
        }

        private void Complete()
        {
            IsCompleted = true;
            if (statusRenderer != null)
            {
                var m = statusRenderer.material;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 3f);
            }
            if (sequence != null) sequence.NotifyStepCompleted(this);
        }
    }
}

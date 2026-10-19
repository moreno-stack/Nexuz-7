using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Gameplay;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.Power;
using Nexus7.UI;

namespace Nexus7.Security
{
    /// <summary>
    /// Terminal de hackeo: gastando recursos (por defecto 1 Circuito de control) desactiva temporalmente
    /// uno o varios sistemas IHackable (cámaras, barreras).
    ///
    /// El hackeo NO es instantáneo: el jugador queda inmóvil 'Hack Time' segundos con una barra de progreso,
    /// expuesto a los drones (si te capturan, se cancela y no se gastan los recursos).
    /// El tiempo restante de cada sistema desactivado se muestra en el HUD (PowerStatusUI).
    ///
    /// Colocar en: un objeto con collider. En 'Targets' arrastra los componentes SecurityCamera / ElectricBarrier.
    /// </summary>
    public class HackTerminal : Interactable
    {
        /// <summary>Terminales activas (para el HUD).</summary>
        public static readonly List<HackTerminal> All = new List<HackTerminal>();

        [Tooltip("Componentes que implementan IHackable (SecurityCamera, ElectricBarrier...).")]
        [SerializeField] private MonoBehaviour[] targets;
        [SerializeField] private List<ItemStack> cost = new List<ItemStack>();
        [SerializeField] private float hackTime = 2.5f;
        [SerializeField] private float disableDuration = 25f;
        [Tooltip("Energía mínima de la red para usar la terminal (0 = tiene batería propia).")]
        [SerializeField, Min(0)] private int requiredGridPower = 0;
        [SerializeField] private float hackNoiseRadius = 2f;
        [SerializeField] private string actionLabel = "Hackear";

        [Header("Pantalla (opcional)")]
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Color idleColor = new Color(0.2f, 0.9f, 0.4f);
        [SerializeField] private Color hackedColor = new Color(0.2f, 0.6f, 1f);

        private bool hacking;
        private float lastDeniedTime = -999f;

        public IEnumerable<IHackable> Targets => targets == null ? Enumerable.Empty<IHackable>() : targets.OfType<IHackable>();
        public string TargetLabel => string.Join(" + ", Targets.Select(t => t.HackLabel));
        public bool TargetsDisabled => Targets.Any(t => t.IsDisabled);
        public float RemainingTime => Targets.Select(t => t.RemainingDisabledTime).DefaultIfEmpty(0f).Max();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public override string Prompt
        {
            get
            {
                if (hacking) return "Hackeando...";
                if (PowerGrid.Power < requiredGridPower) return $"{actionLabel} {TargetLabel} (terminal sin energía)";
                string c = ItemRequirementUtil.HasAny(cost) ? $" ({ItemRequirementUtil.Describe(cost)})" : "";
                return $"{actionLabel} {TargetLabel}{c}";
            }
        }

        public override bool CanInteract(PlayerInteractor interactor) =>
            !hacking && !TargetsDisabled && base.CanInteract(interactor);

        private void Update()
        {
            if (screenRenderer == null) return;
            Color c = TargetsDisabled ? hackedColor : idleColor;
            screenRenderer.material.SetColor("_EmissionColor", c * 2f);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var inventory = interactor.GetComponentInParent<InventorySystem>();
            if (PowerGrid.Power < requiredGridPower)
            {
                Deny($"Terminal sin energía (necesita {requiredGridPower} bloques en la red).");
                return;
            }
            if (inventory == null || !inventory.HasAll(cost))
            {
                Deny("Recursos insuficientes. Necesitas: " + ItemRequirementUtil.Describe(cost, inventory));
                return;
            }
            StartCoroutine(HackRoutine(interactor, inventory));
        }

        private IEnumerator HackRoutine(PlayerInteractor interactor, InventorySystem inventory)
        {
            hacking = true;
            var controller = interactor.GetComponentInParent<PlayerController>();
            var respawn = interactor.GetComponentInParent<PlayerRespawn>();
            var hud = HUDManager.Instance;
            if (controller != null) controller.AddInputLock(this);
            NoiseSystem.Emit(transform.position, hackNoiseRadius, gameObject);

            for (float t = 0f; t < hackTime; t += Time.deltaTime)
            {
                if (respawn != null && respawn.IsRespawning) // capturado durante el hackeo → se cancela
                {
                    Finish(controller, hud);
                    yield break;
                }
                int pct = Mathf.RoundToInt(100f * t / hackTime);
                if (hud != null) hud.SetHint($"HACKEANDO {TargetLabel.ToUpperInvariant()}...  {pct}%");
                yield return null;
            }

            Finish(controller, hud);

            if (!inventory.TryConsume(cost)) { Deny("El hackeo ha fallado: faltan recursos."); yield break; }
            RunStats.RecordHackSuccess();
            float duration = disableDuration * AlertSystem.Profile.hackDuration; // en alerta, la seguridad se recupera antes
            foreach (var target in Targets) target.Disable(duration);
            PickupNotificationUI.Notify($"{TargetLabel}: desactivada {duration:0} s", new Color(0.4f, 0.8f, 1f));
        }

        private void Finish(PlayerController controller, HUDManager hud)
        {
            hacking = false;
            if (controller != null) controller.RemoveInputLock(this);
            if (hud != null) hud.ClearHint();
        }

        private void Deny(string message)
        {
            if (Time.time - lastDeniedTime < 1.5f) return;
            lastDeniedTime = Time.time;
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(message, 2.5f);
        }
    }
}

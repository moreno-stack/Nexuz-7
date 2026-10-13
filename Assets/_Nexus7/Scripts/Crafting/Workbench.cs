using UnityEngine;
using Nexus7.Core;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Crafting
{
    /// <summary>
    /// Banco de trabajo: al interactuar abre la interfaz de fabricación (CraftingUI).
    /// Fabricar hace ruido (herramientas) y deja el banco "manipulado": si un dron de investigación lo
    /// inspecciona poco después (Waypoint → Inspect Target), da la alarma. ¡Fabrica y aléjate!
    ///
    /// Colocar en: la raíz del banco (el collider puede estar en un hijo).
    /// "Inspection Point" = dónde se coloca el dron para revisarlo (opcional).
    /// </summary>
    public class Workbench : Interactable, IInspectable
    {
        [SerializeField] private string stationName = "banco de trabajo";
        [SerializeField] private Transform inspectionPoint;
        [Tooltip("Ruido de las herramientas al fabricar.")]
        [SerializeField] private float craftNoiseRadius = 5f;
        [SerializeField] private Renderer screenRenderer;

        private float lastUsedTime = -999f;
        private bool tampered;

        public string InspectionLabel => $"el {stationName}";
        public Vector3 InspectionPosition => inspectionPoint != null ? inspectionPoint.position : transform.position;

        public override string Prompt => $"Usar {stationName} (fabricar)";

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var crafting = interactor.GetComponentInParent<CraftingSystem>();
            if (crafting == null) { Debug.LogWarning("[Workbench] El jugador no tiene CraftingSystem.", this); return; }
            if (CraftingUI.Instance != null) CraftingUI.Instance.Open(this, crafting);
        }

        /// <summary>Lo llama la UI al empezar a fabricar.</summary>
        public void RegisterCraft()
        {
            lastUsedTime = Time.time;
            tampered = true;
            NoiseSystem.Emit(transform.position, craftNoiseRadius, gameObject);
        }

        public bool WasTamperedRecently(float window) => tampered && Time.time - lastUsedTime <= window;
        public void ClearTamper() => tampered = false;

        private void Update()
        {
            if (screenRenderer == null) return;
            // La pantalla parpadea en ámbar mientras el banco está "caliente" (manipulado recientemente).
            bool hot = WasTamperedRecently(45f) && Mathf.Repeat(Time.time * 2f, 1f) < 0.5f;
            screenRenderer.material.SetColor("_EmissionColor", (hot ? new Color(1f, 0.6f, 0.1f) : new Color(0.2f, 0.7f, 1f)) * 1.5f);
        }
    }
}

using UnityEngine;
using Nexus7.Core;
using Nexus7.Inventory;

namespace Nexus7.Crafting
{
    /// <summary>
    /// Punto genérico que un dron puede inspeccionar (escritorio, armario...). Se considera "manipulado"
    /// cuando se abre el LootContainer vigilado o cuando otro script llama a MarkTampered().
    ///
    /// Colocar en: un GameObject vacío donde debe situarse el dron. Arrástralo a Waypoint → Inspect Target.
    /// </summary>
    public class InspectableSpot : MonoBehaviour, IInspectable
    {
        [SerializeField] private string label = "el despacho";
        [Tooltip("Contenedor vigilado: abrirlo cuenta como manipulación.")]
        [SerializeField] private LootContainer watchedContainer;

        private float tamperTime = -999f;
        private bool tampered;
        private bool wasOpen;

        public string InspectionLabel => label;
        public Vector3 InspectionPosition => transform.position;

        private void Update()
        {
            if (watchedContainer == null) return;
            if (watchedContainer.IsOpen && !wasOpen) MarkTampered();
            wasOpen = watchedContainer.IsOpen;
        }

        public void MarkTampered()
        {
            tampered = true;
            tamperTime = Time.time;
        }

        public bool WasTamperedRecently(float window) => tampered && Time.time - tamperTime <= window;
        public void ClearTamper() => tampered = false;
    }
}

using UnityEngine;
using Nexus7.Inventory;
using Nexus7.Visual.Models;

namespace Nexus7.Player
{
    /// <summary>
    /// Muestra en la mano del jugador el modelo del objeto seleccionado en la barra rápida (ModelLibrary):
    /// batería, circuito, llave, tarjeta, señuelo, PEM... con su lado mayor de 'size' metros.
    ///
    /// Colocar en: el GameObject raíz "Player". 'Hand Socket' = la mano derecha del modelo (o un punto delante de la cámara).
    /// </summary>
    public class HeldItemView : MonoBehaviour
    {
        [SerializeField] private Hotbar hotbar;
        [SerializeField] private Transform handSocket;
        [SerializeField] private float size = 0.16f;

        private GameObject current;

        public Transform HandSocket { get => handSocket; set { handSocket = value; Rebuild(); } }

        private void Awake() { if (hotbar == null) hotbar = GetComponent<Hotbar>(); }
        private void OnEnable() { if (hotbar != null) hotbar.SelectionChanged += Rebuild; }
        private void OnDisable() { if (hotbar != null) hotbar.SelectionChanged -= Rebuild; }
        private void Start() => Rebuild();

        public void Rebuild()
        {
            if (current != null) Destroy(current);
            current = null;
            var item = hotbar != null ? hotbar.SelectedItem : null;
            if (item == null || handSocket == null) return;

            // Modelo del objeto (ModelLibrary) con su lado mayor = 'size' (los materiales son compartidos: no se destruyen)
            var bp = ItemModels.For(item);
            var mn = bp.MinCorner(); var mx = bp.MaxCorner();
            float largest = System.Math.Max(mx.x - mn.x, System.Math.Max(mx.y - mn.y, mx.z - mn.z));
            if (largest > 1e-4f) bp = bp.Scaled(size / largest);
            var model = ModelFactory.Build(bp, handSocket, false);
            model.name = "ObjetoEnMano";
            model.localPosition = new Vector3(0f, 0f, size * 0.6f);
            current = model.gameObject;
        }
    }
}

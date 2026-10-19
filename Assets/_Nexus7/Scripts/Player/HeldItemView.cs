using UnityEngine;
using Nexus7.Devices;
using Nexus7.Inventory;

namespace Nexus7.Player
{
    /// <summary>
    /// Muestra en la mano del jugador una forma sencilla del objeto seleccionado en la barra rápida:
    /// cilindro (baterías y dispositivos), caja (piezas y componentes) o cápsula (herramientas).
    ///
    /// Colocar en: el GameObject raíz "Player". 'Hand Socket' = la mano derecha del modelo (o un punto delante de la cámara).
    /// </summary>
    public class HeldItemView : MonoBehaviour
    {
        [SerializeField] private Hotbar hotbar;
        [SerializeField] private Transform handSocket;
        [SerializeField] private float size = 0.16f;

        private GameObject current;
        private static Material sharedMaterial;

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

            PrimitiveType shape = item.Category == ItemCategory.Tool ? PrimitiveType.Capsule
                                : item.Category == ItemCategory.Battery || item.Category == ItemCategory.Device ? PrimitiveType.Cylinder
                                : PrimitiveType.Cube;
            current = GameObject.CreatePrimitive(shape);
            current.name = "ObjetoEnMano";
            var col = current.GetComponent<Collider>();
            if (col != null) Destroy(col);
            current.transform.SetParent(handSocket, false);
            current.transform.localPosition = new Vector3(0f, 0f, size * 0.6f);
            current.transform.localScale = shape == PrimitiveType.Cube ? new Vector3(size, size * 0.6f, size)
                                         : new Vector3(size * 0.7f, size * 0.6f, size * 0.7f);

            var device = item as DeviceItemData;
            Color c = device != null ? device.DeviceColor
                    : item.Category == ItemCategory.Battery ? new Color(0.3f, 0.9f, 0.4f)
                    : item.Category == ItemCategory.Component ? new Color(0.3f, 0.6f, 1f)
                    : new Color(0.6f, 0.62f, 0.66f);
            var r = current.GetComponent<Renderer>();
            if (r != null)
            {
                if (sharedMaterial == null) sharedMaterial = r.sharedMaterial;
                var m = new Material(sharedMaterial) { color = c };
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 0.4f);
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Nexus7.UI
{
    /// <summary>
    /// Pone a una Image (o RawImage) la forma generada por código: círculo, anillo, estrella, viñeta,
    /// degradado o líneas de escaneo (estas últimas repetidas en mosaico sobre una RawImage).
    /// Los sprites se crean al cargar la escena: así no hace falta guardar imágenes en el proyecto.
    ///
    /// Colocar en: el mismo objeto que la Image/RawImage (lo hacen los constructores de escena).
    /// </summary>
    [ExecuteAlways]
    public class UIShape : MonoBehaviour
    {
        [SerializeField] private UIShapeKind kind = UIShapeKind.Circle;
        [Tooltip("Solo líneas de escaneo: píxeles de pantalla por cada repetición de la textura.")]
        [SerializeField] private float tilePixels = 4f;

        public UIShapeKind Kind => kind;

        private void OnEnable() => Apply();

        // El mosaico de las líneas de escaneo depende del tamaño final del rectángulo
        private void OnRectTransformDimensionsChange() { if (kind == UIShapeKind.Scanlines) Apply(); }

        public void Apply()
        {
            var img = GetComponent<Image>();
            if (img != null) { img.sprite = ProceduralSprites.Get(kind); img.preserveAspect = kind == UIShapeKind.Circle || kind == UIShapeKind.Ring || kind == UIShapeKind.Star; return; }
            var raw = GetComponent<RawImage>();
            if (raw == null) return;
            raw.texture = ProceduralSprites.Texture(kind);
            if (kind == UIShapeKind.Scanlines)
            {
                var r = ((RectTransform)transform).rect;
                if (r.width <= 0f || r.height <= 0f) return;
                float n = ProceduralSprites.SizeOf(kind) * Mathf.Max(1f, tilePixels / 4f);
                raw.uvRect = new Rect(0f, 0f, Mathf.Max(1f, r.width / n), Mathf.Max(1f, r.height / n));
            }
        }
    }
}

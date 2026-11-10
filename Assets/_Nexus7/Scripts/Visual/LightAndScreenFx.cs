using UnityEngine;

namespace Nexus7.Visual
{
    /// <summary>
    /// Luz que parpadea con un estilo (FlickerModel): zumbido de fluorescente, averiada o de emergencia.
    /// Si tiene un renderer emisivo asignado (la lámpara), su brillo parpadea a la vez.
    /// Solo se añade a luces que no controla ningún otro script (lo comprueba VisualUpgrade).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private FlickerStyle style = FlickerStyle.Hum;
        [SerializeField] private Renderer emissiveRenderer;
        [Tooltip("Semilla del parpadeo (negativa = se calcula con la posición, para que cada luz sea distinta).")]
        [SerializeField] private float seed = -1f;

        private Light lightSource;
        private float baseIntensity;
        private Color baseEmission;
        private MaterialPropertyBlock block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public FlickerStyle Style => style;

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource.intensity;
            if (seed < 0f) seed = Mathf.Abs(transform.position.x * 13.1f + transform.position.z * 7.7f) % 100f;
            if (emissiveRenderer != null)
            {
                block = new MaterialPropertyBlock();
                var mat = emissiveRenderer.sharedMaterial;
                baseEmission = mat != null && mat.HasProperty(EmissionId) ? mat.GetColor(EmissionId) : Color.white;
            }
        }

        private void Update()
        {
            float f = FlickerModel.Sample(style, Time.time, seed);
            lightSource.intensity = baseIntensity * f;
            if (emissiveRenderer != null)
            {
                emissiveRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionId, baseEmission * f);
                emissiveRenderer.SetPropertyBlock(block);
            }
        }
    }

    /// <summary>
    /// Pantalla o panel emisivo que parpadea de forma realista: casi siempre estable, con temblores y apagones cortos.
    /// Usa MaterialPropertyBlock: no crea materiales nuevos.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class EmissiveFlicker : MonoBehaviour
    {
        [SerializeField] private FlickerStyle style = FlickerStyle.Faulty;
        [SerializeField] private float seed = -1f;

        private Renderer target;
        private Color baseEmission;
        private MaterialPropertyBlock block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            var mat = target.sharedMaterial;
            baseEmission = mat != null && mat.HasProperty(EmissionId) ? mat.GetColor(EmissionId) : Color.black;
            if (seed < 0f) seed = Mathf.Abs(transform.position.x * 3.7f + transform.position.y * 5.3f + transform.position.z * 1.9f) % 100f;
        }

        private void Update()
        {
            target.GetPropertyBlock(block);
            block.SetColor(EmissionId, baseEmission * FlickerModel.Sample(style, Time.time, seed));
            target.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// Holograma con fallo digital (GlitchModel): saltos laterales, estiramientos, cortes completos,
    /// brillo irregular y líneas de escaneo que suben (desplaza la textura con _BaseMap_ST).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class HoloGlitch : MonoBehaviour
    {
        [SerializeField] private float seed = -1f;
        [Tooltip("Velocidad con la que suben las líneas de la textura (repeticiones por segundo).")]
        [SerializeField] private float scrollSpeed = 0.25f;

        private Renderer target;
        private Vector3 basePos, baseScale;
        private Color baseEmission;
        private MaterialPropertyBlock block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private Color baseColor;

        private void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            var mat = target.sharedMaterial;
            baseEmission = mat != null && mat.HasProperty(EmissionId) ? mat.GetColor(EmissionId) : Color.black;
            baseColor = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            if (seed < 0f) seed = Mathf.Abs(transform.position.x * 2.3f + transform.position.z * 4.1f) % 100f;
        }

        private void Update()
        {
            var g = GlitchModel.Sample(Time.time, seed);
            target.enabled = g.visible;
            transform.localPosition = basePos + transform.localRotation * new Vector3(g.offsetX * baseScale.x, 0f, 0f);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * g.scaleY, baseScale.z);
            target.GetPropertyBlock(block);
            block.SetColor(EmissionId, baseEmission * g.intensity);
            block.SetColor(BaseColorId, baseColor * g.intensity);
            block.SetVector(BaseMapStId, new Vector4(1f, 1f, 0f, Mathf.Repeat(Time.time * scrollSpeed, 1f)));
            target.SetPropertyBlock(block);
        }
    }
}

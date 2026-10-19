using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Environment
{
    /// <summary>
    /// Pantalla de monitorización cuyo brillo y color dependen del nivel de alerta:
    /// en Normal muestra datos azules con "refresco"; en Amarilla parpadea en ámbar;
    /// en Roja destella en rojo con interferencias. Son la principal fuente de luz dramática de la sala.
    ///
    /// Colocar en: un objeto con Renderer cuyo material tenga emisión. Opcional: una Light cercana que la acompañe.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class AlertScreen : MonoBehaviour
    {
        [SerializeField] private Color normalColor = new Color(0.2f, 0.65f, 1f);
        [SerializeField] private Color yellowColor = new Color(1f, 0.65f, 0.1f);
        [SerializeField] private Color redColor = new Color(1f, 0.08f, 0.05f);
        [SerializeField] private float intensity = 2f;
        [SerializeField] private Light screenLight;
        [SerializeField] private float lightIntensity = 1.2f;

        private Renderer target;
        private MaterialPropertyBlock block;
        private Color current;
        private float seed;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            current = normalColor;
            seed = Random.value * 10f;
        }

        private void Update()
        {
            var level = AlertSystem.Level;
            Color goal = level == AlertLevel.Red ? redColor : level == AlertLevel.Yellow ? yellowColor : normalColor;
            current = Color.Lerp(current, goal, 2.5f * Time.deltaTime);

            float f;
            switch (level)
            {
                case AlertLevel.Red:
                    f = Mathf.Repeat(Time.time * 2.5f + seed, 1f) < 0.5f ? 1.3f : 0.25f;
                    if (Random.value < 0.04f) f = 0.05f; // interferencia
                    break;
                case AlertLevel.Yellow:
                    f = 0.6f + 0.4f * Mathf.Sin((Time.time + seed) * 4f);
                    break;
                default:
                    f = 0.85f + 0.15f * Mathf.Sin((Time.time + seed) * 12f); // refresco de datos
                    break;
            }

            target.GetPropertyBlock(block);
            block.SetColor(EmissionId, current * (intensity * f));
            target.SetPropertyBlock(block);

            if (screenLight != null)
            {
                screenLight.color = current;
                screenLight.intensity = lightIntensity * f;
            }
        }
    }
}

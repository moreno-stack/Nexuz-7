using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Environment
{
    /// <summary>
    /// Luz que sigue el nivel de alerta de la nave:
    ///   Normal   → azul frío, estable
    ///   Amarilla → ámbar, pulso lento
    ///   Roja     → rojo, pulso rápido tipo sirena
    /// Opcionalmente sincroniza un material emisivo (lámpara, franja de techo).
    ///
    /// Colocar en: un GameObject con Light.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class AlertLight : MonoBehaviour
    {
        [SerializeField] private Color normalColor = new Color(0.55f, 0.78f, 1f);
        [SerializeField] private Color yellowColor = new Color(1f, 0.68f, 0.18f);
        [SerializeField] private Color redColor = new Color(1f, 0.08f, 0.04f);
        [SerializeField] private float baseIntensity = 1.6f;
        [SerializeField] private float yellowPulse = 0.8f;   // ciclos/s
        [SerializeField] private float redPulse = 2.2f;
        [SerializeField] private float transitionSpeed = 3f;
        [SerializeField] private Renderer emissiveRenderer;
        [SerializeField] private float emissionStrength = 2.5f;

        private Light lightSource;
        private MaterialPropertyBlock block;
        private Color current;
        private float phase;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            block = new MaterialPropertyBlock();
            current = normalColor;
            phase = Random.value; // que no pulsen todas a la vez
        }

        private void Update()
        {
            var level = AlertSystem.Level;
            Color target = level == AlertLevel.Red ? redColor : level == AlertLevel.Yellow ? yellowColor : normalColor;
            float freq = level == AlertLevel.Red ? redPulse : level == AlertLevel.Yellow ? yellowPulse : 0f;
            float factor = freq > 0f ? 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin((Time.time * freq + phase) * Mathf.PI * 2f)) : 1f;

            current = Color.Lerp(current, target, transitionSpeed * Time.deltaTime);
            lightSource.color = current;
            lightSource.intensity = baseIntensity * factor;

            if (emissiveRenderer == null) return;
            emissiveRenderer.GetPropertyBlock(block);
            block.SetColor(EmissionId, current * (emissionStrength * factor));
            emissiveRenderer.SetPropertyBlock(block);
        }
    }
}

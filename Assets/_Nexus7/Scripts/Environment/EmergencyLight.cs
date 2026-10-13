using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Luz de emergencia que parpadea (ruido Perlin + apagones breves aleatorios) o pulsa como alarma.
    /// Opcionalmente sincroniza la emisión del material de la lámpara para que el "foco" también parpadee.
    ///
    /// Colocar en: un GameObject con componente Light (Point o Spot).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class EmergencyLight : MonoBehaviour
    {
        public enum Mode { Flicker, Pulse }

        [SerializeField] private Mode mode = Mode.Flicker;
        [SerializeField] private float baseIntensity = 2.5f;

        [Header("Parpadeo")]
        [SerializeField] private float flickerSpeed = 7f;
        [Range(0f, 1f)]
        [SerializeField] private float flickerAmount = 0.4f;
        [Tooltip("Probabilidad por segundo de un apagón breve.")]
        [SerializeField] private float blackoutChancePerSecond = 0.2f;
        [SerializeField] private Vector2 blackoutDuration = new Vector2(0.05f, 0.3f);

        [Header("Pulso (alarma)")]
        [Tooltip("Ciclos por segundo.")]
        [SerializeField] private float pulseFrequency = 0.8f;

        [Header("Material emisivo (opcional)")]
        [SerializeField] private Renderer emissiveRenderer;
        [SerializeField] private Color emissionColor = new Color(1f, 0.08f, 0.03f);
        [SerializeField] private float emissionStrength = 4f;

        private Light lightSource;
        private MaterialPropertyBlock block;
        private float seed;
        private float blackoutTimer;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            block = new MaterialPropertyBlock();
            seed = Random.value * 100f; // cada luz parpadea distinto
        }

        private void Update()
        {
            float factor = mode == Mode.Flicker
                ? 1f - flickerAmount * Mathf.PerlinNoise(seed, Time.time * flickerSpeed)
                : 0.2f + 0.8f * (0.5f + 0.5f * Mathf.Sin((Time.time * pulseFrequency + seed) * Mathf.PI * 2f));

            // Apagones breves aleatorios (solo en modo parpadeo).
            if (mode == Mode.Flicker)
            {
                if (blackoutTimer > 0f) { blackoutTimer -= Time.deltaTime; factor = 0.05f; }
                else if (Random.value < blackoutChancePerSecond * Time.deltaTime)
                    blackoutTimer = Random.Range(blackoutDuration.x, blackoutDuration.y);
            }

            lightSource.intensity = baseIntensity * factor;

            if (emissiveRenderer != null)
            {
                emissiveRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionId, emissionColor * (emissionStrength * factor));
                emissiveRenderer.SetPropertyBlock(block);
            }
        }
    }
}

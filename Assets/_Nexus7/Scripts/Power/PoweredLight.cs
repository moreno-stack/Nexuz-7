using UnityEngine;

namespace Nexus7.Power
{
    /// <summary>
    /// Iluminación dinámica según el estado de la red:
    ///   OFFLINE  → rojo de emergencia, tenue y parpadeante
    ///   PARTIAL  → ámbar, más intensa cuanta más energía haya
    ///   RESTORED → blanco frío a plena potencia
    /// Al subir de estado hace un breve "arranque" (parpadeo rápido), como un fluorescente al encenderse.
    /// También puede iluminar un material emisivo (lámpara, núcleo del reactor).
    ///
    /// Colocar en: un GameObject con Light.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PoweredLight : MonoBehaviour
    {
        [Tooltip("Energía mínima para que ESTA luz salga del modo emergencia (0 = sigue a la red).")]
        [SerializeField, Min(0)] private int minPower = 0;

        [Header("Offline")]
        [SerializeField] private Color offlineColor = new Color(1f, 0.08f, 0.04f);
        [SerializeField] private float offlineIntensity = 0.9f;
        [SerializeField] private bool flickerWhenOffline = true;
        [Header("Partial")]
        [SerializeField] private Color partialColor = new Color(1f, 0.68f, 0.32f);
        [SerializeField] private float partialIntensity = 1.4f;
        [Header("Restored")]
        [SerializeField] private Color restoredColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private float restoredIntensity = 2.2f;

        [Header("Transición")]
        [SerializeField] private float transitionSpeed = 2f;
        [SerializeField] private float bootFlickerTime = 0.7f;

        [Header("Material emisivo (opcional)")]
        [SerializeField] private Renderer emissiveRenderer;
        [SerializeField] private float emissionStrength = 2.5f;

        private Light lightSource;
        private MaterialPropertyBlock block;
        private Color currentColor;
        private float currentIntensity;
        private int lastLevel = -1;
        private float bootTimer;
        private float seed;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            block = new MaterialPropertyBlock();
            seed = Random.value * 100f;
            currentColor = offlineColor;
            currentIntensity = offlineIntensity;
        }

        private void Update()
        {
            var grid = PowerGrid.Instance;
            int power = PowerGrid.Power;
            int max = grid != null ? grid.MaxPower : 1;

            // ¿En qué estado está ESTA luz?
            Color targetColor;
            float targetIntensity;
            bool offline = power <= 0 || power < minPower;
            if (offline)                { targetColor = offlineColor;  targetIntensity = offlineIntensity; }
            else if (power >= max)      { targetColor = restoredColor; targetIntensity = restoredIntensity; }
            else
            {
                float t = (float)power / max;
                targetColor = partialColor;
                targetIntensity = Mathf.Lerp(partialIntensity * 0.7f, partialIntensity * 1.3f, t);
            }

            // Arranque: al subir de nivel, parpadeo rápido
            if (lastLevel >= 0 && power > lastLevel) bootTimer = bootFlickerTime;
            lastLevel = power;

            float k = transitionSpeed * Time.deltaTime;
            currentColor = Color.Lerp(currentColor, targetColor, k);
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, k);

            float factor = 1f;
            if (bootTimer > 0f)
            {
                bootTimer -= Time.deltaTime;
                factor = Random.value < 0.5f ? 0.1f : 1.2f;
            }
            else if (offline && flickerWhenOffline)
            {
                factor = 1f - 0.45f * Mathf.PerlinNoise(seed, Time.time * 6f);
                if (Random.value < 0.003f) factor = 0.05f; // micro-apagón
            }

            lightSource.color = currentColor;
            lightSource.intensity = currentIntensity * factor;

            if (emissiveRenderer != null)
            {
                emissiveRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionId, currentColor * (emissionStrength * factor * currentIntensity / Mathf.Max(0.01f, restoredIntensity)));
                emissiveRenderer.SetPropertyBlock(block);
            }
        }
    }
}

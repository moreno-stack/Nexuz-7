using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Pantalla de terminal "viva": parpadea, alterna colores y a veces se cuelga con glitches.
    /// Ambientación de laboratorio abandonado.
    ///
    /// Colocar en: un objeto con Renderer cuyo material tenga emisión.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class BlinkingScreen : MonoBehaviour
    {
        [SerializeField] private Color[] colors = { new Color(0.2f, 0.8f, 1f), new Color(0.3f, 1f, 0.6f) };
        [SerializeField] private float intensity = 1.6f;
        [SerializeField] private Vector2 changeInterval = new Vector2(0.4f, 2.5f);
        [Range(0f, 1f)]
        [SerializeField] private float glitchChance = 0.15f;

        private Renderer target;
        private MaterialPropertyBlock block;
        private float timer;
        private Color current;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            current = colors.Length > 0 ? colors[0] : Color.cyan;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = Random.Range(changeInterval.x, changeInterval.y);
                if (colors.Length > 0) current = colors[Random.Range(0, colors.Length)];
            }

            float factor = 0.85f + 0.15f * Mathf.Sin(Time.time * 20f);          // barrido de refresco
            if (Random.value < glitchChance * Time.deltaTime * 10f) factor = Random.value < 0.5f ? 0.05f : 2f;

            target.GetPropertyBlock(block);
            block.SetColor(EmissionId, current * (intensity * factor));
            target.SetPropertyBlock(block);
        }
    }
}

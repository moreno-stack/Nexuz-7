using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Player;

namespace Nexus7.UI
{
    /// <summary>
    /// Dos barras pequeñas encima del escudo:
    ///   · ESTAMINA: solo aparece cuando no está llena. Verde → amarilla y roja «AGOTADO» si se vacía.
    ///   · RUIDO: lo fuerte que suenan tus pasos ahora mismo (0 quieto · 1 m agachado · 6 m caminando · 12 m corriendo).
    ///
    /// Colocar en: el Canvas "HUD" (lo añade el constructor). 'Fill' = Image con pivote X = 0.
    /// </summary>
    public class StaminaUI : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject staminaRoot;
        [SerializeField] private Image staminaFill;
        [SerializeField] private TextMeshProUGUI staminaLabel;
        [SerializeField] private GameObject noiseRoot;
        [SerializeField] private Image noiseFill;
        [SerializeField] private TextMeshProUGUI noiseLabel;
        [Tooltip("Radio que llena la barra de ruido.")]
        [SerializeField] private float maxNoise = 12f;

        private float shownNoise, peakNoise;
        private int shownMeters = -1;

        // También cuenta el ruido que no son pasos (jadeo, aterrizajes...)
        private void OnEnable() => Nexus7.Core.NoiseSystem.NoiseEmitted += OnNoise;
        private void OnDisable() => Nexus7.Core.NoiseSystem.NoiseEmitted -= OnNoise;
        private void OnNoise(Vector3 position, float radius, GameObject source)
        {
            if (player != null && source == player.gameObject) peakNoise = Mathf.Max(peakNoise, radius);
        }

        private void Start()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            var stamina = player != null ? player.Stamina : null;
            bool hud = !GameHud.Hidden && player != null;

            bool showStamina = hud && stamina != null && (stamina.Normalized < 0.995f || stamina.Exhausted);
            if (staminaRoot != null && staminaRoot.activeSelf != showStamina) staminaRoot.SetActive(showStamina);
            if (showStamina)
            {
                float k = stamina.Normalized;
                if (staminaFill != null)
                {
                    staminaFill.rectTransform.localScale = new Vector3(k, 1f, 1f);
                    staminaFill.color = stamina.Exhausted ? new Color(1f, 0.3f, 0.25f)
                                      : Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.45f, 1f, 0.55f), k);
                }
                if (staminaLabel != null) staminaLabel.text = stamina.Exhausted ? "<color=#FF6A5A>AGOTADO</color>" : "ESTAMINA";
            }

            // El ruido sube al instante y baja suave (se lee mejor)
            peakNoise = Mathf.MoveTowards(peakNoise, 0f, 8f * Time.deltaTime);
            float noise = Mathf.Max(player != null ? player.CurrentNoiseRadius : 0f, peakNoise);
            shownNoise = noise > shownNoise ? noise : Mathf.MoveTowards(shownNoise, noise, 10f * Time.deltaTime);
            bool showNoise = hud && shownNoise > 0.05f;
            if (noiseRoot != null && noiseRoot.activeSelf != showNoise) noiseRoot.SetActive(showNoise);
            if (showNoise)
            {
                float n = Mathf.Clamp01(shownNoise / Mathf.Max(0.1f, maxNoise));
                if (noiseFill != null)
                {
                    noiseFill.rectTransform.localScale = new Vector3(n, 1f, 1f);
                    noiseFill.color = Color.Lerp(new Color(0.5f, 0.8f, 1f), new Color(1f, 0.45f, 0.2f), n);
                }
                int meters = Mathf.RoundToInt(shownNoise);
                if (noiseLabel != null && meters != shownMeters) { shownMeters = meters; noiseLabel.text = "RUIDO  " + meters + " m"; }
            }
        }
    }
}

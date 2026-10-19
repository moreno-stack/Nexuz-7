using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus7.UI
{
    /// <summary>
    /// Subtítulos de las cinemáticas (abajo, centrados, con el nombre de quien habla en color) + franjas negras de cine
    /// + indicador "Mantén [Espacio] para saltar". El texto aparece letra a letra (30 por segundo).
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class SubtitleUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform barTop;
        [SerializeField] private RectTransform barBottom;
        [SerializeField] private TextMeshProUGUI skipText;
        [SerializeField] private Image skipFill;
        [SerializeField] private GameObject skipBar;
        [SerializeField] private float charsPerSecond = 30f;
        [SerializeField] private float barHeight = 110f;

        private float shownSince;
        private int total;
        private float bars, barsTarget;

        public static string SpeakerColor(string speaker) =>
            speaker == "Tú" ? "#9FE0FF" : speaker.StartsWith("IA") ? "#FFB060" : "#E6E6E6";

        private void Awake()
        {
            if (text != null) text.text = "";
            if (background != null) background.enabled = false;
            if (skipText != null) skipText.text = "";
            if (skipBar != null) skipBar.SetActive(false);
            ApplyBars();
        }

        public void Show(string speaker, string line)
        {
            if (text == null) return;
            text.text = string.IsNullOrEmpty(speaker) ? line : $"<color={SpeakerColor(speaker)}><b>{speaker}:</b></color> {line}";
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            total = text.textInfo.characterCount;
            shownSince = Time.unscaledTime;
            if (background != null) background.enabled = true;
        }

        public void Hide()
        {
            if (text != null) text.text = "";
            if (background != null) background.enabled = false;
        }

        public void SetLetterbox(bool on)
        {
            barsTarget = on ? 1f : 0f;
            if (skipText != null) skipText.text = on ? "Mantén [Espacio] para saltar" : "";
            if (skipBar != null) skipBar.SetActive(on);
        }

        public void SetSkipProgress(float p)
        {
            if (skipFill != null) skipFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(p), 1f, 1f);
        }

        private void Update()
        {
            if (text != null && total > 0)
                text.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt((Time.unscaledTime - shownSince) * charsPerSecond));
            if (bars != barsTarget) { bars = Mathf.MoveTowards(bars, barsTarget, Time.unscaledDeltaTime * 2.5f); ApplyBars(); }
        }

        private void ApplyBars()
        {
            float h = barHeight * bars;
            if (barTop != null) barTop.sizeDelta = new Vector2(barTop.sizeDelta.x, h);
            if (barBottom != null) barBottom.sizeDelta = new Vector2(barBottom.sizeDelta.x, h);
        }
    }
}

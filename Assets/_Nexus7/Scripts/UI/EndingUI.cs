using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Gameplay;

namespace Nexus7.UI
{
    /// <summary>
    /// Pantalla final de VICTORIA o DERROTA con resultados:
    ///   · Título y color del final (Óptimo / Táctico / Crítico) y texto narrativo
    ///   · Desglose por categoría con barras (Sigilo, Recursos, Tiempo, Creatividad) y penalizaciones
    ///   · Estadísticas detalladas de toda la partida
    ///   · Puntuación total animada, rango (S-D), récord
    ///   · Botones [R] Reiniciar partida · [M] Menú principal (también con ratón)
    ///
    /// Colocar en: el Canvas "HUD" de la escena final. Necesita un EventSystem en la escena para los clics.
    /// </summary>
    public class EndingUI : MonoBehaviour
    {
        public static EndingUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI storyText;
        [SerializeField] private TextMeshProUGUI breakdownText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI totalText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [Tooltip("Etiquetas de las 4 categorías (Sigilo, Recursos, Tiempo, Creatividad), una por barra.")]
        [SerializeField] private TextMeshProUGUI[] categoryLabels = new TextMeshProUGUI[4];
        [Tooltip("Rellenos de las 4 barras (pivote a la izquierda: se escalan en X de 0 a 1).")]
        [SerializeField] private Image[] categoryBars = new Image[4];
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private bool shown;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
            if (restartButton != null) restartButton.onClick.AddListener(GameFlow.Restart);
            if (menuButton != null) menuButton.onClick.AddListener(GameFlow.ReturnToMenu);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!shown) return;
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton0)) GameFlow.Restart();
            else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.JoystickButton1)) GameFlow.ReturnToMenu();
        }

        public void Show(ScoreResult r, ScoreInput i, string story, bool newRecord)
        {
            shown = true;
            panel.SetActive(true);

            string color = r.ending == EndingType.Optimal ? "#66FFB0" : r.ending == EndingType.Tactical ? "#FFC23A" : "#FF5040";
            string name = r.ending == EndingType.Optimal ? "FINAL ÓPTIMO — EL FANTASMA DEL NEXUS-7"
                        : r.ending == EndingType.Tactical ? "FINAL TÁCTICO — ESCAPE A LA FUERZA"
                        : "FINAL CRÍTICO — LA NAVE TE ARRASTRA CONSIGO";
            titleText.text = $"<color={color}><b>{(i.escaped ? "VICTORIA" : "DERROTA")}</b></color>\n<size=60%>{name}</size>";
            storyText.text = story;

            string[] names = { "Sigilo", "Recursos", "Tiempo", "Creatividad" };
            int[] values = { r.stealth, r.resources, r.time, r.creativity };
            int[] maxima = { ScoreCalculator.StealthMax, ScoreCalculator.ResourcesMax, ScoreCalculator.TimeMax, ScoreCalculator.CreativityMax };
            for (int c = 0; c < categoryLabels.Length && c < names.Length; c++)
                if (categoryLabels[c] != null) categoryLabels[c].text = $"{names[c]}  <b>{values[c]}</b> <size=75%><color=#8FA3B8>/ {maxima[c]}</color></size>";

            var sb = new StringBuilder();
            if (r.penalty > 0) sb.AppendLine($"<color=#FF7A6A>Penalización por muertes: -{r.penalty}</color>");
            if (!i.escaped) sb.AppendLine("<color=#FF7A6A>Sin escape: puntuación x0.5</color>");
            foreach (var h in r.highlights) sb.AppendLine($"<color=#9FE0FF>+ {h}</color>");
            breakdownText.text = sb.ToString();

            int min = Mathf.FloorToInt(i.totalSeconds / 60f), sec = Mathf.FloorToInt(i.totalSeconds % 60f);
            const string col = "<pos=52%>";
            statsText.text =
                "<b>ESTADÍSTICAS DE LA PARTIDA</b>\n\n" +
                $"Tiempo total: <b>{min:00}:{sec:00}</b>{col}Cuenta atrás sobrante: <b>{i.evacuationRemaining:0} s</b>\n" +
                $"Detecciones: <b>{i.detections}</b>{col}Alarmas: <b>{i.alarms}</b>\n" +
                $"Alertas rojas: <b>{i.redAlerts}</b>{col}Capturas: <b>{i.captures}</b>\n" +
                $"Muertes: <b>{i.deaths}</b>{col}Ruido fuerte: <b>{i.loudNoise:0.0}</b>\n" +
                $"Objetos recogidos: <b>{i.itemsCollected}</b>{col}Objetos fabricados: <b>{i.itemsCrafted}</b>\n" +
                $"Dispositivos usados: <b>{i.devicesUsed}</b>{col}Tipos distintos: <b>{i.distinctDeviceKinds}/3</b>\n" +
                $"Hackeos superados: <b>{i.hacksSucceeded}</b>{col}Hackeos fallidos: <b>{i.hacksFailed}</b>\n" +
                $"Sectores apagados: <b>{i.sectorsDisabled}</b>{col}Objetivos de PEM: <b>{i.empTargetsHit}</b>\n" +
                $"Señuelos efectivos: <b>{i.decoysTriggered}</b>{col}Recursos sobrantes: <b>{i.resourceValue:0}</b>";

            gradeText.text = r.grade;
            gradeText.color = r.grade == "S" ? new Color(1f, 0.85f, 0.3f) : r.grade == "A" ? new Color(0.4f, 1f, 0.7f)
                            : r.grade == "B" ? new Color(0.4f, 0.8f, 1f) : r.grade == "C" ? new Color(1f, 0.7f, 0.3f) : new Color(1f, 0.35f, 0.3f);

            StartCoroutine(Animate(r, newRecord));
        }

        private IEnumerator Animate(ScoreResult r, bool newRecord)
        {
            float[] targets =
            {
                (float)r.stealth / ScoreCalculator.StealthMax, (float)r.resources / ScoreCalculator.ResourcesMax,
                (float)r.time / ScoreCalculator.TimeMax, (float)r.creativity / ScoreCalculator.CreativityMax
            };
            for (float t = 0f; t <= 1.5f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.5f);
                for (int b = 0; b < categoryBars.Length && b < targets.Length; b++)
                    if (categoryBars[b] != null) categoryBars[b].rectTransform.localScale = new Vector3(targets[b] * k, 1f, 1f);
                totalText.text = $"PUNTUACIÓN  <b>{Mathf.RoundToInt(r.total * k)}</b>";
                yield return null;
            }
            totalText.text = $"PUNTUACIÓN  <b>{r.total}</b>\n<size=55%>{(newRecord ? "<color=#FFE14A>¡NUEVO RÉCORD!</color>" : $"Récord: {GameFlow.BestScore} ({GameFlow.BestGrade})")}</size>";
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Core;

namespace Nexus7.UI
{
    /// <summary>
    /// Indicador del nivel de alerta (arriba en el centro): "NORMAL / ALERTA AMARILLA / ALERTA ROJA"
    /// + barra de tensión con marcas de umbral. En Alerta Roja parpadea.
    /// Opcional: al empezar la escena muestra el registro de incidentes acumulado en la partida.
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class AlertHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image heatFill;
        [SerializeField] private bool briefingOnStart = true;

        private float shown;

        private void Start()
        {
            shown = AlertSystem.Heat;
            if (!briefingOnStart || HUDManager.Instance == null) return;
            var s = AlertSystem.Stats;
            HUDManager.Instance.QueueMessage(
                $"IA NEXUS-7: Registro de la nave: {s.alarms} alarmas, {s.spotted} detecciones, {s.captures} capturas. " +
                $"Nivel actual: {ShipStatusReport.LevelName(AlertSystem.Level)}.", 3.5f);
        }

        private void Update()
        {
            var level = AlertSystem.Level;
            shown = Mathf.MoveTowards(shown, AlertSystem.Heat, 40f * Time.deltaTime);

            Color c;
            ColorUtility.TryParseHtmlString(ShipStatusReport.LevelHex(level), out c);
            if (level == AlertLevel.Red && Mathf.Repeat(Time.time * 2f, 1f) < 0.35f) c = Color.white;

            if (label != null)
            {
                label.text = $"{ShipStatusReport.LevelName(level)}  <size=70%>{shown:0}</size>";
                label.color = c;
            }
            if (heatFill != null)
            {
                heatFill.rectTransform.localScale = new Vector3(shown / 100f, 1f, 1f);
                heatFill.color = c;
            }
        }
    }
}

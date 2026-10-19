using TMPro;
using UnityEngine;
using Nexus7.Gameplay;

namespace Nexus7.UI
{
    /// <summary>
    /// Cuenta atrás grande en pantalla: "AUTODESTRUCCIÓN 04:32". Blanco → naranja (aviso) → rojo parpadeante (crítica),
    /// con un "latido" de escala en los últimos 10 segundos. También muestra el progreso de la secuencia de lanzamiento.
    ///
    /// Colocar en: el Canvas "HUD" de la escena final.
    /// </summary>
    public class CountdownHUD : MonoBehaviour
    {
        [SerializeField] private EvacuationCountdown countdown;
        [SerializeField] private LaunchSequence launch;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI launchText;

        private void Update()
        {
            if (countdown == null || timerText == null) return;

            float r = countdown.Remaining;
            int min = Mathf.FloorToInt(r / 60f), sec = Mathf.FloorToInt(r % 60f);
            Color c = countdown.CurrentPhase == EvacuationCountdown.Phase.Critical
                ? (Mathf.Repeat(Time.time * 2f, 1f) < 0.5f ? new Color(1f, 0.15f, 0.1f) : Color.white)
                : countdown.CurrentPhase == EvacuationCountdown.Phase.Warning ? new Color(1f, 0.6f, 0.15f) : Color.white;

            timerText.text = $"<size=55%>AUTODESTRUCCIÓN</size>\n{min:00}:{sec:00}";
            timerText.color = c;
            float pulse = r <= 10f && countdown.Running ? 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI)) : 1f;
            timerText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);

            if (launchText != null && launch != null)
                launchText.text = launch.IsReady
                    ? "<color=#66FF99>CÁPSULA B LISTA: ¡EMBARCA!</color>"
                    : $"Secuencia de lanzamiento: {launch.Completed}/{launch.Total}";
        }
    }
}

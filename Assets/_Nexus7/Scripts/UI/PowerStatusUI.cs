using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Power;
using Nexus7.Security;

namespace Nexus7.UI
{
    /// <summary>
    /// Indicador de energía (arriba a la izquierda, bajo el objetivo): "ENERGÍA 2/4 · PARCIAL" + barra,
    /// y debajo la lista de sistemas hackeados con su cuenta atrás ("Barrera eléctrica: 18 s").
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class PowerStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image barFill;
        [SerializeField] private TextMeshProUGUI hackList;

        [SerializeField] private Color offlineColor = new Color(1f, 0.2f, 0.1f);
        [SerializeField] private Color partialColor = new Color(1f, 0.7f, 0.3f);
        [SerializeField] private Color restoredColor = new Color(0.4f, 0.9f, 1f);

        private float shownLevel;
        private readonly StringBuilder sb = new StringBuilder();

        private void Update()
        {
            var grid = PowerGrid.Instance;
            if (grid != null)
            {
                string state; Color c;
                switch (grid.State)
                {
                    case PowerState.Restored: state = "RESTAURADA"; c = restoredColor; break;
                    case PowerState.Partial:  state = "PARCIAL";    c = partialColor;  break;
                    default:                  state = "EMERGENCIA"; c = offlineColor;  break;
                }
                shownLevel = Mathf.MoveTowards(shownLevel, grid.Level, Time.deltaTime);
                if (label != null) { label.text = $"ENERGÍA  {grid.CurrentPower}/{grid.MaxPower}  ·  {state}"; label.color = c; }
                if (barFill != null) { barFill.rectTransform.localScale = new Vector3(shownLevel, 1f, 1f); barFill.color = c; }
            }

            if (hackList != null)
            {
                sb.Clear();
                foreach (var terminal in HackTerminal.All)
                {
                    if (!terminal.TargetsDisabled) continue;
                    float left = terminal.RemainingTime;
                    string color = left < 5f ? "#FF6A5A" : "#66CCFF"; // rojo cuando está a punto de reactivarse
                    sb.AppendLine($"<color={color}>{terminal.TargetLabel} desactivada: {Mathf.CeilToInt(left)} s</color>");
                }
                hackList.text = sb.ToString();
            }
        }
    }
}

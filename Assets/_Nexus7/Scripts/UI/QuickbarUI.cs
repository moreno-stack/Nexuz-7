using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Devices;
using Nexus7.Stealth;

namespace Nexus7.UI
{
    /// <summary>
    /// Barra de dispositivos (abajo en el centro): "[1] Señuelo x2  [2] PEM x1  [3] Camuflaje x0"
    /// con la ranura seleccionada resaltada y la acción disponible ([G] lanzar / activar).
    /// También muestra el camuflaje óptico activo: tinte de pantalla + cuenta atrás.
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    [System.Obsolete("Usa HotbarUI (barra rápida 1–9). Se mantiene solo para escenas sin reconstruir.")]
    public class QuickbarUI : MonoBehaviour
    {
        [SerializeField] private DeviceQuickbar quickbar;
        [SerializeField] private PlayerStealth stealth;
        [SerializeField] private TextMeshProUGUI slotsText;
        [SerializeField] private TextMeshProUGUI cloakText;
        [SerializeField] private Image cloakOverlay;

        private readonly StringBuilder sb = new StringBuilder();

        private void Update()
        {
            if (quickbar != null && slotsText != null)
            {
                sb.Clear();
                for (int i = 0; i < quickbar.SlotCount; i++)
                {
                    var d = quickbar.GetSlot(i);
                    if (d == null) continue;
                    int n = quickbar.GetCount(i);
                    bool sel = i == quickbar.SelectedIndex;
                    string hex = sel ? "#" + ColorUtility.ToHtmlStringRGB(d.DeviceColor) : (n > 0 ? "#DDDDDD" : "#666666");
                    string label = $"[{i + 1}] {d.DisplayName} x{n}";
                    sb.Append(sel ? $"<color={hex}><b>{label}</b></color>" : $"<color={hex}>{label}</color>");
                    sb.Append("     ");
                }
                var s = quickbar.Selected;
                if (s != null)
                {
                    string action = s.UseMode == DeviceUseMode.Throw
                        ? (quickbar.IsAiming ? "suelta [G] para lanzar" : "mantén [G] para apuntar")
                        : "[G] activar";
                    sb.Append($"\n<size=75%><color=#9AB8CC>{action}</color></size>");
                }
                slotsText.text = sb.ToString();
            }

            bool cloaked = stealth != null && stealth.IsCloaked;
            if (cloakText != null)
                cloakText.text = cloaked ? $"CAMUFLAJE ÓPTICO  {Mathf.CeilToInt(stealth.CloakRemaining)} s" : "";
            if (cloakOverlay != null)
            {
                var c = cloakOverlay.color;
                float target = cloaked ? 0.12f + 0.05f * Mathf.Sin(Time.time * 3f) : 0f;
                if (cloaked && stealth.CloakRemaining < 3f) target *= Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? 1f : 0.3f; // aviso de fin
                c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime);
                cloakOverlay.color = c;
            }
        }
    }
}

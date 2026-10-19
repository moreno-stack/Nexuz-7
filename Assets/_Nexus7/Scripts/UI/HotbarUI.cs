using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Devices;
using Nexus7.Inventory;
using Nexus7.Stealth;

namespace Nexus7.UI
{
    /// <summary>
    /// Barra rápida en pantalla (abajo en el centro): "[1] Batería ×2  [2] Señuelo ×1 ..." con la casilla
    /// seleccionada resaltada y la acción disponible. También muestra el camuflaje óptico activo.
    /// Sustituye a QuickbarUI.
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private Hotbar hotbar;
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private DeviceQuickbar devices;
        [SerializeField] private PlayerStealth stealth;
        [SerializeField] private TextMeshProUGUI slotsText;
        [SerializeField] private TextMeshProUGUI cloakText;
        [SerializeField] private Image cloakOverlay;

        private readonly StringBuilder sb = new StringBuilder();

        private void Start()
        {
            // El HUD puede construirse antes que el jugador: se resuelven las referencias que falten por la etiqueta "Player".
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            if (hotbar == null) hotbar = player.GetComponent<Hotbar>();
            if (inventory == null) inventory = player.GetComponent<InventorySystem>();
            if (devices == null) devices = player.GetComponent<DeviceQuickbar>();
            if (stealth == null) stealth = player.GetComponent<PlayerStealth>();
        }

        private void Update()
        {
            if (hotbar != null && inventory != null && slotsText != null)
            {
                sb.Clear();
                var m = hotbar.Model;
                if (m.Count == 0) sb.Append("<color=#7A8794>Barra rápida vacía: recoge objetos para usarlos con [1]–[9]</color>");
                for (int i = 0; i < m.Count; i++)
                {
                    var it = m.Get(i);
                    bool sel = i == m.Selected;
                    var dev = it as DeviceItemData;
                    string hex = sel ? (dev != null ? "#" + ColorUtility.ToHtmlStringRGB(dev.DeviceColor) : "#FFFFFF") : "#B8C2CC";
                    string label = $"[{i + 1}] {it.DisplayName} ×{inventory.GetCount(it)}";
                    sb.Append(sel ? $"<color={hex}><b>{label}</b></color>" : $"<color={hex}>{label}</color>");
                    if (i < m.Count - 1) sb.Append("   ");
                }
                var selDev = hotbar.SelectedItem as DeviceItemData;
                if (selDev != null)
                {
                    string action = selDev.UseMode == DeviceUseMode.Throw
                        ? (devices != null && devices.IsAiming ? "suelta [G] / clic para lanzar" : "mantén [G] / clic para apuntar")
                        : "[G] / clic: usar";
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
                if (cloaked && stealth.CloakRemaining < 3f) target *= Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? 1f : 0.3f;
                c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime);
                cloakOverlay.color = c;
            }
        }
    }
}

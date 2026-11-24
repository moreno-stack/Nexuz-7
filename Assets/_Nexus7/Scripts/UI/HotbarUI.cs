using System.Collections.Generic;
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

        // Fila de iconos encima del texto (se crea por código; solo se actualiza cuando algo cambia)
        public const float IconSize = 44f, IconGap = 8f;
        private RectTransform iconRow;
        private readonly List<IconSlot> slots = new List<IconSlot>();
        private readonly List<ItemData> shownItems = new List<ItemData>();
        private string shownState;

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
                UpdateIcons(m);
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

        /// <summary>Un icono por casilla de la barra rápida; la seleccionada con marco blanco y algo mayor.</summary>
        private void UpdateIcons(HotbarModel m)
        {
            sb.Clear();
            for (int i = 0; i < m.Count; i++) sb.Append(m.Get(i)?.Id).Append(':').Append(inventory.GetCount(m.Get(i))).Append(';');
            sb.Append(m.Selected);
            string state = sb.ToString();
            if (state == shownState) return;
            shownState = state;

            if (iconRow == null)
            {
                var go = new GameObject("IconosBarraRapida", typeof(RectTransform));
                go.transform.SetParent(slotsText.transform.parent, false);
                iconRow = go.GetComponent<RectTransform>();
                if (iconRow == null) return;
                iconRow.anchorMin = iconRow.anchorMax = new Vector2(0.5f, 0f);
                iconRow.pivot = new Vector2(0.5f, 0f);
                iconRow.anchoredPosition = new Vector2(0f, 84f);   // encima del texto de la barra (y = 12, alto 64)
                iconRow.sizeDelta = new Vector2(0f, IconSize);
            }
            while (slots.Count < m.Count) slots.Add(IconSlot.Create(iconRow, IconSize));
            shownItems.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                bool used = i < m.Count;
                slots[i].gameObject.SetActive(used);
                if (!used) continue;
                var it = m.Get(i);
                shownItems.Add(it);
                slots[i].Set(it, inventory.GetCount(it).ToString(), i == m.Selected);
                slots[i].Place(new Vector2(0.5f, 0f), new Vector2((i - (m.Count - 1) * 0.5f) * (IconSize + IconGap), IconSize * 0.5f));
            }
        }
    }
}

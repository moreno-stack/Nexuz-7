using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.UI
{
    /// <summary>
    /// Panel de inventario (Tab / I / botón View del mando). Lista los objetos agrupados por categoría
    /// con su cantidad y la ocupación de espacios. Se refresca solo con el evento Changed del inventario.
    /// No pausa el juego: ¡los drones siguen patrullando mientras miras tu mochila!
    ///
    /// Colocar en: el Canvas "HUD". "Panel" = objeto que se muestra/oculta; "List Text" = TextMeshPro dentro.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI listText;

        [Header("Controles")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode toggleKeyAlt = KeyCode.I;
        [Tooltip("JoystickButton6 = botón View/Back en mando Xbox.")]
        [SerializeField] private KeyCode togglePadButton = KeyCode.JoystickButton6;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            if (inventory == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) inventory = player.GetComponent<InventorySystem>();
            }
            if (inventory != null) inventory.Changed += Refresh;
            if (panel != null) panel.SetActive(false);
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(toggleKeyAlt) || Input.GetKeyDown(togglePadButton))
            {
                if (panel != null) panel.SetActive(!panel.activeSelf);
                Refresh();
            }
        }

        public void Refresh()
        {
            if (listText == null || inventory == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"<b>INVENTARIO</b>   <size=70%>{inventory.UsedSlots}/{inventory.Capacity} espacios</size>\n");

            var summary = inventory.GetSummary();
            if (summary.Count == 0)
            {
                sb.AppendLine("<color=#888888>(vacío)</color>");
            }
            else
            {
                foreach (var group in summary.GroupBy(s => s.item.Category))
                {
                    sb.AppendLine($"<color=#7FD4FF><b>{ItemData.CategoryLabel(group.Key)}</b></color>");
                    foreach (var stack in group)
                    {
                        string hex = ColorUtility.ToHtmlStringRGB(stack.item.UiColor);
                        string key = stack.item.IsKeyItem ? "  <size=70%><color=#FFB347>[CLAVE]</color></size>" : "";
                        sb.AppendLine($"   <color=#{hex}>{stack.item.DisplayName}</color>  x{stack.amount}{key}");
                    }
                    sb.AppendLine();
                }
            }
            sb.Append("<size=70%><color=#888888>[Tab] cerrar</color></size>");
            listText.text = sb.ToString();
        }
    }
}

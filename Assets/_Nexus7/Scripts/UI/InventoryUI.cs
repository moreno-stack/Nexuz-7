using System.Collections.Generic;
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

        // Rejilla de iconos a la derecha del panel (se crea por código al empezar)
        public const int GridColumns = 4;
        public const float TileSize = 56f, TileGap = 6f;
        private RectTransform grid;
        private readonly List<IconSlot> tiles = new List<IconSlot>();

        private void Start()
        {
            if (inventory == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) inventory = player.GetComponent<InventorySystem>();
            }
            if (inventory != null) inventory.Changed += Refresh;
            BuildGrid();
            if (panel != null) panel.SetActive(false);
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        // Un golpe cierra el inventario (no se queda tapando la pantalla mientras te atacan)
        private void OnEnable()  => Nexus7.Player.PlayerHealth.PlayerHit += OnPlayerHit;
        private void OnDisable() => Nexus7.Player.PlayerHealth.PlayerHit -= OnPlayerHit;
        private void OnPlayerHit(Vector3 _) { if (panel != null && panel.activeSelf) panel.SetActive(false); }

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
            RefreshGrid(summary.GroupBy(s => s.item.Category).SelectMany(g => g).ToList());
        }

        /// <summary>Estrecha la lista de texto y pone a su derecha la rejilla de iconos.</summary>
        private void BuildGrid()
        {
            if (panel == null || grid != null) return;
            if (listText != null && listText.rectTransform != null)
            {
                listText.rectTransform.sizeDelta = new Vector2(400f, listText.rectTransform.sizeDelta.y);
                listText.rectTransform.anchoredPosition = new Vector2(-120f, listText.rectTransform.anchoredPosition.y);
            }
            var go = new GameObject("RejillaIconos", typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);
            grid = go.GetComponent<RectTransform>();
            if (grid == null) return;
            grid.anchorMin = grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(1f, 1f);
            grid.anchoredPosition = new Vector2(-20f, -70f);
            grid.sizeDelta = new Vector2(GridColumns * (TileSize + TileGap), 0f);
        }

        /// <summary>Una casilla por montón: icono, cantidad y marco naranja si es objeto clave.</summary>
        private void RefreshGrid(List<ItemStack> stacks)
        {
            if (grid == null) return;
            while (tiles.Count < stacks.Count) tiles.Add(IconSlot.Create(grid, TileSize));
            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i < stacks.Count;
                tiles[i].gameObject.SetActive(used);
                if (!used) continue;
                tiles[i].Set(stacks[i].item, "×" + stacks[i].amount, false);
                int col = i % GridColumns, row = i / GridColumns;
                tiles[i].Place(new Vector2(0f, 1f), new Vector2(col * (TileSize + TileGap) + TileSize * 0.5f, -(row * (TileSize + TileGap) + TileSize * 0.5f)));
            }
        }
    }
}

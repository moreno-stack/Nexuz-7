using System.Collections;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Crafting;
using Nexus7.Gameplay;
using Nexus7.Inventory;
using Nexus7.Player;

namespace Nexus7.UI
{
    /// <summary>
    /// Interfaz de fabricación (se abre desde un Workbench):
    ///   [W/S] o [↑/↓] elegir receta · [E] / [Intro] fabricar · [Q] / [Retroceso] salir   (mando: stick, A, B)
    /// Columna izquierda: recetas (verde = se puede fabricar, gris = faltan recursos).
    /// Columna derecha: descripción, ingredientes "tienes/necesitas" y resultado.
    /// Fabricar tarda unos segundos, hace ruido y deja el banco "manipulado" para los drones.
    /// El juego NO se pausa: los drones siguen patrullando.
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        public static CraftingUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI recipeList;
        [SerializeField] private TextMeshProUGUI details;
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private Image progressFill;

        private CraftingSystem crafting;
        private Workbench station;
        private PlayerController controller;
        private int selected;
        private int openedFrame;
        private bool busy;
        private float axisCooldown;
        private readonly StringBuilder sb = new StringBuilder();

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
            if (progressRoot != null) progressRoot.SetActive(false);
        }

        private void OnEnable() => PlayerRespawn.PlayerCaught += ForceClose;
        private void OnDisable() => PlayerRespawn.PlayerCaught -= ForceClose;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Open(Workbench bench, CraftingSystem system)
        {
            if (IsOpen || system == null) return;
            station = bench;
            crafting = system;
            controller = system.GetComponent<PlayerController>();
            if (controller != null) controller.AddInputLock(this);
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, crafting.Recipes.Count - 1));
            openedFrame = Time.frameCount; // la misma pulsación de [E] que abrió el banco no debe fabricar
            panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (progressRoot != null) progressRoot.SetActive(false);
            if (controller != null) controller.RemoveInputLock(this);
            busy = false;
        }

        private void ForceClose()
        {
            StopAllCoroutines();
            Close();
        }

        private void Update()
        {
            if (!IsOpen || busy || Time.frameCount == openedFrame || crafting == null) return;
            int count = crafting.Recipes.Count;

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                Close();
                return;
            }

            int move = 0;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) move = -1;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) move = 1;
            axisCooldown -= Time.deltaTime;
            float axis = Input.GetAxisRaw("Vertical");
            if (move == 0 && Mathf.Abs(axis) > 0.6f && axisCooldown <= 0f) { move = axis > 0f ? -1 : 1; axisCooldown = 0.25f; }
            if (move != 0 && count > 0) { selected = (selected + move + count) % count; Refresh(); }

            if (count > 0 && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0)))
                StartCoroutine(CraftRoutine(crafting.Recipes[selected]));
        }

        private IEnumerator CraftRoutine(CraftingRecipe recipe)
        {
            if (!crafting.HasIngredients(recipe))
            {
                PickupNotificationUI.Notify("Faltan recursos para " + recipe.DisplayName, new Color(1f, 0.45f, 0.35f));
                yield break;
            }

            busy = true;
            if (station != null) station.RegisterCraft();
            if (progressRoot != null) progressRoot.SetActive(true);

            for (float t = 0f; t < recipe.CraftTime; t += Time.deltaTime)
            {
                if (progressFill != null) progressFill.rectTransform.localScale = new Vector3(t / recipe.CraftTime, 1f, 1f);
                yield return null;
            }
            if (progressRoot != null) progressRoot.SetActive(false);
            busy = false;

            switch (crafting.TryCraft(recipe))
            {
                case CraftingSystem.CraftResult.Success:
                    PickupNotificationUI.Notify($"Fabricado: {recipe.DisplayName}", new Color(0.4f, 1f, 0.6f)); break;
                case CraftingSystem.CraftResult.NoSpace:
                    PickupNotificationUI.Notify("Inventario lleno: no se pudo fabricar", new Color(1f, 0.45f, 0.35f)); break;
                default:
                    PickupNotificationUI.Notify("Faltan recursos", new Color(1f, 0.45f, 0.35f)); break;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (crafting == null) return;
            var inventory = crafting.Inventory;

            sb.Clear();
            sb.AppendLine("<b>FABRICACIÓN</b>\n");
            for (int i = 0; i < crafting.Recipes.Count; i++)
            {
                var r = crafting.Recipes[i];
                int can = crafting.MaxCraftable(r);
                string color = can > 0 ? "#7CFFB2" : "#7A7A7A";
                string cursor = i == selected ? "<color=#66E0FF>></color> " : "   ";
                string name = i == selected ? $"<b>{r.DisplayName}</b>" : r.DisplayName;
                sb.AppendLine($"{cursor}<color={color}>{name}</color>  <size=70%>({can} posibles)</size>");
            }
            sb.Append("\n<size=70%><color=#888888>[W/S] elegir   [E] fabricar   [Q] salir</color></size>");
            recipeList.text = sb.ToString();

            sb.Clear();
            if (crafting.Recipes.Count > 0)
            {
                var r = crafting.Recipes[selected];
                sb.AppendLine($"<b>{r.DisplayName}</b>");
                sb.AppendLine($"<size=80%>{r.Description}</size>\n");
                sb.AppendLine("<color=#7FD4FF>INGREDIENTES</color>");
                foreach (var ing in r.Ingredients.Where(x => x.IsValid))
                {
                    int have = inventory.GetCount(ing.item);
                    string c = have >= ing.amount ? "#7CFFB2" : "#FF7A6A";
                    sb.AppendLine($"  <color={c}>{ing.item.DisplayName}  {have}/{ing.amount}</color>");
                }
                sb.AppendLine($"\n<color=#7FD4FF>RESULTADO</color>\n  {r.Result.DisplayName} x{r.ResultAmount}   (en inventario: {inventory.GetCount(r.Result)})");
                sb.AppendLine($"\n<size=70%><color=#AAAAAA>Tiempo: {r.CraftTime:0.#} s · Fabricar hace ruido y los drones revisan los bancos.</color></size>");
            }
            details.text = sb.ToString();
        }
    }
}

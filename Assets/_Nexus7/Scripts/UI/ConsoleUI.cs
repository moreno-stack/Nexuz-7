using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;
using Nexus7.Gameplay;
using Nexus7.Interaction;
using Nexus7.Player;
using Nexus7.Security;

namespace Nexus7.UI
{
    /// <summary>
    /// Menú de una ControlConsole: [W/S] elegir · [E] ejecutar · [Q] salir.
    /// Las acciones con hackeo abren el minijuego; al acertar se ejecuta la acción, al fallar la consola se bloquea.
    /// El juego NO se pausa mientras usas la consola: los drones siguen moviéndose.
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class ConsoleUI : MonoBehaviour
    {
        public static ConsoleUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI menuText;
        [SerializeField] private TextMeshProUGUI infoText;

        private ControlConsole console;
        private PlayerController controller;
        private int selected;
        private int openedFrame;
        private bool waitingHack;
        private float axisCooldown;
        private string infoOverride;
        private readonly StringBuilder sb = new StringBuilder();

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable() { PlayerRespawn.PlayerCaught += ForceClose; PlayerHealth.PlayerHit += OnPlayerHit; }
        private void OnDisable() { PlayerRespawn.PlayerCaught -= ForceClose; PlayerHealth.PlayerHit -= OnPlayerHit; }
        private void OnPlayerHit(Vector3 attacker) { if (IsOpen) ForceClose(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Open(ControlConsole target, PlayerInteractor interactor)
        {
            if (IsOpen || waitingHack || target == null) return;
            console = target;
            controller = interactor.GetComponentInParent<PlayerController>();
            if (controller != null) controller.AddInputLock(this);
            selected = 0;
            infoOverride = null;
            openedFrame = Time.frameCount;
            panel.SetActive(true);
            Render();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (controller != null) controller.RemoveInputLock(this);
            waitingHack = false;
            console = null;
        }

        private void ForceClose() => Close();

        private void Update()
        {
            if (!IsOpen || waitingHack || console == null || Time.frameCount == openedFrame) return;
            int count = console.Actions.Count;

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.JoystickButton1)) { Close(); return; }

            int move = 0;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) move = -1;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) move = 1;
            axisCooldown -= Time.deltaTime;
            float axis = Input.GetAxisRaw("Vertical");
            if (move == 0 && Mathf.Abs(axis) > 0.6f && axisCooldown <= 0f) { move = axis > 0f ? -1 : 1; axisCooldown = 0.25f; }
            if (move != 0 && count > 0) { selected = (selected + move + count) % count; infoOverride = null; }

            if (count > 0 && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0)))
                Execute(console.Actions[selected]);

            Render();
        }

        private void Execute(ConsoleAction action)
        {
            if (action.oneShot && action.used) { infoOverride = "Esta acción ya se ha completado."; return; }
            if (action.type == ConsoleActionType.ShowInfo) { infoOverride = ShipStatusReport.Build(action.infoText); return; }

            if (!action.requiresHack || HackMinigameUI.Instance == null) { Apply(action); return; }

            waitingHack = true;
            panel.SetActive(false);
            var source = console;
            HackMinigameUI.Instance.Begin(action.difficulty, action.label, source.transform.position, success =>
            {
                waitingHack = false;
                if (!IsActiveConsole(source)) return; // capturado mientras hackeaba
                if (success)
                {
                    Apply(action);
                    if (!IsActiveConsole(source)) return;   // la acción cerró la consola (fin de fase)
                }
                else
                {
                    source.LockOut();
                    PickupNotificationUI.Notify("Hackeo fallido: consola bloqueada", new Color(1f, 0.35f, 0.3f));
                    Close();
                    return;
                }
                panel.SetActive(true);
                openedFrame = Time.frameCount;   // la última tecla del hackeo no cuenta como orden del menú
                Render();
            });
        }

        private bool IsActiveConsole(ControlConsole c) => console == c && controller != null;

        private void Apply(ConsoleAction action)
        {
            switch (action.type)
            {
                case ConsoleActionType.DisableSector:
                    if (action.sector != null)
                    {
                        float d = action.sector.DisableAll(action.duration);
                        infoOverride = $"<color=#66FF99>{action.sector.SectorName}: {action.sector.SystemCount} sistemas desactivados durante {d:0} s.</color>";
                        PickupNotificationUI.Notify($"{action.sector.SectorName} desactivado ({d:0} s)", new Color(0.4f, 1f, 0.6f));
                    }
                    break;

                case ConsoleActionType.UnlockDoor:
                    if (action.door != null)
                    {
                        action.door.Authorize();
                        infoOverride = action.door.IsInLockdown
                            ? $"<color=#FFC23A>{action.door.DoorName}: autorizada, pero sellada por la ALERTA ROJA. Baja la alerta.</color>"
                            : $"<color=#66FF99>{action.door.DoorName}: desbloqueada.</color>";
                    }
                    break;

                case ConsoleActionType.ReduceAlert:
                    AlertSystem.ReduceHeat(action.heatReduction);
                    infoOverride = $"<color=#66FF99>Protocolo de alerta reiniciado. Tensión: {AlertSystem.Heat:0}/100 ({ShipStatusReport.LevelName(AlertSystem.Level)}).</color>";
                    break;

                case ConsoleActionType.CompletePhase:
                    action.used = true;
                    Close();
                    StartCoroutine(CompleteRoutine(action));
                    return;
            }
            action.used = true;
        }

        private IEnumerator CompleteRoutine(ConsoleAction action)
        {
            // Cierre de fase: el jugador y los drones quedan congelados durante los mensajes y el fundido.
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var pc = playerGo != null ? playerGo.GetComponent<PlayerController>() : null;
            if (pc != null) pc.AddInputLock(this);
            foreach (var e in Nexus7.Enemies.EnemyPatrol.All.ToArray()) e.gameObject.SetActive(false);

            Nexus7.Gameplay.AlertEventBridge.MuteLevelMessages = true;
            AlertSystem.ForceLevel(AlertLevel.Normal);
            Nexus7.Gameplay.AlertEventBridge.MuteLevelMessages = false;
            var hud = HUDManager.Instance;
            if (hud != null)
            {
                if (!string.IsNullOrEmpty(action.infoText)) hud.QueueMessage(action.infoText, 4f);
                yield return hud.WaitForMessages();
            }
            if (!string.IsNullOrEmpty(action.nextScene) && Application.CanStreamedLevelBeLoaded(action.nextScene))
            {
                if (hud != null) yield return hud.Fade(0f, 1f, 1.2f);
                var inventory = Nexus7.Inventory.ItemRequirementUtil.PlayerInventory;   // el inventario viaja a la siguiente escena
                if (inventory != null) GameSession.SaveInventory(inventory);
                SceneManager.LoadScene(action.nextScene);
            }
            else
            {
                if (hud != null) hud.QueueMessage("— FIN DE LA FASE 5 —", 6f);
                if (pc != null) pc.RemoveInputLock(this);
            }
        }

        private void Render()
        {
            if (console == null) return;
            sb.Clear();
            sb.AppendLine($"<b>{console.ConsoleName.ToUpperInvariant()}</b>");
            sb.AppendLine($"<size=75%>Nave: <color={ShipStatusReport.LevelHex(AlertSystem.Level)}>{ShipStatusReport.LevelName(AlertSystem.Level)}</color></size>\n");
            for (int i = 0; i < console.Actions.Count; i++)
            {
                var a = console.Actions[i];
                string cursor = i == selected ? "<color=#66E0FF>></color> " : "   ";
                string tag = a.type == ConsoleActionType.ShowInfo || !a.requiresHack ? ""
                           : $" <size=70%><color=#FFB347>[hackeo {(a.difficulty == HackDifficulty.Easy ? "fácil" : a.difficulty == HackDifficulty.Medium ? "medio" : "difícil")}]</color></size>";
                string done = a.oneShot && a.used ? " <size=70%><color=#66FF99>(hecho)</color></size>" : "";
                string name = i == selected ? $"<b>{a.label}</b>" : a.label;
                sb.AppendLine($"{cursor}{name}{tag}{done}");
            }
            sb.Append("\n<size=70%><color=#888888>[W/S] elegir   [E] ejecutar   [Q] salir</color></size>");
            menuText.text = sb.ToString();

            if (infoOverride != null) infoText.text = infoOverride;
            else if (console.Actions.Count > 0)
            {
                var a = console.Actions[selected];
                infoText.text = a.type == ConsoleActionType.ShowInfo
                    ? "Pulsa [E] para consultar el estado de la nave."
                    : (string.IsNullOrEmpty(a.infoText) ? "" : a.infoText) +
                      (a.type == ConsoleActionType.DisableSector && a.sector != null ? $"\n\nSistemas del sector: {a.sector.SystemCount}" : "") +
                      "\n\n<size=75%><color=#AAAAAA>Fallar el hackeo bloquea la consola y eleva la alerta de la nave.</color></size>";
            }
        }
    }
}

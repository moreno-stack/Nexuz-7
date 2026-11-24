using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Combat;
using Nexus7.Core;
using Nexus7.Gameplay;
using Nexus7.Inventory;
using Nexus7.UI;

namespace Nexus7.Director
{
    /// <summary>
    /// Panel de pruebas [F4] (DebugPanelModel): atajos para probar el juego sin tener que jugarlo entero.
    /// Solo se añade en el editor o en una compilación de desarrollo ("Development Build"); en la versión final no existe.
    /// Con el panel abierto: F5 objetos imprescindibles · F6 munición · F7 accesorio · F8 +1 vida · F9/F10 tensión ±0,1 ·
    /// F11 evento de nave · F12 fase siguiente. Lo que no se puede usar ahora sale en gris con el motivo.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F4;
        private static readonly KeyCode[] actionKeys =
        {
            KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12,
        };
        private static readonly Color Done = new Color(1f, 0.85f, 0.35f);

        private bool open;
        private InventorySystem inventory;
        private PlayerWeapon weapon;
        private WeaponMod lastMod = WeaponMod.None;
        private ShipEvent lastEvent = ShipEvent.None;
        private string lastResult = "";
        private readonly Dictionary<ItemData, int> needed = new Dictionary<ItemData, int>();
        private GUIStyle style;
        private static Texture2D white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        private static void Attach(GameObject player)
        {
            if (!DebugPanelModel.Enabled(Application.isEditor, Debug.isDebugBuild)) return;
            if (player.GetComponent<DebugPanel>() == null) player.AddComponent<DebugPanel>();
        }

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            weapon = GetComponent<PlayerWeapon>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey)) open = !open;
            if (!open) return;
            for (int i = 0; i < actionKeys.Length; i++)
                if (Input.GetKeyDown(actionKeys[i]) && DebugPanelModel.FromKey(DebugPanelModel.FirstKey + i, out DebugAction a))
                    Run(a);
        }

        private DebugContext Context()
        {
            var director = GameDirector.Instance;
            var c = new DebugContext
            {
                HasInventory = inventory != null,
                HasWeapon = weapon != null && weapon.Arsenal != null && weapon.Arsenal.Weapons.Count > 0,
                HasDirector = director != null && director.Model != null,
                HasShipEvents = ShipEventDirector.Instance != null,
                Lives = LivesSystem.Lives,
                MaxLives = LivesSystem.Max,
                PhaseIndex = GameFlow.PhaseIndex(SceneManager.GetActiveScene().name),
                PhaseCount = GameFlow.Phases.GetLength(0),
            };
            if (c.HasDirector)
            {
                c.Tension = director.Model.Tension;
                c.MinTension = director.Model.MinTension;
                c.MaxTension = director.Model.MaxTension;
            }
            if (c.HasInventory)
            {
                CollectNeeded();
                foreach (var kv in needed) c.KeyItemsMissing += DebugPanelModel.Missing(kv.Value, inventory.GetCount(kv.Key));
            }
            return c;
        }

        /// <summary>Objetos imprescindibles de todas las salas de la escena (el mismo objeto en dos salas se suma).</summary>
        private void CollectNeeded()
        {
            needed.Clear();
            foreach (var spawner in FindObjectsByType<RoomLootSpawner>(FindObjectsSortMode.None))
                foreach (var g in spawner.Guaranteed)
                {
                    if (g.item == null || g.amount <= 0) continue;
                    needed.TryGetValue(g.item, out int n);
                    needed[g.item] = n + g.amount;
                }
        }

        private void Run(DebugAction a)
        {
            var c = Context();
            string reason = DebugPanelModel.Unavailable(a, c);
            if (reason != null) { lastResult = $"{DebugPanelModel.Label(a)}: no ({reason})"; return; }

            switch (a)
            {
                case DebugAction.KeyItems:
                {
                    int given = 0;
                    foreach (var kv in needed)
                        given += inventory.Add(kv.Key, DebugPanelModel.Missing(kv.Value, inventory.GetCount(kv.Key)));
                    Report($"Objetos imprescindibles: +{given}");
                    break;
                }
                case DebugAction.Ammo:
                {
                    int added = 0;
                    foreach (var w in weapon.Arsenal.Weapons) added += w.AddReserve(w.Spec.MaxReserve);
                    Report(added > 0 ? $"Munición: +{added} en la reserva" : "Munición: ya estaba llena");
                    break;
                }
                case DebugAction.Mod:
                {
                    var mod = DebugPanelModel.NextMod(lastMod, m => weapon.Arsenal.ModTarget(m) != null);
                    if (mod == WeaponMod.None || !weapon.GiveMod(mod)) { lastResult = "Accesorio: ningún arma admite otro"; break; }
                    lastMod = mod;
                    lastResult = $"Accesorio: {WeaponModModel.Name(mod)}";   // GiveMod ya avisa en pantalla
                    break;
                }
                case DebugAction.Life:
                    LivesSystem.GainLife();   // ya avisa en pantalla
                    lastResult = $"Vidas: {LivesSystem.Lives}/{LivesSystem.Max}";
                    break;
                case DebugAction.TensionUp:
                case DebugAction.TensionDown:
                {
                    int dir = a == DebugAction.TensionUp ? 1 : -1;
                    GameDirector.Instance.ForceTension(DebugPanelModel.Nudge(c.Tension, dir, c.MinTension, c.MaxTension));
                    Report($"Tensión del director: {GameDirector.Instance.Model.Tension:0.00}");
                    break;
                }
                case DebugAction.ShipEvent:
                {
                    lastEvent = DebugPanelModel.NextEvent(lastEvent);
                    bool ok = ShipEventDirector.Instance.Force(lastEvent);
                    lastResult = ok ? $"Evento de nave: {ShipEventModel.Name(lastEvent)}" : "Evento de nave: no se pudo lanzar";
                    break;
                }
                case DebugAction.NextPhase:
                {
                    string scene = GameFlow.Phases[DebugPanelModel.NextPhase(c.PhaseIndex, c.PhaseCount), 0];
                    if (inventory != null) GameSession.SaveInventory(inventory);   // como al cruzar la salida
                    if (SceneLoader.Load(scene)) lastResult = $"Cargando {GameFlow.PhaseTitle(scene)}…";
                    else Report(SceneListModel.MissingSceneMessage(scene));
                    break;
                }
            }
        }

        private void Report(string text)
        {
            lastResult = text;
            PickupNotificationUI.Notify("[PRUEBAS] " + text, Done);
        }

        private void OnGUI()
        {
            if (!open) return;
            if (white == null) { white = new Texture2D(1, 1, TextureFormat.RGBA32, false); white.SetPixel(0, 0, Color.white); white.Apply(); }
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            var c = Context();
            float w = 400f, h = 70f + 20f * DebugPanelModel.Actions.Count, x = Screen.width - w - 16f, y = 16f;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(x, y, w, h), white);
            GUI.color = Color.white;
            float cy = y + 8f;
            GUI.Label(new Rect(x + 12f, cy, w - 24f, 20f), "<b>PANEL DE PRUEBAS</b>   [F4] ocultar   <color=#999>(solo desarrollo)</color>", style);
            cy += 24f;
            foreach (var a in DebugPanelModel.Actions)
            {
                string reason = DebugPanelModel.Unavailable(a, c);
                string line = $"<b>[F{DebugPanelModel.Key(a)}]</b> {DebugPanelModel.Label(a)}";
                if (a == DebugAction.KeyItems && reason == null) line += $" ({c.KeyItemsMissing})";
                GUI.Label(new Rect(x + 12f, cy, w - 24f, 20f), reason == null ? line : $"<color=#777>{line} · {reason}</color>", style);
                cy += 20f;
            }
            GUI.Label(new Rect(x + 12f, cy + 4f, w - 24f, 20f), $"<color=#fd6>{lastResult}</color>", style);
        }
    }
}

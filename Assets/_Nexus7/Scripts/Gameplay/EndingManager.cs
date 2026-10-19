using System.Collections;
using System.Linq;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Environment;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Orquesta el final de la partida en la Sala de Evacuación:
    ///   · EscapePod.Launch()        → Escape()  → Óptimo o Táctico (según ScoreCalculator.DetermineEnding)
    ///   · Cuenta atrás a cero       → Fail()    → Crítico
    ///   · Integridad a cero (PlayerHealth.PlayerDied, sin reaparición en esta zona) → Fail() → Crítico
    /// Reúne todas las estadísticas, calcula la puntuación, guarda el récord y muestra la pantalla de resultados.
    ///
    /// Colocar en: GameSystems de la escena final.
    /// </summary>
    public class EndingManager : MonoBehaviour
    {
        public static EndingManager Instance { get; private set; }

        [SerializeField] private EvacuationCountdown countdown;
        [SerializeField] private CameraShake cameraShake;

        [Header("Textos de los finales")]
        [SerializeField, TextArea(3, 6)] private string optimalText =
            "La cápsula se separa del Nexus-7 en silencio. Ningún sistema registró tu huida. Detrás de ti, la nave se desintegra en una flor de luz blanca... y nadie sabrá jamás que hubo un superviviente.";
        [SerializeField, TextArea(3, 6)] private string tacticalText =
            "Con la seguridad saturada de hackeos y pulsos PEM, la cápsula se abre paso entre los drones que aún golpean el casco. El Nexus-7 estalla a tu espalda. Escapaste a la fuerza... pero escapaste.";
        [Tooltip("Final táctico cuando apenas se usaron hackeos o dispositivos (se escapó a pura carrera).")]
        [SerializeField, TextArea(3, 6)] private string tacticalRunText =
            "Con las alarmas aullando y los drones pisándote los talones, te lanzas dentro de la cápsula en el último instante. El Nexus-7 estalla a tu espalda. No fue limpio... pero escapaste.";
        [SerializeField, TextArea(3, 6)] private string criticalTimeText =
            "La cuenta atrás llega a cero. El reactor del Nexus-7 se libera en un destello cegador. La cápsula nunca llegó a separarse.";
        [SerializeField, TextArea(3, 6)] private string criticalDeathText =
            "Tus sistemas vitales fallan a pocos metros de la cápsula. La nave se lleva consigo a su último tripulante.";

        private bool ended;

        public bool HasEnded => ended;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void OnEnable() => PlayerHealth.PlayerDied += OnPlayerDied;
        private void OnDisable() => PlayerHealth.PlayerDied -= OnPlayerDied;

        private void OnPlayerDied() => Fail(false);

        /// <summary>El jugador ha lanzado la cápsula.</summary>
        public void Escape()
        {
            if (ended) return;
            ended = true;
            StartCoroutine(EndRoutine(true, false));
        }

        /// <summary>Derrota: timeOut = cuenta atrás agotada; si no, integridad a cero.</summary>
        public void Fail(bool timeOut)
        {
            if (ended) return;
            ended = true;
            StartCoroutine(EndRoutine(false, timeOut));
        }

        /// <summary>Reúne los datos de toda la partida.</summary>
        public ScoreInput BuildInput(bool escaped)
        {
            var s = AlertSystem.Stats;
            return new ScoreInput
            {
                detections = s.spotted, alarms = s.alarms, redAlerts = s.redAlerts, captures = s.captures, loudNoise = s.loudNoise,
                resourceValue = ResourceValue(ItemRequirementUtil.PlayerInventory),
                itemsCollected = RunStats.ItemsCollected, itemsCrafted = RunStats.ItemsCrafted, devicesUsed = RunStats.DevicesUsed,
                totalSeconds = RunStats.Elapsed,
                evacuationRemaining = countdown != null ? countdown.Remaining : 0f,
                distinctDeviceKinds = RunStats.DistinctDeviceKinds, hacksSucceeded = RunStats.HacksSucceeded, hacksFailed = s.hackFailures,
                sectorsDisabled = RunStats.SectorsDisabled, empTargetsHit = RunStats.EmpTargetsHit, decoysTriggered = RunStats.DecoysTriggered,
                deaths = RunStats.Deaths,
                escaped = escaped
            };
        }

        /// <summary>Valor de lo que queda en el inventario: chatarra 5 · componente 25 · batería 40 · dispositivo 60.</summary>
        public static float ResourceValue(InventorySystem inventory)
        {
            if (inventory == null) return 0f;
            return inventory.GetSummary().Sum(st =>
            {
                switch (st.item.Category)
                {
                    case ItemCategory.Part:      return 5f * st.amount;
                    case ItemCategory.Component: return 25f * st.amount;
                    case ItemCategory.Battery:   return 40f * st.amount;
                    case ItemCategory.Device:    return 60f * st.amount;
                    default:                     return 0f;   // herramientas y objetos clave no puntúan
                }
            });
        }

        private IEnumerator EndRoutine(bool escaped, bool timeOut)
        {
            if (countdown != null) countdown.Stop();
            var input = BuildInput(escaped);          // se calcula ANTES de la cinemática (tiempo exacto de escape)
            var result = ScoreCalculator.Calculate(input);

            // Cerrar cualquier interfaz abierta (un hackeo a medias no debe completarse tras el final)
            if (HackMinigameUI.Instance != null) HackMinigameUI.Instance.Abort();
            if (ConsoleUI.Instance != null && ConsoleUI.Instance.IsOpen) ConsoleUI.Instance.Close();

            // Congelar al jugador: se DESACTIVAN los componentes (ni una reaparición ni LevelIntro pueden devolver el control)
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var respawn = player.GetComponent<PlayerRespawn>();
                if (respawn != null) { respawn.StopAllCoroutines(); respawn.enabled = false; }
                foreach (var b in new Behaviour[]
                {
                    player.GetComponent<PlayerController>(), player.GetComponent<PlayerLook>(),
                    player.GetComponent<Nexus7.Devices.DeviceQuickbar>(), player.GetComponentInChildren<Nexus7.Interaction.PlayerInteractor>()
                })
                    if (b != null) b.enabled = false;
            }
            foreach (var e in EnemyPatrol.All.ToArray()) e.gameObject.SetActive(false);

            var hud = HUDManager.Instance;
            if (escaped)
            {
                if (hud != null) hud.ShowMessageNow("IA NEXUS-7: Cápsula sellada. Ignición en 3... 2... 1... ¡Lanzamiento!", 2.5f);
                if (cameraShake != null) cameraShake.AddTrauma(1f);
                yield return new WaitForSeconds(2.5f);
            }
            else
            {
                if (cameraShake != null) cameraShake.AddTrauma(1f);
                if (hud != null) hud.ShowMessageNow(timeOut ? "IA NEXUS-7: Tiempo agotado. Fallo de contención del reactor." : "IA NEXUS-7: Constantes vitales perdidas...", 2f);
                yield return new WaitForSeconds(1.5f);
            }
            if (hud != null) yield return hud.Fade(0f, 1f, escaped ? 2f : 0.6f);

            bool record = GameFlow.SubmitScore(result.total, result.grade);
            string story = result.ending == EndingType.Optimal ? optimalText
                         : result.ending == EndingType.Tactical ? (ScoreCalculator.UsedHeavyTactics(input) ? tacticalText : tacticalRunText)
                         : timeOut ? criticalTimeText : criticalDeathText;

            PlayerLook.SetCursorLocked(false);
            if (EndingUI.Instance != null) EndingUI.Instance.Show(result, input, story, record);
        }
    }
}

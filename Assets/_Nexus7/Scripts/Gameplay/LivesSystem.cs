using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Vidas en juego (LivesModel). Las vidas viven en memoria durante la partida y se rellenan al entrar en cada fase.
    ///   · Cada captura o caída (PlayerRespawn.PlayerCaught) cuesta una vida.
    ///   · Sin vidas: «SIN VIDAS», y la fase se reinicia desde su inicio (inventario de la entrada a la fase) con las vidas
    ///     llenas. Las patrullas, refuerzos y oleadas usan el número de reinicios para no repetir la misma colocación.
    ///   · Despejar un encuentro de oleadas devuelve una vida (GainLife).
    /// Colocar en: el jugador (lo añade CombatInstaller).
    /// </summary>
    public class LivesSystem : MonoBehaviour
    {
        private const float RestartDelay = 1.2f;

        private static LivesModel model;
        public static LivesModel Model => model ?? (model = new LivesModel((int)DifficultySettings.Current));
        public static int Lives => Model.Lives;
        public static int Max => Model.Max;
        public static bool IsLastLife => Model.IsLastLife;
        /// <summary>Reinicios por quedarse sin vidas en la fase actual (semilla extra para no repetir).</summary>
        public static int RestartsHere => Model.RestartsIn(SceneManager.GetActiveScene().name);
        public static event Action LivesChanged;

        private bool restarting;

        /// <summary>Nueva partida o volver al menú: vidas nuevas.</summary>
        public static void Clear() => model = null;

        private void Awake()
        {
            Model.EnterScene(SceneManager.GetActiveScene().name, (int)DifficultySettings.Current);
            LivesChanged?.Invoke();
        }

        private void OnEnable() => PlayerRespawn.PlayerCaught += OnCaught;
        private void OnDisable() => PlayerRespawn.PlayerCaught -= OnCaught;

        private void OnCaught()
        {
            if (restarting) return;
            bool gameOver = Model.LoseLife();
            LivesChanged?.Invoke();
            if (!gameOver)
            {
                PickupNotificationUI.Notify(Model.IsLastLife ? "¡ÚLTIMA VIDA!" : $"Vidas: {Model.Lives}/{Model.Max}",
                                            Model.IsLastLife ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.75f, 0.4f));
                return;
            }
            restarting = true;
            StartCoroutine(RestartPhase());
        }

        private IEnumerator RestartPhase()
        {
            if (HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage("SIN VIDAS. Reiniciando la fase: la IA ha cambiado sus patrullas.", 3f);
            yield return new WaitForSecondsRealtime(RestartDelay);
            Model.RestartPhase();
            LivesChanged?.Invoke();
            if (!SceneLoader.Load(SceneManager.GetActiveScene().name)) restarting = false;   // sin escena en Build Settings: se queda la reaparición normal
        }

        /// <summary>Vida extra (al despejar un encuentro). true si la ha ganado.</summary>
        public static bool GainLife()
        {
            if (!Model.GainLife()) return false;
            LivesChanged?.Invoke();
            PickupNotificationUI.Notify($"+1 vida ({Model.Lives}/{Model.Max})", new Color(0.45f, 1f, 0.55f));
            return true;
        }
    }
}

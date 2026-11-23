using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;
using Nexus7.Enemies;

namespace Nexus7.Combat
{
    /// <summary>
    /// Añade el combate a cualquier fase al cargarla, sin tener que reconstruir las escenas:
    ///   · al jugador: PlayerWeapon y el HUD de combate;
    ///   · a cada robot (EnemyPatrol): EnemyHealth.
    /// Es idempotente (si el constructor de la escena ya los puso, no duplica nada). En el menú no hace nada.
    /// </summary>
    public static class CombatInstaller
    {
        /// <summary>Componentes extra que otros módulos (el director) quieren en el jugador.</summary>
        public static event System.Action<GameObject> PlayerInstalled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Install();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

        public static void Install()
        {
            if (SceneManager.GetActiveScene().name == GameFlow.MenuScene) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (player.GetComponent<PlayerWeapon>() == null) player.AddComponent<PlayerWeapon>();
            if (player.GetComponent<Nexus7.UI.CombatHUD>() == null) player.AddComponent<Nexus7.UI.CombatHUD>();
            foreach (var robot in Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None))
                if (robot.GetComponent<EnemyHealth>() == null) robot.gameObject.AddComponent<EnemyHealth>();
            PlayerInstalled?.Invoke(player);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;
using Nexus7.Enemies;

namespace Nexus7.Combat
{
    /// <summary>
    /// Añade el combate a cualquier fase al cargarla, sin tener que reconstruir las escenas:
    ///   · al jugador: PlayerWeapon y el HUD de combate;
    ///   · al jugador: PlayerTarget (para que le den los disparos de los robots);
    ///   · a cada robot (EnemyPatrol): EnemyHealth, y EnemyGun + EnemyTactics si su tipo lleva arma (EnemyGunCatalog.ForRobot);
    ///   · a la fase: las coberturas de combate (CoverField) y el encuentro por oleadas (EncounterDirector).
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
            if (player.GetComponent<PlayerTarget>() == null) player.AddComponent<PlayerTarget>();
            EnemyGun.Tokens.Clear();
            CoverField.Build();
            EncounterDirector.EnsureForScene();
            foreach (var robot in Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None)) InstallRobot(robot.gameObject);
            PlayerInstalled?.Invoke(player);
        }

        /// <summary>Arma antes que vida: EnemyHealth busca el EnemyGun en su Awake. También lo usa el director con los refuerzos.</summary>
        public static void InstallRobot(GameObject robot)
        {
            var gunSpec = EnemyGunCatalog.ForRobot(robot.name);
            if (gunSpec != null && robot.GetComponent<EnemyGun>() == null) robot.AddComponent<EnemyGun>().Configure(gunSpec);
            if (gunSpec != null && robot.GetComponent<EnemyTactics>() == null) robot.AddComponent<EnemyTactics>();
            if (robot.GetComponent<EnemyHealth>() == null) robot.AddComponent<EnemyHealth>();
        }
    }
}

using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Flujo global del juego: nueva partida, reinicio y vuelta al menú.
    /// Reinicia TODO el estado persistente (inventario, alertas, estadísticas) antes de cargar.
    /// </summary>
    public static class GameFlow
    {
        public const string MenuScene = "MenuPrincipal";
        public const string FirstScene = "SalaInicial";
        public const string BestScoreKey = "Nexus7_MejorPuntuacion";
        public const string BestGradeKey = "Nexus7_MejorRango";

        /// <summary>Escenas jugables en orden, con su título (para Continuar y el autoguardado).</summary>
        public static readonly string[,] Phases =
        {
            { "SalaInicial", "Fase 1 · Sala inicial" },
            { "AlmacenCarga", "Fase 2 · Almacén de carga" },
            { "SalaEnergia", "Fase 3 · Sala de energía" },
            { "Laboratorio", "Fase 4 · Laboratorio" },
            { "CentroControl", "Fase 5 · Centro de control" },
            { "Evacuacion", "Fase 6 · Evacuación" },
        };

        public static bool IsPhaseScene(string scene)
        {
            for (int i = 0; i < Phases.GetLength(0); i++) if (Phases[i, 0] == scene) return true;
            return false;
        }

        /// <summary>"Fase 3 · Sala de energía" (si no es una fase, el propio nombre).</summary>
        public static string PhaseTitle(string scene)
        {
            for (int i = 0; i < Phases.GetLength(0); i++) if (Phases[i, 0] == scene) return Phases[i, 1];
            return scene;
        }

        public static void StartNewRun()
        {
            if (SceneLoader.IsLoading) return;   // doble clic o [Intro] durante una carga
            ResetRunState();
            GameSession.NewRun();
            RunStats.EnsureStarted();
            Load(FirstScene);
        }

        /// <summary>Continuar la partida guardada: restaura la sesión y carga el inicio de esa fase.</summary>
        public static bool ContinueRun()
        {
            if (SceneLoader.IsLoading || !SaveSystem.LoadAndApply(out string scene)) return false;
            return Load(scene);
        }

        /// <summary>La partida ha terminado (victoria o derrota final): ya no hay nada que continuar.</summary>
        public static void OnRunFinished() => SaveSystem.Delete();

        /// <summary>Reiniciar desde la pantalla de resultados = nueva partida completa.</summary>
        public static void Restart() => StartNewRun();

        public static void ReturnToMenu()
        {
            ResetRunState();
            if (!Load(MenuScene)) Load(FirstScene); // si aún no se ha creado el menú
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static void ResetRunState()
        {
            Time.timeScale = 1f;
            GameSession.Clear();
            AlertSystem.ResetAll();
            RunStats.Reset();
            Nexus7.Enemies.EnemyPatrol.Paused = false;
        }

        public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public static string BestGrade => PlayerPrefs.GetString(BestGradeKey, "-");

        /// <summary>Guarda la puntuación si es récord. Devuelve true si lo es.</summary>
        public static bool SubmitScore(int score, string grade)
        {
            if (score <= BestScore) return false;
            PlayerPrefs.SetInt(BestScoreKey, score);
            PlayerPrefs.SetString(BestGradeKey, grade);
            PlayerPrefs.Save();
            return true;
        }

        private static bool Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[GameFlow] La escena '{scene}' no está en Build Settings.");
                return false;
            }
            return SceneLoader.Load(scene);   // fundido, barra de carga y autoguardado
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

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

        public static void StartNewRun()
        {
            ResetRunState();
            RunStats.EnsureStarted();
            Load(FirstScene);
        }

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
            SceneManager.LoadScene(scene);
            return true;
        }
    }
}

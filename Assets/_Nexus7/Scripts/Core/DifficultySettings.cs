using UnityEngine;

namespace Nexus7.Core
{
    public enum Difficulty { Facil, Normal, Dificil }

    /// <summary>
    /// Dificultad de la partida (se elige en el menú principal y se guarda en PlayerPrefs).
    ///   Fácil   = daño base de las armas · Normal ×1.5 · Difícil ×2.0
    /// También ajusta la recarga del escudo, la detección de los drones y el multiplicador de puntuación.
    /// </summary>
    public static class DifficultySettings
    {
        public const string PrefsKey = "Nexus7_Dificultad";

        private static Difficulty current = Difficulty.Facil;

        public static Difficulty Current
        {
            get => current;
            set { current = value; PlayerPrefs.SetInt(PrefsKey, (int)value); PlayerPrefs.Save(); }
        }

        public static float DamageMultiplier => current == Difficulty.Dificil ? 2f : current == Difficulty.Normal ? 1.5f : 1f;
        public static float ShieldRegenMultiplier => current == Difficulty.Dificil ? 0.6f : current == Difficulty.Normal ? 0.8f : 1f;
        public static float DetectionMultiplier => current == Difficulty.Dificil ? 1.3f : current == Difficulty.Normal ? 1.15f : 1f;
        public static float ScoreMultiplier => current == Difficulty.Dificil ? 1.3f : current == Difficulty.Normal ? 1.15f : 1f;
        public static string DisplayName => current == Difficulty.Dificil ? "Difícil" : current == Difficulty.Normal ? "Normal" : "Fácil";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Load()
        {
            int v = PlayerPrefs.GetInt(PrefsKey, (int)Difficulty.Facil);
            current = v >= 0 && v <= 2 ? (Difficulty)v : Difficulty.Facil;
        }
    }
}

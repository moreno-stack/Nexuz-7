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

        public static string NameOf(Difficulty d) => d == Difficulty.Dificil ? "Difícil" : d == Difficulty.Normal ? "Normal" : "Fácil";
        public static float DamageMultiplierOf(Difficulty d) => d == Difficulty.Dificil ? 2f : d == Difficulty.Normal ? 1.5f : 1f;
        public static float ScoreMultiplierOf(Difficulty d) => d == Difficulty.Dificil ? 1.3f : d == Difficulty.Normal ? 1.15f : 1f;

        /// <summary>Texto del botón del menú: nombre y multiplicador de daño («Normal ×1,5»).</summary>
        public static string Label(Difficulty d) => $"{NameOf(d)} ×{Num(DamageMultiplierOf(d))}";

        /// <summary>Línea de la pantalla de resultados: nombre y multiplicador de puntuación («Dificultad: Normal ×1,15»).</summary>
        public static string ResultLine(Difficulty d) => $"Dificultad: {NameOf(d)} ×{Num(ScoreMultiplierOf(d))}";

        /// <summary>Número con coma decimal y al menos un decimal (1 → "1,0", 1.15 → "1,15").</summary>
        private static string Num(float v) =>
            v.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Load()
        {
            int v = PlayerPrefs.GetInt(PrefsKey, (int)Difficulty.Facil);
            current = v >= 0 && v <= 2 ? (Difficulty)v : Difficulty.Facil;
        }
    }
}

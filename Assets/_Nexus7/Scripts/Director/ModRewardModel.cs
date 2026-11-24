using System;
using Nexus7.Combat;

namespace Nexus7.Director
{
    /// <summary>Lo que el analizador sabe del jugador al soltar una recompensa (lo rellena ModRewardDirector).</summary>
    public struct RewardInput
    {
        /// <summary>Precisión de los últimos 60 s y disparos en esa ventana (con menos de 8 no cuenta la precisión).</summary>
        public float Accuracy;
        public int Shots;
        public float Camping;
        public float DetectionsPerMinute;
        public float KillsPerMinute;
        /// <summary>El arma de la mano se calienta y es la que más usa (≥ 60 % de los disparos).</summary>
        public bool HotWeaponFavoured;
    }

    /// <summary>
    /// Qué accesorio regala el director al despejar un encuentro o destruir un élite (puro, probado en ModRewardTest).
    /// Puntúa cada accesorio que alguna arma del jugador pueda montar y elige el mejor:
    ///   base 10 · +40 si encaja con cómo juega · −25 si es el mismo que la última vez · azar 0–15.
    ///   · Supresor: juega en sigilo (≤ 0,5 detecciones/min) o se atrinchera (acampada ≥ 50 %).
    ///   · Estabilizador: falla mucho (precisión &lt; 30 % con 8 disparos o más).
    ///   · Cargador ampliado: ritmo agresivo (≥ 1,5 bajas/min).
    ///   · Disipador: usa sobre todo un arma que se calienta.
    /// </summary>
    public static class ModRewardModel
    {
        public const float Base = 10f, Fit = 40f, RepeatPenalty = 25f, RandomWeight = 15f;
        public const float StealthDetections = 0.5f, EntrenchedCamp = 0.5f, PoorAccuracy = 0.3f, AggressiveKpm = 1.5f;
        public const int MinShots = 8;

        /// <param name="usable">¿Alguna arma del jugador puede montarlo ahora? (ArsenalModel.ModTarget != null)</param>
        /// <param name="rng">Azar para desempatar y variar.</param>
        public static WeaponMod Choose(RewardInput input, Func<WeaponMod, bool> usable, WeaponMod last, Random rng)
        {
            var best = WeaponMod.None;
            float bestScore = float.MinValue;
            foreach (var m in WeaponModModel.All)
            {
                if (usable != null && !usable(m)) continue;
                float s = Base + (Fits(m, input) ? Fit : 0f) + (float)rng.NextDouble() * RandomWeight;
                if (m == last) s -= RepeatPenalty;
                if (s > bestScore) { bestScore = s; best = m; }
            }
            return best;
        }

        /// <summary>¿Encaja este accesorio con cómo juega?</summary>
        public static bool Fits(WeaponMod m, RewardInput i)
        {
            switch (m)
            {
                case WeaponMod.Suppressor: return i.DetectionsPerMinute <= StealthDetections || i.Camping >= EntrenchedCamp;
                case WeaponMod.Stabilizer: return i.Shots >= MinShots && i.Accuracy < PoorAccuracy;
                case WeaponMod.ExtendedMag: return i.KillsPerMinute >= AggressiveKpm;
                case WeaponMod.Heatsink: return i.HotWeaponFavoured;
                default: return false;
            }
        }

        /// <summary>Por qué lo ha elegido (para el mensaje).</summary>
        public static string Reason(WeaponMod m, RewardInput i)
        {
            if (!Fits(m, i)) return "para variar tu equipo";
            switch (m)
            {
                case WeaponMod.Suppressor: return i.Camping >= EntrenchedCamp ? "disparas desde la misma posición" : "te mueves sin que te vean";
                case WeaponMod.Stabilizer: return "fallas muchos disparos";
                case WeaponMod.ExtendedMag: return "llevas un ritmo agresivo";
                case WeaponMod.Heatsink: return "tu arma favorita se calienta";
                default: return "";
            }
        }
    }
}

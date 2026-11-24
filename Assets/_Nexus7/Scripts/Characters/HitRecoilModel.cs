using System;

namespace Nexus7.Characters
{
    /// <summary>
    /// Retroceso al recibir un impacto (puro, probado): el torso se echa hacia atrás hasta 12° × fuerza en 0,06 s
    /// y vuelve suavemente; en total 0,25 s. La fuerza sale del daño (20 de daño = 1; entre 0,4 y 1,5).
    /// </summary>
    public static class HitRecoilModel
    {
        public const float MaxDegrees = 12f, Duration = 0.25f, PeakTime = 0.06f, DamageForFull = 20f;

        public static float StrengthFor(float damage) => Math.Max(0.4f, Math.Min(1.5f, damage / DamageForFull));

        /// <summary>Grados hacia atrás 'elapsed' segundos después del impacto (0 fuera del retroceso).</summary>
        public static float Angle(float elapsed, float strength)
        {
            if (elapsed < 0f || elapsed >= Duration || strength <= 0f) return 0f;
            float k = elapsed < PeakTime ? elapsed / PeakTime : 1f - EaseOut((elapsed - PeakTime) / (Duration - PeakTime));
            return MaxDegrees * strength * Math.Max(0f, Math.Min(1f, k));
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}

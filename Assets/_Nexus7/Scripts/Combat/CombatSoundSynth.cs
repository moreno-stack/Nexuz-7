using System;

namespace Nexus7.Combat
{
    public enum CombatSound { Pistol, Rifle, Shotgun, Railgun, Reload, Overheat, Switch, Empty, HitMetal, Explosion, AmmoPickup }

    /// <summary>
    /// Sonidos del combate generados por código (el proyecto no tiene archivos de audio), como ProceduralAudio:
    /// muestras mono en [−1, 1] a 22 050 Hz. Lógica pura y determinista (semilla fija por sonido).
    /// </summary>
    public static class CombatSoundSynth
    {
        public const int SampleRate = 22050;

        public static float[] Make(CombatSound kind)
        {
            switch (kind)
            {
                case CombatSound.Pistol:     return Shot(1400, 300, 0.16f, 0.35f, 0.55f, 1);
                case CombatSound.Rifle:      return Shot(900, 180, 0.11f, 0.55f, 0.5f, 2);
                case CombatSound.Shotgun:    return Shot(500, 60, 0.32f, 0.85f, 0.7f, 3);
                case CombatSound.Railgun:    return Shot(2600, 90, 0.55f, 0.4f, 0.75f, 4);
                case CombatSound.Reload:     return Clicks(new[] { 0.0, 0.18, 0.32 }, 0.36f, 5);
                case CombatSound.Overheat:   return Shot(300, 900, 0.5f, 0.6f, 0.4f, 6);
                case CombatSound.Switch:     return Clicks(new[] { 0.0, 0.07 }, 0.12f, 7);
                case CombatSound.Empty:      return Clicks(new[] { 0.0 }, 0.06f, 8);
                case CombatSound.HitMetal:   return Shot(2200, 1600, 0.09f, 0.3f, 0.45f, 9);
                case CombatSound.Explosion:  return Shot(140, 40, 0.9f, 0.95f, 0.8f, 10);
                default:                     return Shot(660, 1320, 0.18f, 0.0f, 0.4f, 11);
            }
        }

        /// <summary>Barrido tonal + ruido con caída exponencial. 'noise' = proporción de ruido (0..1).</summary>
        private static float[] Shot(double fromHz, double toHz, float seconds, float noise, float amplitude, int seed)
        {
            int n = (int)(seconds * SampleRate);
            var s = new float[n];
            var rnd = new Random(seed);
            double phase = 0, low = 0;
            for (int i = 0; i < n; i++)
            {
                double k = (double)i / n;
                double f = fromHz + (toHz - fromHz) * Math.Sqrt(k);
                phase += 2.0 * Math.PI * f / SampleRate;
                double t = (double)i / SampleRate;
                double env = Math.Min(1.0, t / 0.002) * Math.Exp(-5.0 * k);
                low += (rnd.NextDouble() * 2 - 1 - low) * 0.35;   // ruido filtrado (más grave)
                s[i] = (float)((Math.Sin(phase) * (1 - noise) + low * noise * 1.8) * env * amplitude);
            }
            return s;
        }

        private static float[] Clicks(double[] at, float seconds, int seed)
        {
            int n = (int)(seconds * SampleRate);
            var s = new float[n];
            var rnd = new Random(seed);
            foreach (var start in at)
            {
                int i0 = (int)(start * SampleRate);
                for (int i = 0; i < 500 && i0 + i < n; i++)
                {
                    double env = Math.Exp(-i / 90.0);
                    s[i0 + i] += (float)((rnd.NextDouble() * 2 - 1) * 0.5 + Math.Sin(i * 0.9) * 0.3) * (float)env * 0.6f;
                }
            }
            return s;
        }
    }
}

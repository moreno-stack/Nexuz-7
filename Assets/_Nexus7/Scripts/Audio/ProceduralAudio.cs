using System;

namespace Nexus7.Audio
{
    public enum UiSound { Hover, Click, Back, Test }

    /// <summary>
    /// Música y sonidos generados por código (el proyecto no tiene archivos de audio). Lógica pura: devuelve muestras
    /// mono en [−1, 1] a 22 050 Hz; MusicPlayer y UISounds las convierten en AudioClip.
    /// Las pistas en bucle usan frecuencias con un número entero de ciclos en su duración, así el final empalma
    /// con el principio sin chasquido.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 22050;
        private const double Tau = Math.PI * 2.0;

        /// <summary>Pad ambiental del menú (La menor con novena) con trémolo lento y una campana cada 4 s.</summary>
        public static float[] MenuPad(float seconds)
        {
            int n = (int)(seconds * SampleRate);
            var s = new float[n];
            // Múltiplos de 1/16 Hz: un número entero de ciclos en 16 s
            double[] chord = { 110.0, 164.8125, 220.0, 246.9375, 261.625, 329.625 };
            double[] bells = { 659.25, 880.0, 987.75, 783.9375 };
            double lfo = 2.0 / seconds;   // trémolo: 2 ciclos por vuelta
            double bellEvery = seconds / bells.Length;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double pad = 0;
                for (int k = 0; k < chord.Length; k++)
                {
                    double f = Snap(chord[k], seconds);
                    double detune = Snap(f + 0.125, seconds);   // segunda voz ligeramente desafinada (coro)
                    pad += Math.Sin(Tau * f * t) + 0.6 * Math.Sin(Tau * detune * t + k);
                }
                pad *= 0.05 * (0.75 + 0.25 * Math.Sin(Tau * lfo * t));

                int b = (int)(t / bellEvery);
                double bt = t - b * bellEvery;
                double bell = Math.Sin(Tau * bells[b % bells.Length] * bt) * Math.Min(1.0, bt / 0.01) * Math.Exp(-bt / 0.7) * 0.12;
                s[i] = (float)(pad + bell);
            }
            return Normalize(s, 0.55f);
        }

        /// <summary>Zumbido grave y tenso de la nave (fondo de las fases), más bajo que la música del menú.</summary>
        public static float[] ShipDrone(float seconds)
        {
            int n = (int)(seconds * SampleRate);
            var s = new float[n];
            double f1 = Snap(55.0, seconds), f2 = Snap(82.5, seconds), f3 = Snap(110.0, seconds), f4 = Snap(440.0, seconds);
            double pulse = 3.0 / seconds, shimmer = 2.0 / seconds;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double low = Math.Sin(Tau * f1 * t) + 0.7 * Math.Sin(Tau * f2 * t) + 0.35 * Math.Sin(Tau * f3 * t);
                double p = 0.7 + 0.3 * Math.Sin(Tau * pulse * t);
                double high = 0.08 * Math.Sin(Tau * f4 * t) * (0.5 + 0.5 * Math.Sin(Tau * shimmer * t));
                s[i] = (float)(low * p * 0.3 + high);
            }
            return Normalize(s, 0.3f);
        }

        /// <summary>Sonidos cortos de la interfaz (empiezan y acaban en silencio).</summary>
        public static float[] Ui(UiSound kind)
        {
            switch (kind)
            {
                case UiSound.Hover: return Tone(1200, 1200, 0.04f, 0.35f);
                case UiSound.Click: return Tone(660, 880, 0.09f, 0.5f);
                case UiSound.Back:  return Tone(520, 330, 0.12f, 0.5f);
                default:            return Tone(880, 880, 0.2f, 0.45f);
            }
        }

        /// <summary>Tono con barrido de frecuencia y envolvente (ataque de 5 ms y caída hasta 0).</summary>
        private static float[] Tone(double fromHz, double toHz, float seconds, float amplitude)
        {
            int n = (int)(seconds * SampleRate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double k = (double)i / n;
                double f = fromHz + (toHz - fromHz) * k;
                phase += Tau * f / SampleRate;
                double t = (double)i / SampleRate;
                double env = Math.Min(1.0, t / 0.005) * (1.0 - k) * (1.0 - k);
                s[i] = (float)(Math.Sin(phase) * env * amplitude);
            }
            return s;
        }

        /// <summary>Ajusta la frecuencia para que quepa un número entero de ciclos en la duración del bucle.</summary>
        private static double Snap(double hz, float seconds) => Math.Round(hz * seconds) / seconds;

        private static float[] Normalize(float[] s, float peak)
        {
            float max = 0f;
            foreach (var x in s) max = Math.Max(max, Math.Abs(x));
            if (max < 1e-6f) return s;
            float k = peak / max;
            for (int i = 0; i < s.Length; i++) s[i] *= k;
            return s;
        }
    }
}

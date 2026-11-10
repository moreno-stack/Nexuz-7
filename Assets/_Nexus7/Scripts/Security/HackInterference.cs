using System;
using Nexus7.Core;

namespace Nexus7.Security
{
    /// <summary>
    /// Interferencias del minijuego de hackeo (lógica pura): de vez en cuando aparece «¡INTERFERENCIA!» y hay que pulsar
    /// [Espacio] antes de que se acabe el margen; si no, cuenta como un error de la secuencia.
    ///
    ///   Alerta     Cada          Margen   Máximo
    ///   Normal     —             1,4 s    0 en fácil · 1 en media y difícil
    ///   Amarilla   5–7 s         1,1 s    sin límite
    ///   Roja       3–4,5 s       0,8 s    sin límite
    /// La primera nunca llega antes de 1,5 s de hackeo.
    /// </summary>
    public class HackInterference
    {
        public enum Event { None, Started, Missed }

        public const float FirstDelay = 1.5f;

        public bool Active { get; private set; }
        public float WindowLeft { get; private set; }
        public float Window { get; }
        public int Cleared { get; private set; }
        public int Missed { get; private set; }
        public int Count => Cleared + Missed + (Active ? 1 : 0);

        private readonly float minInterval, maxInterval;
        private readonly int maxCount;
        private readonly Random rng;
        private float untilNext;

        public static void GetParams(AlertLevel level, HackDifficulty difficulty, out float minInterval, out float maxInterval,
                                     out float window, out int maxCount)
        {
            switch (level)
            {
                case AlertLevel.Red:    minInterval = 3f; maxInterval = 4.5f; window = 0.8f; maxCount = 99; break;
                case AlertLevel.Yellow: minInterval = 5f; maxInterval = 7f;   window = 1.1f; maxCount = 99; break;
                default:
                    minInterval = 5f; maxInterval = 7f; window = 1.4f;
                    maxCount = difficulty == HackDifficulty.Easy ? 0 : 1;
                    break;
            }
        }

        public HackInterference(AlertLevel level, HackDifficulty difficulty, int seed)
        {
            GetParams(level, difficulty, out minInterval, out maxInterval, out float window, out maxCount);
            Window = window;
            rng = new Random(seed);
            untilNext = FirstDelay + (float)rng.NextDouble() * 0.5f;
        }

        public Event Tick(float dt)
        {
            if (Active)
            {
                WindowLeft -= dt;
                if (WindowLeft > 0f) return Event.None;
                Active = false;
                Missed++;
                ScheduleNext();
                return Event.Missed;
            }
            if (Count >= maxCount) return Event.None;
            untilNext -= dt;
            if (untilNext > 0f) return Event.None;
            Active = true;
            WindowLeft = Window;
            return Event.Started;
        }

        /// <summary>El jugador pulsa [Espacio]. true si había una interferencia y la ha eliminado.</summary>
        public bool Respond()
        {
            if (!Active) return false;
            Active = false;
            Cleared++;
            ScheduleNext();
            return true;
        }

        private void ScheduleNext() =>
            untilNext = minInterval + (float)rng.NextDouble() * (maxInterval - minInterval);
    }
}

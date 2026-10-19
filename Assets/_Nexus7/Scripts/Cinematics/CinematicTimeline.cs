using System.Collections.Generic;

namespace Nexus7.Cinematics
{
    /// <summary>Línea de tiempo de una cinemática (lógica pura, probada con Mono): qué plano toca y en qué punto.</summary>
    public class CinematicTimeline
    {
        private readonly List<float> durations = new List<float>();

        public float Total { get; private set; }

        public CinematicTimeline(IList<float> shotDurations)
        {
            if (shotDurations != null)
                foreach (var d in shotDurations) { float v = d > 0.01f ? d : 0.01f; durations.Add(v); Total += v; }
        }

        public int Count => durations.Count;
        public bool IsFinished(float t) => t >= Total;

        /// <summary>Plano actual y avance dentro de él (0–1). Pasado el final, se queda en el último fotograma.</summary>
        public void Evaluate(float t, out int shot, out float local01)
        {
            shot = 0; local01 = 0f;
            if (durations.Count == 0) return;
            float acc = 0f;
            for (int i = 0; i < durations.Count; i++)
            {
                if (t < acc + durations[i] || i == durations.Count - 1)
                {
                    shot = i;
                    float k = (t - acc) / durations[i];
                    local01 = k < 0f ? 0f : k > 1f ? 1f : k;
                    return;
                }
                acc += durations[i];
            }
        }

        /// <summary>Segundos transcurridos dentro del plano actual.</summary>
        public float LocalSeconds(float t, int shot)
        {
            float acc = 0f;
            for (int i = 0; i < shot && i < durations.Count; i++) acc += durations[i];
            return t - acc;
        }

        /// <summary>Índice de la línea de subtítulo visible en 'localSeconds', o -1.</summary>
        public static int ActiveLine(IList<SubtitleLine> lines, float localSeconds)
        {
            if (lines == null) return -1;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var l = lines[i];
                if (l != null && localSeconds >= l.at && localSeconds < l.at + l.hold) return i;
            }
            return -1;
        }
    }

    /// <summary>"Mantén pulsado para saltar": true cuando se ha mantenido 'holdSeconds'. Soltar reinicia.</summary>
    public class SkipHold
    {
        private readonly float holdSeconds;
        private float held;

        public SkipHold(float holdSeconds) { this.holdSeconds = holdSeconds > 0f ? holdSeconds : 1f; }

        public float Progress => held / holdSeconds;

        public bool Update(bool pressed, float dt)
        {
            if (!pressed) { held = 0f; return false; }
            held += dt;
            if (held >= holdSeconds - 0.0001f) { held = holdSeconds; return true; }
            return false;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    public enum RobotKind { Security, Guard, Research, Elite }

    /// <summary>
    /// Oleadas de cada fase (diseño ia-combate-design.md, Normal) y cómo se ajustan (puro, probado):
    ///   · dificultad: ×0,7 Fácil, ×1 Normal, ×1,3 Difícil (redondeando, mínimo 1 por oleada);
    ///   · director: con tensión ≥ 0,7 la última oleada trae 1 robot más; con ≤ 0,3 la oleada más grande trae 1 menos.
    /// </summary>
    public static class EncounterCatalog
    {
        private static readonly RobotKind S = RobotKind.Security, G = RobotKind.Guard, R = RobotKind.Research, E = RobotKind.Elite;

        public static RobotKind[][] ForScene(string scene)
        {
            switch (scene)
            {
                case "SalaInicial":   return new[] { new[] { S, S } };
                case "AlmacenCarga":  return new[] { new[] { S, S }, new[] { G, S } };
                case "SalaEnergia":   return new[] { new[] { S, S, R }, new[] { G, G } };
                case "Laboratorio":   return new[] { new[] { S, S }, new[] { G, G }, new[] { E, S } };
                case "CentroControl": return new[] { new[] { S, S, S }, new[] { G, G, R }, new[] { E, G, G } };
                case "Evacuacion":    return new[] { new[] { G, G, S }, new[] { E, E } };
                default:              return new RobotKind[0][];
            }
        }

        public static float DifficultyFactor(int difficulty) => difficulty <= 0 ? 0.7f : difficulty >= 2 ? 1.3f : 1f;

        /// <summary>Oleadas listas para aparecer según la dificultad y la tensión del director.</summary>
        public static List<List<RobotKind>> Scale(RobotKind[][] waves, int difficulty, float tension)
        {
            var result = new List<List<RobotKind>>();
            if (waves == null) return result;
            float f = DifficultyFactor(difficulty);
            foreach (var w in waves)
            {
                if (w == null || w.Length == 0) continue;
                int n = Math.Max(1, (int)Math.Round(w.Length * f, MidpointRounding.AwayFromZero));
                var list = new List<RobotKind>(n);
                for (int i = 0; i < n; i++) list.Add(w[i % w.Length]);   // más robots: repite los tipos de la oleada
                result.Add(list);
            }
            if (result.Count == 0) return result;
            if (tension >= 0.7f) { var last = result[result.Count - 1]; last.Add(last[0]); }
            else if (tension <= 0.3f)
            {
                int big = 0;
                for (int i = 1; i < result.Count; i++) if (result[i].Count > result[big].Count) big = i;
                if (result[big].Count > 1) result[big].RemoveAt(result[big].Count - 1);
            }
            return result;
        }
    }

    /// <summary>Qué activa el encuentro: un objetivo de la fase; los puntos seguros solo si la fase no tiene objetivos.</summary>
    public static class EncounterTriggerRule
    {
        public const string Checkpoint = "punto_seguro";

        public static bool ShouldActivate(string kind, bool phaseHasObjectives) =>
            !string.IsNullOrEmpty(kind) && (kind != Checkpoint || !phaseHasObjectives);

        /// <summary>Fases sin objetivos ni puntos seguros: al alejarse 20 m de donde empezó.</summary>
        public const float WanderDistance = 20f;
    }

    public enum EncounterState { Idle, Countdown, Active, Cleared }
    public enum EncounterEvent { None, SpawnWave, Announce, Cleared }

    /// <summary>
    /// Secuencia de oleadas (pura, probada):
    ///   Activar (una vez) → cuenta atrás 3 s → OLEADA 1 → cuando quedan 1 o 0 robots de la oleada, o a los 25 s,
    ///   aviso «¡REFUERZOS!» y cuenta atrás 3 s → OLEADA 2 → ... → tras la última, sin robots vivos → ZONA DESPEJADA.
    ///   Reset() (el jugador cae) lo deja como antes de activarse, y se puede volver a activar.
    /// </summary>
    public sealed class WaveModel
    {
        public const float CountdownTime = 3f, MaxWaveTime = 25f;
        public const int AdvanceWhenAlive = 1;

        public int WaveCount { get; }
        public EncounterState State { get; private set; } = EncounterState.Idle;
        /// <summary>Índice de la oleada en curso (−1 antes de la primera).</summary>
        public int CurrentWave { get; private set; } = -1;
        public float CountdownLeft(float time) => State == EncounterState.Countdown ? Math.Max(0f, countdownEnd - time) : 0f;

        private float countdownEnd, waveStart;

        public WaveModel(int waveCount) { WaveCount = Math.Max(0, waveCount); }

        public bool Activate(float time)
        {
            if (State != EncounterState.Idle || WaveCount == 0) return false;
            State = EncounterState.Countdown;
            countdownEnd = time + CountdownTime;
            return true;
        }

        /// <param name="aliveInWave">Robots vivos de la oleada en curso.</param>
        public EncounterEvent Tick(float time, int aliveInWave)
        {
            switch (State)
            {
                case EncounterState.Countdown:
                    if (time < countdownEnd) return EncounterEvent.None;
                    State = EncounterState.Active;
                    CurrentWave++;
                    waveStart = time;
                    return EncounterEvent.SpawnWave;
                case EncounterState.Active:
                    bool last = CurrentWave >= WaveCount - 1;
                    if (last)
                    {
                        if (aliveInWave > 0) return EncounterEvent.None;
                        State = EncounterState.Cleared;
                        return EncounterEvent.Cleared;
                    }
                    if (aliveInWave > AdvanceWhenAlive && time < waveStart + MaxWaveTime) return EncounterEvent.None;
                    State = EncounterState.Countdown;
                    countdownEnd = time + CountdownTime;
                    return EncounterEvent.Announce;
            }
            return EncounterEvent.None;
        }

        public void Reset()
        {
            State = EncounterState.Idle;
            CurrentWave = -1;
        }
    }

    /// <summary>
    /// Dónde aparece cada robot de una oleada (puro, probado). Entre los puntos candidatos:
    ///   descartados a menos de 8 m o a más de 35 m del jugador; −60 si está a la vista (35°, 18 m);
    ///   −30 si se usó en el intento anterior (anti-repetición); prefiere 12–20 m; y un poco de azar.
    /// Nunca repite punto en la misma oleada si hay suficientes.
    /// </summary>
    public static class EncounterSpawnPicker
    {
        public const float MinDistance = 8f, MaxDistance = 35f, ViewDistance = 18f, ViewHalfAngle = 35f;

        public static List<int> Pick(IList<float> xs, IList<float> zs, float px, float pz, float fx, float fz,
                                     ICollection<int> recentlyUsed, int count, Random rng)
        {
            var scored = new List<KeyValuePair<float, int>>();
            for (int i = 0; i < xs.Count; i++)
            {
                float dx = xs[i] - px, dz = zs[i] - pz;
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                if (d < MinDistance || d > MaxDistance) continue;
                float s = 100f;
                if (InView(dx, dz, d, fx, fz)) s -= 60f;
                if (recentlyUsed != null && recentlyUsed.Contains(i)) s -= 30f;
                if (d < 12f) s -= (12f - d) * 2f;
                else if (d > 20f) s -= (d - 20f);
                s += (float)rng.NextDouble() * 15f;
                scored.Add(new KeyValuePair<float, int>(s, i));
            }
            scored.Sort((a, b) => b.Key.CompareTo(a.Key));
            var result = new List<int>(count);
            for (int i = 0; i < count && scored.Count > 0; i++) result.Add(scored[i % scored.Count].Value);
            return result;
        }

        public static bool InView(float dx, float dz, float d, float fx, float fz)
        {
            if (d > ViewDistance) return false;
            if (d < 1e-3f) return true;
            double fl = Math.Sqrt(fx * fx + fz * fz);
            if (fl < 1e-4) return false;
            double cos = (dx * fx + dz * fz) / (d * fl);
            return cos >= Math.Cos(ViewHalfAngle * Math.PI / 180.0);
        }
    }
}

using System;

namespace Nexus7.Security
{
    public enum HackDifficulty { Easy, Medium, Hard }
    public enum HackSymbol { Up, Left, Down, Right }

    /// <summary>
    /// Lógica pura del minijuego de hackeo (sin Unity, se puede probar aislada):
    /// una secuencia de códigos que hay que introducir EN ORDEN antes de que acabe el tiempo.
    ///
    ///   · Cada acierto avanza.  · Cada error: −ErrorPenalty segundos (y la UI avisa a la seguridad).
    ///   · Demasiados errores o tiempo agotado → FALLO.
    ///
    ///   Dificultad   Longitud   Tiempo   Errores permitidos   Penalización
    ///   Fácil            5        10 s          3                 2 s
    ///   Media            7        12 s          3                 3 s
    ///   Difícil          9        14 s          2                 4 s
    /// En Alerta Roja la UI usa timeScale 0.8 y +1 de longitud.
    /// </summary>
    public class HackSequence
    {
        public enum StepResult { Correct, Wrong, Completed, Failed, Ignored }

        public HackSymbol[] Symbols { get; }
        public int Index { get; private set; }
        public int Errors { get; private set; }
        public int MaxErrors { get; }
        public float TimeLimit { get; }
        public float TimeLeft { get; private set; }
        public float ErrorPenalty { get; }
        public bool Succeeded { get; private set; }
        public bool Failed { get; private set; }
        public bool Finished => Succeeded || Failed;
        public float Progress => Symbols.Length == 0 ? 1f : (float)Index / Symbols.Length;
        public HackSymbol Current => Symbols[Math.Min(Index, Symbols.Length - 1)];

        public static void GetParams(HackDifficulty d, out int length, out float time, out int maxErrors, out float penalty)
        {
            switch (d)
            {
                case HackDifficulty.Easy:   length = 5; time = 10f; maxErrors = 3; penalty = 2f; break;
                case HackDifficulty.Medium: length = 7; time = 12f; maxErrors = 3; penalty = 3f; break;
                default:                    length = 9; time = 14f; maxErrors = 2; penalty = 4f; break;
            }
        }

        public HackSequence(HackDifficulty difficulty, int seed, float timeScale = 1f, int extraLength = 0)
        {
            GetParams(difficulty, out int length, out float time, out int maxErrors, out float penalty);
            length += Math.Max(0, extraLength);
            var rng = new Random(seed);
            Symbols = new HackSymbol[length];
            for (int i = 0; i < length; i++) Symbols[i] = (HackSymbol)rng.Next(4);
            MaxErrors = maxErrors;
            ErrorPenalty = penalty;
            TimeLimit = time * Math.Max(0.1f, timeScale);
            TimeLeft = TimeLimit;
        }

        public StepResult Input(HackSymbol symbol)
        {
            if (Finished) return StepResult.Ignored;

            if (symbol == Symbols[Index])
            {
                Index++;
                if (Index >= Symbols.Length) { Succeeded = true; return StepResult.Completed; }
                return StepResult.Correct;
            }

            Errors++;
            TimeLeft -= ErrorPenalty;
            if (Errors > MaxErrors || TimeLeft <= 0f) { Failed = true; return StepResult.Failed; }
            return StepResult.Wrong;
        }

        /// <summary>Avanza el reloj. Devuelve true si el tiempo acaba de agotarse.</summary>
        public bool Tick(float deltaTime)
        {
            if (Finished) return false;
            TimeLeft -= deltaTime;
            if (TimeLeft > 0f) return false;
            TimeLeft = 0f;
            Failed = true;
            return true;
        }

        public static string Key(HackSymbol s)
        {
            switch (s)
            {
                case HackSymbol.Up:   return "W";
                case HackSymbol.Left: return "A";
                case HackSymbol.Down: return "S";
                default:              return "D";
            }
        }
    }
}

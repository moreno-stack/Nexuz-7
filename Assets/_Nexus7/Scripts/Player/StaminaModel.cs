using System;

namespace Nexus7.Player
{
    /// <summary>
    /// Estamina para correr (lógica pura, se prueba fuera de Unity):
    ///   · 100 puntos; correr gasta 17/s (unos 6 s) × dificultad; un salto, 8.
    ///   · Tras 1 s sin correr se recupera: 14/s caminando, 22/s quieto o agachado.
    ///   · Al llegar a 0 queda AGOTADO: no puede correr hasta recuperar el 35 % (evita correr a tirones).
    /// </summary>
    public class StaminaModel
    {
        public const float Max = 100f;
        public float DrainPerSecond = 17f;
        public float JumpCost = 8f;
        public float RegenWalking = 14f;
        public float RegenIdle = 22f;
        public float RegenDelay = 1f;
        public float RecoverThreshold = 35f;

        public float DrainMultiplier { get; set; }
        public float Current { get; private set; } = Max;
        public bool Exhausted { get; private set; }
        public bool CanRun => !Exhausted && Current > 0f;
        public float Normalized => Current / Max;

        private float sinceSpent = 999f;

        public StaminaModel(float drainMultiplier) { DrainMultiplier = drainMultiplier; }

        /// <summary>Fácil ×0,75 · Normal ×1 · Difícil ×1,25 (índice de Difficulty).</summary>
        public static float DrainMultiplierFor(int difficultyIndex) =>
            difficultyIndex <= 0 ? 0.75f : difficultyIndex == 1 ? 1f : 1.25f;

        /// <summary>Avanza el tiempo. Devuelve true en el instante en que se agota.</summary>
        public bool Tick(float dt, bool running, bool moving)
        {
            if (dt <= 0f) return false;
            if (running && moving && CanRun)
            {
                sinceSpent = 0f;
                return Spend(DrainPerSecond * DrainMultiplier * dt);
            }
            float before = sinceSpent;
            sinceSpent += dt;
            // Solo cuenta el tiempo que pasa de la espera (RegenDelay) dentro de este paso
            float regenTime = Math.Max(0f, sinceSpent - Math.Max(before, RegenDelay));
            if (regenTime > 0f)
            {
                Current = Math.Min(Max, Current + (moving ? RegenWalking : RegenIdle) * regenTime);
                if (Exhausted && Current >= RecoverThreshold) Exhausted = false;
            }
            return false;
        }

        /// <summary>Un salto gasta estamina (saltar siempre se puede).</summary>
        public bool Jump()
        {
            sinceSpent = 0f;
            return Spend(JumpCost);
        }

        public void Refill()
        {
            Current = Max;
            Exhausted = false;
            sinceSpent = 999f;
        }

        private bool Spend(float amount)
        {
            Current = Math.Max(0f, Current - amount);
            if (Current > 0f || Exhausted) return false;
            Exhausted = true;
            return true;
        }
    }
}

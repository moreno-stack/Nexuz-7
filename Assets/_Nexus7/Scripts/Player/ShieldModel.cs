namespace Nexus7.Player
{
    /// <summary>
    /// Lógica pura del escudo del traje (sin Unity, probada con Mono):
    ///   Absorb → el escudo se lleva el daño primero y devuelve lo que sobra para la vida.
    ///   Tick   → tras RegenDelay segundos sin daño recarga RegenPerSecond × multiplicador de dificultad.
    /// </summary>
    public class ShieldModel
    {
        public const float RegenDelay = 6f;
        public const float RegenPerSecond = 8f;

        public float Capacity { get; private set; }
        public float Value { get; private set; }
        public float LastHitTime { get; private set; } = -999f;

        public ShieldModel(float capacity)
        {
            Capacity = capacity < 0f ? 0f : capacity;
            Value = Capacity;
        }

        /// <summary>Devuelve el daño que el escudo NO pudo absorber.</summary>
        public float Absorb(float damage, float now)
        {
            if (damage <= 0f) return 0f;
            LastHitTime = now;
            float absorbed = damage < Value ? damage : Value;
            Value -= absorbed;
            return damage - absorbed;
        }

        public void Tick(float dt, float now, float regenMultiplier)
        {
            if (Value >= Capacity || dt <= 0f || now - LastHitTime < RegenDelay) return;
            Value += RegenPerSecond * regenMultiplier * dt;
            if (Value > Capacity) Value = Capacity;
        }

        public void Refill() => Value = Capacity;
    }
}

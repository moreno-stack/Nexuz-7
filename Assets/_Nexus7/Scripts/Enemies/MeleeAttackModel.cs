namespace Nexus7.Enemies
{
    public enum MeleeEvent { None, Hit, Dodged, Finished }

    /// <summary>
    /// Lógica pura del golpe cuerpo a cuerpo de un robot (probada con Mono):
    ///   Begin → Anticipación (windup) → impacto: Hit si el jugador sigue en arco y alcance, si no Dodged
    ///   → Recuperación (recovery) → Finished. Cancel() (PEM, aturdimiento) lo corta en cualquier momento.
    /// </summary>
    public class MeleeAttackModel
    {
        public enum Phase { Idle, Windup, Recovery }

        private readonly float windup, recovery;
        private float start;

        public Phase Current { get; private set; } = Phase.Idle;

        public MeleeAttackModel(float windup, float recovery)
        {
            this.windup = windup;
            this.recovery = recovery;
        }

        public bool Begin(float now)
        {
            if (Current != Phase.Idle) return false;
            Current = Phase.Windup;
            start = now;
            return true;
        }

        public MeleeEvent Tick(float now, bool targetInArc, bool targetInRange)
        {
            switch (Current)
            {
                case Phase.Windup:
                    if (now - start < windup) return MeleeEvent.None;
                    Current = Phase.Recovery;
                    start += windup;
                    return targetInArc && targetInRange ? MeleeEvent.Hit : MeleeEvent.Dodged;
                case Phase.Recovery:
                    if (now - start < recovery) return MeleeEvent.None;
                    Current = Phase.Idle;
                    return MeleeEvent.Finished;
                default:
                    return MeleeEvent.None;
            }
        }

        public void Cancel() => Current = Phase.Idle;

        /// <summary>¿Está el objetivo dentro del arco? 'angle' = grados entre el frente del robot y el objetivo.</summary>
        public static bool InArc(float angle, float arcDegrees) => angle <= arcDegrees * 0.5f;
    }
}

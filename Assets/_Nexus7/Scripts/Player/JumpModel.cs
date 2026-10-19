using System;

namespace Nexus7.Player
{
    /// <summary>
    /// Lógica pura del salto (probada con Mono):
    ///   · velocidad inicial para alcanzar 'height' con la gravedad dada
    ///   · margen ("coyote time") tras dejar el suelo, sin doble salto
    ///   · altura de caída para el ruido al aterrizar (0 / 6 / 9 m de radio)
    /// </summary>
    public class JumpModel
    {
        private const float RegroundDelay = 0.15f; // tras saltar, el suelo tarda en "soltar" al CharacterController

        private readonly float height, gravity, coyote;
        private bool airborne, jumped;
        private float lastGroundedTime = -999f, jumpTime = -999f, peakY;

        public JumpModel(float height, float gravity, float coyote)
        {
            this.height = height;
            this.gravity = gravity;
            this.coyote = coyote;
        }

        public bool IsAirborne => airborne;
        private float suppressUntil = -999f;

        /// <summary>Los aterrizajes durante 'seconds' no cuentan como caída (p. ej. al reaparecer en un punto algo elevado).</summary>
        public void SuppressLanding(float now, float seconds) => suppressUntil = now + seconds;

        /// <summary>Llamar cada frame. Al aterrizar devuelve la altura de la caída (desde el punto más alto); si no, 0.</summary>
        public float NotifyGrounded(bool grounded, float now, float y)
        {
            if (grounded)
            {
                float fall = 0f;
                if (airborne) { fall = now <= suppressUntil ? 0f : peakY - y; airborne = false; }
                if (now - jumpTime > RegroundDelay) jumped = false;
                lastGroundedTime = now;
                return fall < 0f ? 0f : fall;
            }
            if (!airborne) { airborne = true; peakY = y; lastGroundedTime = now; }
            else if (y > peakY) peakY = y;
            return 0f;
        }

        public bool TryJump(bool grounded, bool crouching, bool inputLocked, float now, out float verticalVelocity)
        {
            verticalVelocity = 0f;
            if (crouching || inputLocked || jumped) return false;
            if (!grounded && now - lastGroundedTime > coyote) return false;
            verticalVelocity = (float)Math.Sqrt(2.0 * Math.Abs(gravity) * height);
            jumped = true;
            jumpTime = now;
            return true;
        }

        public float LandingNoiseRadius(float fallHeight) => fallHeight < 0.3f ? 0f : fallHeight <= 2f ? 6f : 9f;
    }
}

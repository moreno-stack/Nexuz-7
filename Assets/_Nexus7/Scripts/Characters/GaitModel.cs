using System;

namespace Nexus7.Characters
{
    /// <summary>Ciclo de pasos procedural (lógica pura): amplitud y frecuencia según la velocidad.</summary>
    public class GaitModel
    {
        public const float MaxLegSwing = 45f;

        /// <summary>Fase del ciclo en radianes.</summary>
        public float Phase { get; private set; }

        public void Advance(float speed, float dt)
        {
            if (speed <= 0.05f) return;
            Phase += (1.6f + 0.9f * speed) * dt * (float)Math.PI;
            if (Phase > 1000f) Phase -= 2f * (float)Math.PI * 100f;
        }

        public float LegSwingDegrees(float speed) => speed <= 0f ? 0f : Math.Min(MaxLegSwing, 12.5f * speed);
        public float ArmSwingDegrees(float speed) => 0.7f * LegSwingDegrees(speed);
    }

    /// <summary>Postura del brazo derecho durante un golpe (lógica pura): anticipación → golpe → reposo.</summary>
    public static class AttackPose
    {
        public const float WindupAngle = -120f, StrikeAngle = 40f, RecoverTime = 0.3f;

        public static float RightArmAngle(float t, float windup, float strike)
        {
            if (t <= 0f) return 0f;
            if (t < windup) return WindupAngle * Smooth(t / windup);
            if (t < windup + strike) return WindupAngle + (StrikeAngle - WindupAngle) * Smooth((t - windup) / strike);
            float r = t - windup - strike;
            if (r < RecoverTime) return StrikeAngle * (1f - Smooth(r / RecoverTime));
            return 0f;
        }

        private static float Smooth(float x) { x = x < 0f ? 0f : x > 1f ? 1f : x; return x * x * (3f - 2f * x); }
    }
}

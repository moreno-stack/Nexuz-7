using System;

namespace Nexus7.Combat
{
    public enum HealthResult { Ignored, Damaged, Killed }

    /// <summary>
    /// Vida de un enemigo (lógica pura, probada en Tests/Suites/WeaponTest.cs):
    /// recibe daño, muere una sola vez y, muerto, ya no recibe más.
    /// </summary>
    public sealed class HealthModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Normalized => Max > 0f ? Current / Max : 0f;

        public HealthModel(float max)
        {
            Max = Math.Max(1f, max);
            Current = Max;
        }

        public HealthResult Apply(float amount)
        {
            if (IsDead || amount <= 0f) return HealthResult.Ignored;
            Current = Math.Max(0f, Current - amount);
            return IsDead ? HealthResult.Killed : HealthResult.Damaged;
        }

        /// <summary>Cambia la vida máxima conservando el porcentaje (el director puede blindar o debilitar robots vivos).</summary>
        public void Rescale(float newMax)
        {
            if (IsDead) return;
            float n = Normalized;
            Max = Math.Max(1f, newMax);
            Current = Math.Max(1f, Max * n);
        }

        public void Reset() => Current = Max;

        /// <summary>Daño crítico en puntos débiles (cabeza / óptica).</summary>
        public const float CriticalMultiplier = 2f;

        /// <summary>Vida base según el tipo de robot (por su nombre en la escena).</summary>
        public static float BaseHealthFor(string robotName)
        {
            string n = (robotName ?? "").ToLowerInvariant();
            if (n.Contains("elite") || n.Contains("élite")) return 120f;
            if (n.Contains("guard")) return 90f;
            if (n.Contains("investig") || n.Contains("research")) return 50f;
            if (n.Contains("camara") || n.Contains("cámara")) return 25f;
            return 60f;
        }
    }
}

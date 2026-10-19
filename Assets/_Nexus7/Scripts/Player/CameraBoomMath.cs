namespace Nexus7.Player
{
    /// <summary>Cálculos de la cámara en tercera persona (lógica pura, probada con Mono).</summary>
    public static class CameraBoomMath
    {
        /// <summary>Por debajo de esta distancia (m) se pasa temporalmente a primera persona.</summary>
        public const float MinThirdPersonDistance = 0.6f;

        /// <summary>Distancia final del brazo: si choca con algo, se acerca hasta el obstáculo menos el radio.</summary>
        public static float ResolveDistance(float desired, float hitDistance, bool hit, float radius)
        {
            if (!hit) return desired;
            float d = hitDistance - radius;
            if (d < 0f) d = 0f;
            return d < desired ? d : desired;
        }

        public static bool ShouldFallbackToFirstPerson(float distance) => distance < MinThirdPersonDistance;
    }
}

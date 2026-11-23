namespace Nexus7.Core
{
    /// <summary>
    /// Multiplicadores que el director adaptativo (Nexus7.Director.GameDirector) aplica a los sistemas de siempre.
    /// Valen 1 si no hay director (menú, pruebas, escenas antiguas), así que no cambian nada por sí solos.
    /// Los leen: EnemyPatrol (velocidad y espera en waypoints), EnemyVision (detección) y PlayerHealth (daño de robots).
    /// </summary>
    public static class AdaptiveModifiers
    {
        public static float EnemySpeed { get; private set; } = 1f;
        public static float Detection { get; private set; } = 1f;
        public static float EnemyDamage { get; private set; } = 1f;
        public static float PatrolWait { get; private set; } = 1f;

        public static void Set(float enemySpeed, float detection, float enemyDamage, float patrolWait)
        {
            EnemySpeed = Clamp(enemySpeed, 0.5f, 1.5f);
            Detection = Clamp(detection, 0.5f, 1.6f);
            EnemyDamage = Clamp(enemyDamage, 0.5f, 1.6f);
            PatrolWait = Clamp(patrolWait, 0.4f, 2f);
        }

        public static void Reset() => EnemySpeed = Detection = EnemyDamage = PatrolWait = 1f;

        private static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Reset();
    }
}

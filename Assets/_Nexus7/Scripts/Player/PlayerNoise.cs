namespace Nexus7.Player
{
    /// <summary>Radio del ruido de los pasos (m): quieto 0 · agachado 1 (casi silencio) · caminar 6 · correr 12.</summary>
    public static class PlayerNoise
    {
        public const float Walk = 6f, Run = 12f, Crouch = 1f;
        /// <summary>Jadeo al agotar la estamina.</summary>
        public const float Panting = 3f;

        public static float StepRadius(bool crouching, bool running, bool idle,
                                       float walk = Walk, float run = Run, float crouch = Crouch)
        {
            if (idle) return 0f;
            if (crouching) return crouch;
            return running ? run : walk;
        }
    }
}

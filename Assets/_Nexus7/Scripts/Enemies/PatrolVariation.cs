namespace Nexus7.Enemies
{
    /// <summary>
    /// Cómo reanuda la patrulla un robot tras un reinicio (lo decide el director, ver Nexus7.Director.SpawnVariationModel):
    /// en qué waypoint aparece, en qué sentido recorre la ruta y pequeñas variaciones de ritmo.
    /// </summary>
    public struct PatrolVariation
    {
        public int StartIndex;
        /// <summary>+1 o −1: sentido de la ruta (también en las rutas en bucle).</summary>
        public int Direction;
        /// <summary>Multiplica la espera en cada waypoint (0,7–1,3).</summary>
        public float WaitMultiplier;
        /// <summary>Multiplica la velocidad (0,92–1,08): dos intentos nunca van perfectamente sincronizados.</summary>
        public float SpeedMultiplier;
    }
}

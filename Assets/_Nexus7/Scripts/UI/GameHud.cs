namespace Nexus7.UI
{
    /// <summary>¿Se oculta el HUD de juego (alerta, cuenta atrás)? Durante las cinemáticas y desde que empieza un final.</summary>
    public static class GameHud
    {
        public static bool ShouldHide(bool cinematicPlaying, bool endingStarted) => cinematicPlaying || endingStarted;

        public static bool Hidden =>
            ShouldHide(Nexus7.Cinematics.CinematicDirector.IsPlaying, Nexus7.Gameplay.EndingManager.EndingStarted);
    }
}

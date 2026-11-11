using System;

namespace Nexus7.UI
{
    /// <summary>Reglas del menú principal (lógica pura, se prueba fuera de Unity).</summary>
    public static class MenuState
    {
        public static bool ContinueEnabled(bool hasSave) => hasSave;

        /// <summary>Nueva partida pide confirmación si va a sobrescribir una partida guardada.</summary>
        public static bool NeedsConfirm(bool hasSave) => hasSave;

        /// <summary>[Intro], ←/→ y LB/RB del menú solo funcionan sin paneles abiertos encima.</summary>
        public static bool AcceptsMainInput(bool settingsOpen, bool confirmOpen) => !settingsOpen && !confirmOpen;

        public static int ToPercent(float v) => (int)Math.Round(Math.Max(0f, Math.Min(1f, v)) * 100f, MidpointRounding.AwayFromZero);
        public static float FromPercent(float p) => Math.Max(0f, Math.Min(100f, p)) / 100f;

        public static string FullscreenLabel(bool on) => on ? "PANTALLA COMPLETA: SÍ" : "PANTALLA COMPLETA: NO";
    }
}

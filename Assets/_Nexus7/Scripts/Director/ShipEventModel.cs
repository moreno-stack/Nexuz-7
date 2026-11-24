using System;
using System.Collections.Generic;

namespace Nexus7.Director
{
    public enum ShipEvent { None, Blackout, SteamLeak, SecuritySweep }

    /// <summary>
    /// Eventos de nave (puro, probado). En cada intento el director elige uno según cómo vas:
    ///   · vas mal (tensión &lt; 0,45 o caídas recientes ≥ 1): uno que AYUDA, APAGÓN (zona en sombra) o FUGA DE VAPOR
    ///     (tapa la vista de los robots);
    ///   · vas sobrado (tensión ≥ 0,65): BARRIDO DE SEGURIDAD (los robots cercanos van a revisar la zona);
    ///   · en medio: la mitad de las veces nada, si no cualquiera.
    /// Nunca repite el evento ni el sitio del intento anterior si hay alternativa.
    /// El sitio se elige «en tu camino»: a 6–25 m, mejor por delante.
    /// </summary>
    public static class ShipEventModel
    {
        public const float HelpBelow = 0.45f, HurtFrom = 0.65f;
        public const float BlackoutRadius = 7f, BlackoutLightFactor = 0.2f;
        public const int SteamClouds = 2;
        public const float SweepDelay = 15f, SweepRepeat = 45f, SweepNoiseRadius = 25f;
        public const float MinDistance = 6f, MaxDistance = 25f, AheadAngle = 60f;

        /// <param name="roll">Aleatorio en [0, 1).</param>
        public static ShipEvent Choose(float tension, float recentDeaths, ShipEvent previous, double roll)
        {
            if (tension < HelpBelow || recentDeaths >= 1f)
            {
                var help = roll < 0.5 ? ShipEvent.Blackout : ShipEvent.SteamLeak;
                if (help == previous) help = help == ShipEvent.Blackout ? ShipEvent.SteamLeak : ShipEvent.Blackout;
                return help;
            }
            if (tension >= HurtFrom) return ShipEvent.SecuritySweep;
            if (roll < 0.5) return ShipEvent.None;
            var any = roll < 0.67 ? ShipEvent.Blackout : roll < 0.84 ? ShipEvent.SteamLeak : ShipEvent.SecuritySweep;
            if (any == previous) any = any == ShipEvent.SecuritySweep ? ShipEvent.Blackout : any + 1;
            return any;
        }

        /// <summary>Índice del sitio (o −1). Por delante +30, el sitio anterior −50, algo de azar.</summary>
        public static int PickSpot(IList<float> xs, IList<float> zs, float px, float pz, float fx, float fz, int previous, Random rng)
        {
            int best = -1;
            float bestScore = float.MinValue;
            double fl = Math.Sqrt(fx * fx + fz * fz);
            for (int i = 0; i < xs.Count; i++)
            {
                float dx = xs[i] - px, dz = zs[i] - pz;
                double d = Math.Sqrt(dx * dx + dz * dz);
                if (d < MinDistance || d > MaxDistance) continue;
                float s = (float)rng.NextDouble() * 20f;
                if (fl > 1e-4 && (dx * fx + dz * fz) / (d * fl) >= Math.Cos(AheadAngle * Math.PI / 180.0)) s += 30f;
                if (i == previous) s -= 50f;
                if (s > bestScore) { bestScore = s; best = i; }
            }
            return best;
        }

        public static string Name(ShipEvent e) =>
            e == ShipEvent.Blackout ? "Apagón" : e == ShipEvent.SteamLeak ? "Fuga de vapor" : e == ShipEvent.SecuritySweep ? "Barrido de seguridad" : "Ninguno";

        public static bool Helps(ShipEvent e) => e == ShipEvent.Blackout || e == ShipEvent.SteamLeak;
    }
}

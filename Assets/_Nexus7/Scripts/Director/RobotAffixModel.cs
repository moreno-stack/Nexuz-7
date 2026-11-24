using System;
using System.Collections.Generic;

namespace Nexus7.Director
{
    /// <summary>Rasgo de un robot para un intento.</summary>
    public enum RobotAffix { None, Armored, Swift, Hawkeye }

    /// <summary>
    /// Variantes de robot (puro, probado). En cada intento el director marca a algunos robots con un rasgo:
    ///   · BLINDADO (dorado): vida ×1,4 · RÁPIDO (verde): velocidad ×1,2 · OJO DE ÁGUILA (magenta): detecta ×1,3 y apunta mejor (×0,8).
    /// Cuántos: 0 con tensión &lt; 0,4; 1 hasta 0,6; 2 hasta 0,8; 3 desde 0,8; nunca más de la mitad de los robots (redondeando arriba).
    /// Anti-repetición: prefiere robots que no tenían rasgo en el intento anterior y, si repite robot, le cambia el rasgo.
    /// </summary>
    public static class RobotAffixModel
    {
        public const float ArmoredHealth = 1.4f, SwiftSpeed = 1.2f, HawkeyeDetection = 1.3f, HawkeyeSpread = 0.8f;

        public static int CountFor(float tension, int robots)
        {
            if (robots <= 0) return 0;
            int n = tension >= 0.8f ? 3 : tension >= 0.6f ? 2 : tension >= 0.4f ? 1 : 0;
            return Math.Min(n, (robots + 1) / 2);
        }

        public static float Health(RobotAffix a) => a == RobotAffix.Armored ? ArmoredHealth : 1f;
        public static float Speed(RobotAffix a) => a == RobotAffix.Swift ? SwiftSpeed : 1f;
        public static float Detection(RobotAffix a) => a == RobotAffix.Hawkeye ? HawkeyeDetection : 1f;
        public static float Spread(RobotAffix a) => a == RobotAffix.Hawkeye ? HawkeyeSpread : 1f;

        public static string Name(RobotAffix a) =>
            a == RobotAffix.Armored ? "Blindado" : a == RobotAffix.Swift ? "Rápido" : a == RobotAffix.Hawkeye ? "Ojo de águila" : "—";

        /// <summary>Reparte 'count' rasgos entre 'robots' (ids). 'previous' = reparto del intento anterior (puede ser null).</summary>
        public static Dictionary<string, RobotAffix> Assign(IList<string> robots, int count, int seed,
                                                            IDictionary<string, RobotAffix> previous)
        {
            var result = new Dictionary<string, RobotAffix>();
            if (robots == null || count <= 0) return result;
            var rng = new Random(seed);
            var order = new List<KeyValuePair<double, string>>();
            foreach (var id in robots)
            {
                bool had = previous != null && previous.TryGetValue(id, out var p) && p != RobotAffix.None;
                order.Add(new KeyValuePair<double, string>((had ? 1.0 : 0.0) + rng.NextDouble() * 0.9, id));   // los que no tenían, primero
            }
            order.Sort((a, b) => a.Key.CompareTo(b.Key));
            var kinds = new[] { RobotAffix.Armored, RobotAffix.Swift, RobotAffix.Hawkeye };
            int start = rng.Next(kinds.Length);
            for (int i = 0; i < count && i < order.Count; i++)
            {
                string id = order[i].Value;
                var affix = kinds[(start + i) % kinds.Length];
                if (previous != null && previous.TryGetValue(id, out var before) && before == affix)
                    affix = kinds[(Array.IndexOf(kinds, affix) + 1) % kinds.Length];   // mismo robot: otro rasgo
                result[id] = affix;
            }
            return result;
        }
    }
}

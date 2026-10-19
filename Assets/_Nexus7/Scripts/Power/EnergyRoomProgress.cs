using System.Collections.Generic;

namespace Nexus7.Power
{
    /// <summary>Zonas de la Sala de Energía (Fase 3) según la energía necesaria para llegar.</summary>
    public enum EnergyArea { South, Central, EastWing, WestWing, Reactor }

    /// <summary>
    /// Modelo simplificado de la progresión de la Fase 3, para comprobar que ningún reparto del botín bloquea la sala:
    /// zócalo A (sur, batería) · interruptor (ala este) · zócalo B (ala oeste, batería) · reactor (3 bloques + módulo de control).
    /// El módulo del reactor es un objeto propio: los hackeos y las recetas gastan circuitos, nunca el módulo.
    /// </summary>
    public static class EnergyRoomProgress
    {
        /// <summary>Sur y sala central: siempre. Alas: 1 bloque (puerta S1; el ala oeste por las pasarelas). Reactor: 2 bloques.</summary>
        public static bool AreaOpen(EnergyArea a, int power)
        {
            switch (a)
            {
                case EnergyArea.EastWing:
                case EnergyArea.WestWing: return power >= 1;
                case EnergyArea.Reactor:  return power >= 2;
                default:                  return true;
            }
        }

        /// <summary>¿Se llega a los 4 bloques empezando con estas baterías/módulos y este botín ("battery" / "module"; "circuit" no cuenta)?</summary>
        public static bool Solvable(int startBatteries, int startModules, IList<(EnergyArea area, string item)> loot)
        {
            int batteries = startBatteries, modules = startModules;
            var taken = new bool[loot.Count];
            bool socketA = false, breaker = false, socketB = false, reactor = false;
            bool changed = true;
            while (changed)
            {
                changed = false;
                int power = (socketA ? 1 : 0) + (breaker ? 1 : 0) + (socketB ? 1 : 0) + (reactor ? 1 : 0);
                for (int i = 0; i < loot.Count; i++)
                {
                    if (taken[i] || !AreaOpen(loot[i].area, power)) continue;
                    taken[i] = true; changed = true;
                    if (loot[i].item == "battery") batteries++;
                    else if (loot[i].item == "module") modules++;
                }
                if (!socketA && batteries > 0) { socketA = true; batteries--; changed = true; }
                else if (!breaker && AreaOpen(EnergyArea.EastWing, power)) { breaker = true; changed = true; }
                else if (!socketB && batteries > 0 && AreaOpen(EnergyArea.WestWing, power)) { socketB = true; batteries--; changed = true; }
                else if (!reactor && power >= 3 && modules > 0) { reactor = true; modules--; changed = true; }
            }
            return socketA && breaker && socketB && reactor;
        }
    }
}

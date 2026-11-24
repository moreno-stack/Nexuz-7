using System;
using System.Collections.Generic;

namespace Nexus7.Director
{
    /// <summary>
    /// Mapa de calor de la IA (puro, probado en HeatmapTest): dónde te detectan, dónde caes y dónde acampas, en celdas de
    /// 4 × 4 m. En cada intento el calor se enfría (×0,6) para que pese más lo reciente.
    ///   Pesos: detección +1 · caída +3 · acampada +0,5 por muestra (cada 5 s con acampada ≥ 70 %).
    /// Al reaparecer, el director manda una unidad a la celda más caliente que:
    ///   · tenga al menos 2 de calor (un solo susto no es una ruta);
    ///   · esté a 10 m o más de tu punto de reaparición (no se te echa encima nada más volver);
    ///   · no sea la del intento anterior, si hay otra que valga (anti-repetición).
    /// </summary>
    public sealed class HeatmapModel
    {
        public const float CellSize = 4f;
        public const float DetectionHeat = 1f, DeathHeat = 3f, CampHeat = 0.5f, CampThreshold = 0.7f;
        public const float DecayPerAttempt = 0.6f, MinHeat = 2f, SafeDistance = 10f, Forget = 0.05f;

        private readonly Dictionary<long, float> heat = new Dictionary<long, float>();

        public int Cells => heat.Count;
        public long LastPick { get; private set; } = long.MinValue;

        public static long Key(float x, float z) => Key((int)Math.Floor(x / CellSize), (int)Math.Floor(z / CellSize));
        private static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
        private static void Center(long key, out float x, out float z)
        {
            int cx = (int)(key >> 32), cz = (int)(uint)key;
            x = (cx + 0.5f) * CellSize;
            z = (cz + 0.5f) * CellSize;
        }

        public void Add(float x, float z, float amount)
        {
            if (amount <= 0f) return;
            long k = Key(x, z);
            heat.TryGetValue(k, out float h);
            heat[k] = h + amount;
        }

        public float HeatAt(float x, float z) => heat.TryGetValue(Key(x, z), out float h) ? h : 0f;

        /// <summary>Nuevo intento: todo se enfría y lo que queda casi a 0 se olvida.</summary>
        public void Decay()
        {
            var keys = new List<long>(heat.Keys);
            foreach (var k in keys)
            {
                float h = heat[k] * DecayPerAttempt;
                if (h < Forget) heat.Remove(k); else heat[k] = h;
            }
        }

        /// <summary>
        /// La celda a vigilar en este intento (centro en x, z), o false si ninguna vale.
        /// Si la única válida es la del intento anterior, la repite (mejor vigilar algo que nada).
        /// </summary>
        public bool PickHotspot(float safeX, float safeZ, out float x, out float z, out float h)
        {
            long best = long.MinValue, fallback = long.MinValue;
            float bestHeat = 0f, fallbackHeat = 0f;
            foreach (var kv in heat)
            {
                if (kv.Value < MinHeat) continue;
                Center(kv.Key, out float cx, out float cz);
                float dx = cx - safeX, dz = cz - safeZ;
                if (dx * dx + dz * dz < SafeDistance * SafeDistance) continue;
                if (kv.Key == LastPick)
                {
                    fallback = kv.Key; fallbackHeat = kv.Value;
                    continue;
                }
                // Desempate estable por clave para que el resultado no dependa del orden del diccionario
                if (kv.Value > bestHeat || (kv.Value == bestHeat && kv.Key < best)) { best = kv.Key; bestHeat = kv.Value; }
            }
            if (best == long.MinValue) { best = fallback; bestHeat = fallbackHeat; }
            x = z = h = 0f;
            if (best == long.MinValue) return false;
            Center(best, out x, out z);
            h = bestHeat;
            LastPick = best;
            return true;
        }

        public void Clear()
        {
            heat.Clear();
            LastPick = long.MinValue;
        }
    }
}

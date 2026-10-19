using System;
using System.Collections.Generic;

namespace Nexus7.Inventory
{
    /// <summary>Resultado de una tirada: qué objeto va en qué punto.</summary>
    public struct LootPlacement
    {
        public int point;
        public ItemData item;
        public int amount;
        /// <summary>true si es un objeto imprescindible de la sala (nunca falta).</summary>
        public bool guaranteed;
    }

    /// <summary>
    /// Reparto del botín aleatorio de una sala (lógica pura, probada con Mono):
    ///   1) baraja los puntos con la semilla
    ///   2) coloca primero los objetos GARANTIZADOS (si hay más que puntos, los sobrantes comparten el primero)
    ///   3) en cada punto restante: puede quedar vacío (emptyChance) o recibir un objeto por peso con cantidad min–max
    /// </summary>
    public static class LootRoller
    {
        public static List<LootPlacement> Roll(int seed, int pointCount, float[] emptyChance, IList<ItemStack> guaranteed, IList<LootEntry> table)
        {
            var result = new List<LootPlacement>();
            if (pointCount <= 0)
            {
                // Sin puntos: los imprescindibles nunca se pierden (point = -1 → en el propio spawner).
                if (guaranteed != null)
                    foreach (var g in guaranteed)
                        if (g.item != null && g.amount > 0) result.Add(new LootPlacement { point = -1, item = g.item, amount = g.amount, guaranteed = true });
                return result;
            }
            var rng = new Random(seed);

            var order = new int[pointCount];
            for (int i = 0; i < pointCount; i++) order[i] = i;
            for (int i = pointCount - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = order[i]; order[i] = order[j]; order[j] = t; }

            int used = 0;
            if (guaranteed != null)
                foreach (var g in guaranteed)
                {
                    if (g.item == null || g.amount <= 0) continue;
                    int point = used < pointCount ? order[used] : order[0];
                    if (used < pointCount) used++;
                    result.Add(new LootPlacement { point = point, item = g.item, amount = g.amount, guaranteed = true });
                }

            float total = 0f;
            if (table != null) foreach (var e in table) if (e != null && e.item != null && e.weight > 0f) total += e.weight;

            for (int k = used; k < pointCount; k++)
            {
                int point = order[k];
                float empty = emptyChance != null && point < emptyChance.Length ? emptyChance[point] : 0f;
                if (rng.NextDouble() < empty || total <= 0f) continue;
                double pick = rng.NextDouble() * total;
                foreach (var e in table)
                {
                    if (e == null || e.item == null || e.weight <= 0f) continue;
                    pick -= e.weight;
                    if (pick > 0) continue;
                    int lo = Math.Max(1, e.minAmount), hi = Math.Max(lo, e.maxAmount);
                    result.Add(new LootPlacement { point = point, item = e.item, amount = rng.Next(lo, hi + 1) });
                    break;
                }
            }
            return result;
        }
    }
}

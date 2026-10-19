using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>Una posibilidad de botín: objeto, peso relativo y cantidad mínima/máxima.</summary>
    [Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Min(0f)] public float weight = 1f;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;

        public LootEntry() { }
        public LootEntry(ItemData item, float weight, int minAmount, int maxAmount)
        {
            this.item = item; this.weight = weight; this.minAmount = minAmount; this.maxAmount = maxAmount;
        }
    }

    /// <summary>
    /// Tabla de botín aleatorio de una sala.
    /// Crear: Assets > Create > Nexus-7 > Tabla de botín (o la crean los constructores de escena).
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus-7/Tabla de botín", fileName = "NuevaTablaBotin")]
    public class LootTable : ScriptableObject
    {
        public List<LootEntry> entries = new List<LootEntry>();
    }
}

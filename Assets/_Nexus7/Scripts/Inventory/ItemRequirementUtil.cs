using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Utilidades compartidas por todo lo que pide objetos (zócalos, consolas, terminales de hackeo, paneles de reparación).
    /// </summary>
    public static class ItemRequirementUtil
    {
        private static InventorySystem cachedPlayerInventory;

        /// <summary>Inventario del jugador (busca por la etiqueta "Player" y lo guarda en caché).</summary>
        public static InventorySystem PlayerInventory
        {
            get
            {
                if (cachedPlayerInventory == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null) cachedPlayerInventory = player.GetComponent<InventorySystem>();
                }
                return cachedPlayerInventory;
            }
        }

        /// <summary>"Circuito de control 1/1, Chatarra metálica 0/2" (con lo que lleva el jugador).</summary>
        public static string Describe(IEnumerable<ItemStack> requirements, InventorySystem inventory = null)
        {
            if (inventory == null) inventory = PlayerInventory;
            return string.Join(", ", requirements.Where(r => r.IsValid).Select(r =>
                $"{r.item.DisplayName} {(inventory != null ? Mathf.Min(inventory.GetCount(r.item), r.amount) : 0)}/{r.amount}"));
        }

        /// <summary>"Circuito de control x1" (sin progreso, para avisos cortos).</summary>
        public static string DescribeCost(IEnumerable<ItemStack> requirements) =>
            string.Join(", ", requirements.Where(r => r.IsValid).Select(r => $"{r.item.DisplayName} x{r.amount}"));

        public static bool HasAny(IEnumerable<ItemStack> requirements) =>
            requirements != null && requirements.Any(r => r.IsValid);
    }
}

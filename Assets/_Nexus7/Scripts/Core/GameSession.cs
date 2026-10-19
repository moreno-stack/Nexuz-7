using System.Collections.Generic;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.Core
{
    /// <summary>
    /// Datos que sobreviven a los cambios de escena durante una partida (en memoria).
    /// Por ahora: el inventario. SceneExitTrigger lo guarda antes de cargar la siguiente escena
    /// e InventorySystem lo recupera en Awake. Así las baterías y circuitos del Almacén llegan a la Sala de Energía.
    ///
    /// Base para un futuro sistema de guardado en disco: basta con serializar ItemData.Id + cantidad.
    /// </summary>
    public static class GameSession
    {
        private static readonly List<ItemStack> savedInventory = new List<ItemStack>();

        public static bool HasSavedInventory { get; private set; }
        public static IReadOnlyList<ItemStack> SavedInventory => savedInventory;

        public static void SaveInventory(InventorySystem inventory)
        {
            savedInventory.Clear();
            savedInventory.AddRange(inventory.GetSummary());
            HasSavedInventory = true;
        }

        /// <summary>El jugador ya se ha puesto el traje espacial (Fase 1).</summary>
        public static bool HasSuit { get; set; }
        /// <summary>Semilla de la partida: decide los objetos aleatorios de cada sala.</summary>
        public static int RunSeed { get; private set; } = System.Environment.TickCount;

        /// <summary>Nueva partida: sin traje y con objetos aleatorios distintos.</summary>
        public static void NewRun()
        {
            HasSuit = false;
            RunSeed = unchecked(System.Environment.TickCount * 7919 + RunSeed);
        }

        public static void Clear()
        {
            HasSuit = false;
            savedInventory.Clear();
            HasSavedInventory = false;
        }

        // Si en Project Settings desactivas "Domain Reload" al entrar en Play, los estáticos no se reinician solos.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}

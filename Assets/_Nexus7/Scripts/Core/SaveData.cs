using System;
using System.Collections.Generic;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.Core
{
    /// <summary>Partida guardada (se escribe como JSON con JsonUtility). Se guarda al entrar en cada fase.</summary>
    [Serializable]
    public class SaveData
    {
        public int version;
        /// <summary>Escena de la fase (p. ej. "SalaEnergia").</summary>
        public string scene;
        public string phaseTitle;
        public long savedAtUtcTicks;
        /// <summary>Inventario con el que se entró en la fase.</summary>
        public List<SavedItem> items = new List<SavedItem>();
        public bool hasSuit;
        public int runSeed;
        public string hotbarId;
        public int difficulty;
        public RunStatsSnapshot stats = new RunStatsSnapshot();
    }

    [Serializable]
    public class SavedItem
    {
        public string id;
        public int amount;
    }

    /// <summary>Estadísticas de la partida (RunStats) para el resultado final.</summary>
    [Serializable]
    public class RunStatsSnapshot
    {
        public float elapsed;
        public int itemsCollected, itemsCrafted, devicesUsed, hacksSucceeded, empTargetsHit, decoysTriggered, deaths;
        public float damageTaken;
        public int[] deviceKinds = new int[0];
        public string[] sectors = new string[0];
    }

    /// <summary>Lógica pura de la partida guardada: textos, compatibilidad, lectura e inventario (se prueba fuera de Unity).</summary>
    public static class SaveSlot
    {
        public const int Version = 1;

        public static string RelativeTime(TimeSpan ago)
        {
            if (ago.TotalMinutes < 1) return "hace un momento";
            if (ago.TotalHours < 1) return $"hace {(int)ago.TotalMinutes} min";
            if (ago.TotalDays < 1) return $"hace {(int)ago.TotalHours} h";
            int days = (int)ago.TotalDays;
            return days == 1 ? "hace 1 día" : $"hace {days} días";
        }

        /// <summary>Texto bajo «Continuar»: "Fase 3 · Sala de energía — hace 2 h".</summary>
        public static string Describe(SaveData d, DateTime nowUtc, bool sceneAvailable)
        {
            if (d == null) return "Sin partida guardada";
            if (!sceneAvailable) return "Partida no disponible";
            var ago = nowUtc - new DateTime(d.savedAtUtcTicks, DateTimeKind.Utc);
            return GameFlow.PhaseTitle(d.scene) + " — " + RelativeTime(ago);
        }

        public static bool IsCompatible(SaveData d) => d != null && d.version == Version && !string.IsNullOrEmpty(d.scene);

        /// <summary>Lee el JSON. Vacío, corrupto o de otra versión → null (nunca lanza).</summary>
        public static SaveData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                return IsCompatible(d) ? d : null;
            }
            catch (Exception) { return null; }
        }

        public static List<SavedItem> ToSaved(IEnumerable<ItemStack> stacks)
        {
            var list = new List<SavedItem>();
            if (stacks == null) return list;
            foreach (var s in stacks)
                if (s.item != null && s.amount > 0) list.Add(new SavedItem { id = s.item.Id, amount = s.amount });
            return list;
        }

        /// <summary>De Id + cantidad a inventario. Los Id que ya no existen se omiten y se cuentan en 'unknown'.</summary>
        public static List<ItemStack> FromSaved(List<SavedItem> saved, Func<string, ItemData> find, out int unknown)
        {
            unknown = 0;
            var list = new List<ItemStack>();
            if (saved == null) return list;
            foreach (var s in saved)
            {
                if (s == null || s.amount <= 0) continue;
                var item = find != null ? find(s.id) : null;
                if (item == null) { unknown++; continue; }
                list.Add(new ItemStack(item, s.amount));
            }
            return list;
        }
    }
}

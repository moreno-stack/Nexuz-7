using System;
using System.IO;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.Core
{
    /// <summary>
    /// Partida guardada en disco (un único hueco): Application.persistentDataPath/nexus7_partida.json.
    ///   · Se guarda automáticamente al entrar en cada fase (SceneLoader), con el inventario con que se entra.
    ///   · «Continuar» restaura la sesión y carga esa fase desde el principio.
    ///   · Se borra al terminar la partida (victoria o derrota final).
    /// Cualquier error de lectura o escritura se registra como aviso y se trata como «sin partida».
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "nexus7_partida.json";
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static SaveData cache;
        private static bool cacheValid;

        /// <summary>La partida guardada, o null si no hay (o está dañada / es de otra versión).</summary>
        public static SaveData Peek()
        {
            if (cacheValid) return cache;
            cacheValid = true;
            cache = null;
            try { if (File.Exists(FilePath)) cache = SaveSlot.Parse(File.ReadAllText(FilePath)); }
            catch (Exception e) { Debug.LogWarning("[Nexus-7] No se pudo leer la partida guardada: " + e.Message); }
            return cache;
        }

        /// <summary>Hay una partida que se puede continuar (y su escena está en Build Settings).</summary>
        public static bool HasSave
        {
            get { var d = Peek(); return d != null && Application.CanStreamedLevelBeLoaded(d.scene); }
        }

        /// <summary>Texto para el botón Continuar.</summary>
        public static string Describe()
        {
            var d = Peek();
            return SaveSlot.Describe(d, DateTime.UtcNow, d != null && Application.CanStreamedLevelBeLoaded(d.scene));
        }

        public static void SaveCurrent(string scene, InventorySystem inventory)
        {
            var d = new SaveData
            {
                version = SaveSlot.Version,
                scene = scene,
                phaseTitle = GameFlow.PhaseTitle(scene),
                savedAtUtcTicks = DateTime.UtcNow.Ticks,
                items = SaveSlot.ToSaved(inventory != null ? inventory.GetSummary() : null),
                hasSuit = GameSession.HasSuit,
                runSeed = GameSession.RunSeed,
                hotbarId = GameSession.HotbarSelectedId,
                difficulty = (int)DifficultySettings.Current,
                stats = RunStats.Snapshot(),
            };
            try
            {
                // Se escribe en un archivo temporal y luego se reemplaza: nunca queda un guardado a medias
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                cache = d;
                cacheValid = true;
            }
            catch (Exception e) { Debug.LogWarning("[Nexus-7] No se pudo guardar la partida: " + e.Message); }
        }

        /// <summary>Restaura la sesión de la partida guardada. Devuelve la escena a cargar.</summary>
        public static bool LoadAndApply(out string scene)
        {
            scene = null;
            var d = Peek();
            if (d == null || !Application.CanStreamedLevelBeLoaded(d.scene)) return false;

            GameFlow.ResetRunState();
            DifficultySettings.Current = (Difficulty)Mathf.Clamp(d.difficulty, 0, 2);
            var catalog = ItemCatalog.Load();
            if (catalog == null && d.items != null && d.items.Count > 0)
                Debug.LogWarning("[Nexus-7] Falta el catálogo de objetos (Nexus-7 → Actualizar catálogo de objetos): se continúa sin inventario.");
            var items = SaveSlot.FromSaved(d.items, id => catalog != null ? catalog.Find(id) : null, out int unknown);
            if (unknown > 0 && catalog != null) Debug.LogWarning($"[Nexus-7] {unknown} objeto(s) de la partida guardada ya no existen y se han omitido.");
            GameSession.Restore(items, d.hasSuit, d.runSeed, d.hotbarId);
            RunStats.Restore(d.stats);
            scene = d.scene;
            return true;
        }

        public static void Delete()
        {
            cache = null;
            cacheValid = true;
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning("[Nexus-7] No se pudo borrar la partida guardada: " + e.Message); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { cache = null; cacheValid = false; }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Lista de todos los objetos del juego, para guardar y cargar el inventario por Id (SaveSystem).
    /// La genera el editor en Assets/_Nexus7/Resources/Nexus7_CatalogoObjetos.asset
    /// (Nexus-7 → Actualizar catálogo de objetos, y al construir el juego).
    /// </summary>
    public class ItemCatalog : ScriptableObject
    {
        public const string ResourceName = "Nexus7_CatalogoObjetos";

        [SerializeField] private List<ItemData> items = new List<ItemData>();
        private Dictionary<string, ItemData> byId;

        public int Count => items.Count;

        public ItemData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId == null)
            {
                byId = new Dictionary<string, ItemData>();
                foreach (var item in items) if (item != null && !byId.ContainsKey(item.Id)) byId.Add(item.Id, item);
            }
            byId.TryGetValue(id, out var found);
            return found;
        }

        public void SetItems(IEnumerable<ItemData> all)
        {
            items.Clear();
            foreach (var i in all) if (i != null) items.Add(i);
            byId = null;
        }

        private static ItemCatalog cached;
        public static ItemCatalog Load()
        {
            if (cached == null) cached = Resources.Load<ItemCatalog>(ResourceName);
            return cached;
        }
    }
}

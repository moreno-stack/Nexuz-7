using System.Linq;
using UnityEditor;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Genera Assets/_Nexus7/Resources/Nexus7_CatalogoObjetos.asset con todos los ItemData del juego
    /// (para guardar y cargar el inventario por Id). Se ejecuta al construir el juego y desde el menú.
    /// </summary>
    public static class ItemCatalogBuilder
    {
        private const string Folder = "Assets/_Nexus7/Resources";
        private const string AssetPath = Folder + "/" + ItemCatalog.ResourceName + ".asset";

        [MenuItem("Nexus-7/Actualizar catálogo de objetos", false, 3)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Nexus7", "Resources");
            var items = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Nexus7/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(i => i != null).OrderBy(i => i.Id).ToList();
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }
            catalog.SetItems(items);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nexus-7] Catálogo de objetos: {items.Count} objetos en {AssetPath}.");
        }
    }
}

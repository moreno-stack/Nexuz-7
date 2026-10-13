using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>Categorías de objetos del juego.</summary>
    public enum ItemCategory
    {
        Part,       // Piezas / chatarra (ScrapMetal)
        Battery,    // Baterías (Battery)
        Component,  // Componentes electrónicos
        Tool        // Herramientas (ToolComponent) — normalmente objetos clave
    }

    /// <summary>
    /// Definición de un TIPO de objeto (dato, no instancia). Se crea como asset:
    /// clic derecho en Project → Create → Nexus-7 → Objeto.
    /// Todas las baterías del juego apuntan al MISMO asset "Item_Bateria"; el inventario
    /// guarda referencias a estos assets + cantidades.
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus-7/Objeto", fileName = "Item_Nuevo")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Identificador único y estable (para guardado de partida). Si se deja vacío se usa el nombre del asset.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Objeto";
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private ItemCategory category = ItemCategory.Part;
        [SerializeField] private Sprite icon;
        [Tooltip("Cuántas unidades caben en un mismo espacio del inventario.")]
        [SerializeField, Min(1)] private int maxStack = 10;
        [Tooltip("Objeto de historia: no se puede tirar ni consumir por accidente.")]
        [SerializeField] private bool isKeyItem;
        [Tooltip("Color usado en notificaciones y en la lista del inventario.")]
        [SerializeField] private Color uiColor = Color.white;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public Sprite Icon => icon;
        public int MaxStack => Mathf.Max(1, maxStack);
        public bool IsKeyItem => isKeyItem;
        public Color UiColor => uiColor;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }

        /// <summary>Nombre en español de cada categoría (para la UI).</summary>
        public static string CategoryLabel(ItemCategory c)
        {
            switch (c)
            {
                case ItemCategory.Part:      return "PIEZAS";
                case ItemCategory.Battery:   return "BATERÍAS";
                case ItemCategory.Component: return "COMPONENTES";
                case ItemCategory.Tool:      return "HERRAMIENTAS";
                default:                     return c.ToString().ToUpperInvariant();
            }
        }
    }
}

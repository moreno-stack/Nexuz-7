using System.Collections.Generic;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.Crafting
{
    /// <summary>
    /// Receta de fabricación (asset): ingredientes → resultado.
    /// Crear: clic derecho en Project → Create → Nexus-7 → Receta.
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus-7/Receta", fileName = "Receta_Nueva")]
    public class CraftingRecipe : ScriptableObject
    {
        [SerializeField] private string displayName = "Receta";
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private List<ItemStack> ingredients = new List<ItemStack>();
        [SerializeField] private ItemData result;
        [SerializeField, Min(1)] private int resultAmount = 1;
        [Tooltip("Segundos que tarda en fabricarse en el banco (el jugador queda expuesto).")]
        [SerializeField, Min(0f)] private float craftTime = 2f;

        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<ItemStack> Ingredients => ingredients;
        public ItemData Result => result;
        public int ResultAmount => resultAmount;
        public float CraftTime => craftTime;
        public bool IsValid => result != null && ingredients.Count > 0;
    }
}

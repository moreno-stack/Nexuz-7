using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.Crafting
{
    /// <summary>
    /// Lógica de fabricación (sin UI): consulta qué recetas se pueden hacer y fabrica de forma ATÓMICA:
    /// consume ingredientes → añade el resultado → si el resultado no cabe, deshace todo (devuelve ingredientes).
    ///
    /// Colocar en: el GameObject raíz "Player" (junto a InventorySystem).
    /// La interfaz (CraftingUI) se abre desde un Workbench.
    /// </summary>
    [RequireComponent(typeof(InventorySystem))]
    public class CraftingSystem : MonoBehaviour
    {
        public enum CraftResult { Success, MissingIngredients, NoSpace, InvalidRecipe }

        [SerializeField] private List<CraftingRecipe> knownRecipes = new List<CraftingRecipe>();

        private InventorySystem inventory;

        public IReadOnlyList<CraftingRecipe> Recipes => knownRecipes;
        public InventorySystem Inventory => inventory != null ? inventory : (inventory = GetComponent<InventorySystem>());

        /// <summary>Se dispara tras fabricar con éxito.</summary>
        public event Action<CraftingRecipe> Crafted;

        private void Awake() => inventory = GetComponent<InventorySystem>();

        public void LearnRecipe(CraftingRecipe recipe)
        {
            if (recipe != null && !knownRecipes.Contains(recipe)) knownRecipes.Add(recipe);
        }

        public bool HasIngredients(CraftingRecipe recipe) =>
            recipe != null && recipe.IsValid && Inventory.HasAll(recipe.Ingredients);

        /// <summary>Cuántas veces podría fabricarse ahora mismo con lo que hay en el inventario.</summary>
        public int MaxCraftable(CraftingRecipe recipe)
        {
            if (recipe == null || !recipe.IsValid) return 0;
            return recipe.Ingredients.Where(i => i.IsValid)
                         .GroupBy(i => i.item)
                         .Select(g => Inventory.GetCount(g.Key) / Mathf.Max(1, g.Sum(i => i.amount)))
                         .DefaultIfEmpty(0)
                         .Min();
        }

        public CraftResult TryCraft(CraftingRecipe recipe)
        {
            if (recipe == null || !recipe.IsValid) return CraftResult.InvalidRecipe;
            if (!Inventory.TryConsume(recipe.Ingredients)) return CraftResult.MissingIngredients;

            int added = Inventory.Add(recipe.Result, recipe.ResultAmount);
            if (added < recipe.ResultAmount)
            {
                // Deshacer: quitar lo que entró y devolver los ingredientes.
                if (added > 0) Inventory.Remove(recipe.Result, added);
                foreach (var ing in recipe.Ingredients) if (ing.IsValid) Inventory.Add(ing.item, ing.amount);
                return CraftResult.NoSpace;
            }

            Crafted?.Invoke(recipe);
            return CraftResult.Success;
        }
    }
}

// Assets/Scripts/Crafting/RecipeDatabase.cs
using System.Collections.Generic;
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// Hardcoded recipe registry for Phase 4.
    /// All recipes are shapeless: match by ingredient counts regardless of slot position.
    /// FindMatch returns the first matching recipe or null.
    /// </summary>
    public static class RecipeDatabase
    {
        private static readonly List<CraftingRecipe> _recipes = new List<CraftingRecipe>();

        static RecipeDatabase()
        {
            RegisterRecipes();
        }

        private static void RegisterRecipes()
        {
            // Stick: 2x Wood → 4x Stick
            _recipes.Add(new CraftingRecipe(
                "stick",
                new ItemId[] { ItemId.Wood, ItemId.Wood, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.Stick, 4),
                shapeless: true));

            // Wood Pickaxe: 3x Wood + 2x Stick → 1x WoodPickaxe
            _recipes.Add(new CraftingRecipe(
                "wood_pickaxe",
                new ItemId[] { ItemId.Wood, ItemId.Wood, ItemId.Wood, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.WoodPickaxe, 1),
                shapeless: true));

            // Wood Axe: 2x Wood + 2x Stick → 1x WoodAxe
            _recipes.Add(new CraftingRecipe(
                "wood_axe",
                new ItemId[] { ItemId.Wood, ItemId.Wood, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.WoodAxe, 1),
                shapeless: true));

            // Wood Shovel: 1x Wood + 2x Stick → 1x WoodShovel
            _recipes.Add(new CraftingRecipe(
                "wood_shovel",
                new ItemId[] { ItemId.Wood, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.WoodShovel, 1),
                shapeless: true));

            // Stone Pickaxe: 3x Stone + 2x Stick → 1x StonePickaxe
            _recipes.Add(new CraftingRecipe(
                "stone_pickaxe",
                new ItemId[] { ItemId.Stone, ItemId.Stone, ItemId.Stone, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.StonePickaxe, 1),
                shapeless: true));

            // Bread: 3x Wheat → 1x Bread
            _recipes.Add(new CraftingRecipe(
                "bread",
                new ItemId[] { ItemId.Wheat, ItemId.Wheat, ItemId.Wheat, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.Bread, 1),
                shapeless: true));
        }

        /// <summary>
        /// Find a recipe matching the given 9-slot grid (shapeless matching).
        /// Returns null if no match found.
        /// </summary>
        public static CraftingRecipe FindMatch(ItemStack[] grid)
        {
            if (grid == null || grid.Length != 9) return null;

            // Build a count dictionary of non-empty slots in the grid
            var gridCounts = new Dictionary<ItemId, int>();
            foreach (var slot in grid)
            {
                if (!slot.IsEmpty)
                {
                    if (!gridCounts.ContainsKey(slot.ItemId)) gridCounts[slot.ItemId] = 0;
                    gridCounts[slot.ItemId] += slot.Count;
                }
            }

            foreach (var recipe in _recipes)
            {
                if (recipe.IsShapeless && MatchesShapeless(gridCounts, recipe.Ingredients))
                    return recipe;
            }
            return null;
        }

        private static bool MatchesShapeless(Dictionary<ItemId, int> gridCounts, ItemId[] recipeIngredients)
        {
            // Build required ingredient counts from recipe
            var required = new Dictionary<ItemId, int>();
            foreach (var id in recipeIngredients)
            {
                if (id == ItemId.None) continue;
                if (!required.ContainsKey(id)) required[id] = 0;
                required[id]++;
            }

            if (required.Count != gridCounts.Count) return false;
            foreach (var kv in required)
            {
                if (!gridCounts.ContainsKey(kv.Key)) return false;
                if (gridCounts[kv.Key] != kv.Value)  return false;
            }
            return true;
        }

        public static IReadOnlyList<CraftingRecipe> AllRecipes => _recipes.AsReadOnly();
    }
}

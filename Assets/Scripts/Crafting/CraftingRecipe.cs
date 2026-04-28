// Assets/Scripts/Crafting/CraftingRecipe.cs
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// Describes one crafting recipe.
    /// Ingredients is a flat 9-element array representing the 3x3 grid (row-major, left-to-right, top-to-bottom).
    /// ItemId.None means "any / empty" in a shapeless recipe.
    /// </summary>
    public class CraftingRecipe
    {
        public string    RecipeId     { get; }
        public ItemId[]  Ingredients  { get; } // length 9
        public ItemStack Result       { get; }
        public bool      IsShapeless  { get; }

        public CraftingRecipe(string recipeId, ItemId[] ingredients, ItemStack result, bool shapeless = true)
        {
            RecipeId    = recipeId;
            Ingredients = ingredients;
            Result      = result;
            IsShapeless = shapeless;
        }
    }
}

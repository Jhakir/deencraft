// Assets/Tests/EditMode/CraftingTests/RecipeDatabaseTests.cs
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft.Crafting;

namespace DeenCraft.Tests.EditMode
{
    public class RecipeDatabaseTests
    {
        private static ItemStack[] MakeGrid(params (ItemId id, int count)[] items)
        {
            var grid = new ItemStack[9];
            for (int i = 0; i < 9; i++) grid[i] = ItemStack.Empty;
            for (int i = 0; i < items.Length && i < 9; i++)
                grid[i] = new ItemStack(items[i].id, items[i].count);
            return grid;
        }

        [Test]
        public void FindMatch_Stick_ReturnsStickRecipe()
        {
            var grid = MakeGrid((ItemId.Wood, 1), (ItemId.Wood, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("stick", result.RecipeId);
            Assert.AreEqual(4, result.Result.Count);
        }

        [Test]
        public void FindMatch_WoodPickaxe_ReturnsCorrectRecipe()
        {
            var grid = MakeGrid(
                (ItemId.Wood, 1), (ItemId.Wood, 1), (ItemId.Wood, 1),
                (ItemId.Stick, 1), (ItemId.Stick, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("wood_pickaxe", result.RecipeId);
        }

        [Test]
        public void FindMatch_StonePickaxe_ReturnsCorrectRecipe()
        {
            var grid = MakeGrid(
                (ItemId.Stone, 1), (ItemId.Stone, 1), (ItemId.Stone, 1),
                (ItemId.Stick, 1), (ItemId.Stick, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("stone_pickaxe", result.RecipeId);
        }

        [Test]
        public void FindMatch_Bread_ReturnsCorrectRecipe()
        {
            var grid = MakeGrid((ItemId.Wheat, 1), (ItemId.Wheat, 1), (ItemId.Wheat, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("bread", result.RecipeId);
        }

        [Test]
        public void FindMatch_UnknownCombo_ReturnsNull()
        {
            var grid = MakeGrid((ItemId.Dirt, 1), (ItemId.Sand, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNull(result);
        }

        [Test]
        public void FindMatch_EmptyGrid_ReturnsNull()
        {
            var grid = new ItemStack[9];
            for (int i = 0; i < 9; i++) grid[i] = ItemStack.Empty;
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNull(result);
        }
    }
}

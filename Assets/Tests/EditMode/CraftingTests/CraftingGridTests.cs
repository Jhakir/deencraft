// Assets/Tests/EditMode/CraftingTests/CraftingGridTests.cs
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft.Crafting;

namespace DeenCraft.Tests.EditMode
{
    public class CraftingGridTests
    {
        [Test]
        public void TryCraft_ProducesResult_WhenRecipeMatches()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var inv = new Inventory();
            var result = grid.TryCraft(inv);

            Assert.AreEqual(ItemId.Stick, result.ItemId);
            Assert.AreEqual(4, result.Count);
        }

        [Test]
        public void TryCraft_RemovesIngredients_FromGrid()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 2));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var inv = new Inventory();
            grid.TryCraft(inv);

            // slot (0,0) had 2 Wood, slot (0,1) had 1 Wood. Recipe needs 2 Wood.
            // ConsumeIngredients iterates slots in order: first takes 1 from slot 0 (leaving 1), then takes 1 from slot 1 (leaving 0).
            Assert.AreEqual(1, grid.GetSlot(0, 0).Count);
            Assert.IsTrue(grid.GetSlot(0, 1).IsEmpty);
        }

        [Test]
        public void TryCraft_AddsResultToInventory()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var inv = new Inventory();
            grid.TryCraft(inv);

            Assert.AreEqual(4, inv.CountItem(ItemId.Stick));
        }

        [Test]
        public void TryCraft_ReturnsEmpty_WhenNoRecipeMatches()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Sand, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Gravel, 1));

            var inv = new Inventory();
            var result = grid.TryCraft(inv);
            Assert.IsTrue(result.IsEmpty);
        }

        [Test]
        public void Preview_ReturnsResult_WithoutCrafting()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var preview = grid.Preview();
            Assert.IsNotNull(preview);
            Assert.AreEqual(ItemId.Stick, preview.Value.ItemId);
            // Grid unchanged
            Assert.IsFalse(grid.GetSlot(0, 0).IsEmpty);
        }

        [Test]
        public void Clear_EmptiesAllSlots()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Stone, 5));
            grid.Clear();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    Assert.IsTrue(grid.GetSlot(r, c).IsEmpty);
        }
    }
}

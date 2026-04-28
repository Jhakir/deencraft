// Assets/Tests/EditMode/PlayerTests/InventoryTests.cs
using NUnit.Framework;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode
{
    public class InventoryTests
    {
        [Test]
        public void AddItem_FillsHotbarFirst()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 1));
            Assert.AreEqual(ItemId.Stone, inv.GetHotbarSlot(0).ItemId);
            Assert.AreEqual(ItemId.None,  inv.GetBackpackSlot(0).ItemId);
        }

        [Test]
        public void AddItem_MergesIntoExistingStack()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 30));
            inv.AddItem(new ItemStack(ItemId.Stone, 20));
            Assert.AreEqual(50, inv.GetHotbarSlot(0).Count);
        }

        [Test]
        public void AddItem_RespectsMaxStackSize()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 64));
            int overflow = inv.AddItem(new ItemStack(ItemId.Stone, 1));
            Assert.AreEqual(64, inv.GetHotbarSlot(0).Count);
            Assert.AreEqual(ItemId.Stone, inv.GetHotbarSlot(1).ItemId);
            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void AddItem_ReturnsOverflow_WhenInventoryFull()
        {
            var inv = new Inventory();
            // Fill all 36 slots with max stacks of Stone (36 * 64 = 2304 items)
            inv.AddItem(new ItemStack(ItemId.Stone, 36 * 64));
            int overflow = inv.AddItem(new ItemStack(ItemId.Stone, 5));
            Assert.AreEqual(5, overflow);
        }

        [Test]
        public void RemoveItem_ReturnsFalse_WhenNotEnoughItems()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Wood, 3));
            bool result = inv.RemoveItem(ItemId.Wood, 5);
            Assert.IsFalse(result);
            Assert.AreEqual(3, inv.CountItem(ItemId.Wood)); // unchanged
        }

        [Test]
        public void RemoveItem_RemovesCorrectCount()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Wood, 10));
            bool result = inv.RemoveItem(ItemId.Wood, 4);
            Assert.IsTrue(result);
            Assert.AreEqual(6, inv.CountItem(ItemId.Wood));
        }

        [Test]
        public void RemoveItem_ClearsSlotWhenExhausted()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Wood, 5));
            inv.RemoveItem(ItemId.Wood, 5);
            Assert.IsTrue(inv.GetHotbarSlot(0).IsEmpty);
        }

        [Test]
        public void CountItem_SumsAcrossHotbarAndBackpack()
        {
            var inv = new Inventory();
            // Fill 9 hotbar slots + 1 backpack slot with 10 items each
            for (int i = 0; i < 10; i++)
                inv.AddItem(new ItemStack(ItemId.Dirt, 64)); // fills 10 slots
            Assert.AreEqual(640, inv.CountItem(ItemId.Dirt));
        }

        [Test]
        public void IsEmpty_ReturnsTrueOnNewInventory()
        {
            var inv = new Inventory();
            Assert.IsTrue(inv.IsEmpty());
        }

        [Test]
        public void IsEmpty_ReturnsFalseAfterAddItem()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Grass, 1));
            Assert.IsFalse(inv.IsEmpty());
        }

        [Test]
        public void SetSelectedSlot_IgnoresOutOfRange()
        {
            var inv = new Inventory();
            inv.SetSelectedSlot(3);
            Assert.AreEqual(3, inv.SelectedSlot);
            inv.SetSelectedSlot(9);  // out of range
            Assert.AreEqual(3, inv.SelectedSlot); // unchanged
        }

        [Test]
        public void ActiveItem_ReflectsSelectedSlot()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 1));
            inv.AddItem(new ItemStack(ItemId.Wood,  1));
            inv.SetSelectedSlot(1);
            Assert.AreEqual(ItemId.Wood, inv.ActiveItem.ItemId);
        }
    }
}

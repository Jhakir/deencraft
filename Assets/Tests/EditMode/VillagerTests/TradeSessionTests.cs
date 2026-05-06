// Assets/Tests/EditMode/VillagerTests/TradeSessionTests.cs
// 8 EditMode tests covering TradeSession validation and security.
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft.Villager;

namespace DeenCraft.Tests.EditMode
{
    [TestFixture]
    public class TradeSessionTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────
        private static Inventory MakeInventory() => new Inventory();

        private static Inventory InventoryWith(ItemId itemId, int count)
        {
            var inv = MakeInventory();
            inv.AddItem(new ItemStack(itemId, count));
            return inv;
        }

        // Standard offer: player gives 2 Wheat, villager gives 1 Bread
        private static TradeOffer WheatForBread(int wheatCost = 2, int breadGain = 1) =>
            new TradeOffer(ItemId.Wheat, wheatCost, ItemId.Bread, breadGain);

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void CanExecute_ReturnsFalse_WhenInventoryIsNull()
        {
            var offer = WheatForBread();
            Assert.IsFalse(TradeSession.CanExecute(offer, null));
        }

        [Test]
        public void CanExecute_ReturnsFalse_WhenOfferIsNull()
        {
            var inv = MakeInventory();
            Assert.IsFalse(TradeSession.CanExecute(null, inv));
        }

        [Test]
        public void CanExecute_ReturnsFalse_WhenNotEnoughItems()
        {
            var inv   = InventoryWith(ItemId.Wheat, 1); // has 1, needs 2
            var offer = WheatForBread(wheatCost: 2);
            Assert.IsFalse(TradeSession.CanExecute(offer, inv));
        }

        [Test]
        public void CanExecute_ReturnsTrue_WhenEnoughItems()
        {
            var inv   = InventoryWith(ItemId.Wheat, 5);
            var offer = WheatForBread(wheatCost: 2);
            Assert.IsTrue(TradeSession.CanExecute(offer, inv));
        }

        [Test]
        public void Execute_ReturnsFalse_WhenInsufficientItems_InventoryUnchanged()
        {
            var inv   = InventoryWith(ItemId.Wheat, 1);
            var offer = WheatForBread(wheatCost: 5);

            bool result = TradeSession.Execute(offer, inv);

            Assert.IsFalse(result);
            Assert.AreEqual(1, inv.CountItem(ItemId.Wheat), "Wheat should be unchanged on failure");
            Assert.AreEqual(0, inv.CountItem(ItemId.Bread),  "Bread should not have been added");
        }

        [Test]
        public void Execute_RemovesRequestedItemFromInventory()
        {
            var inv   = InventoryWith(ItemId.Wheat, 10);
            var offer = WheatForBread(wheatCost: 3);

            bool result = TradeSession.Execute(offer, inv);

            Assert.IsTrue(result);
            Assert.AreEqual(7, inv.CountItem(ItemId.Wheat), "3 wheat should have been consumed");
        }

        [Test]
        public void Execute_AddsOfferedItemToInventory()
        {
            var inv   = InventoryWith(ItemId.Wheat, 10);
            var offer = WheatForBread(wheatCost: 3, breadGain: 2);

            TradeSession.Execute(offer, inv);

            Assert.AreEqual(2, inv.CountItem(ItemId.Bread), "2 bread should have been added");
        }

        [Test]
        public void Execute_ReturnsFalse_WhenCountIsZeroOrNegative()
        {
            // Security: zero-cost trades could be exploited to duplicate items.
            var inv         = InventoryWith(ItemId.Wheat, 10);
            var zeroOffer   = new TradeOffer(ItemId.Wheat, 0, ItemId.Diamond, 100);
            var negOffer    = new TradeOffer(ItemId.Wheat, -1, ItemId.Diamond, 100);

            Assert.IsFalse(TradeSession.Execute(zeroOffer, inv),
                "Zero-cost trade must be rejected");
            Assert.IsFalse(TradeSession.Execute(negOffer, inv),
                "Negative-cost trade must be rejected");
            Assert.AreEqual(0, inv.CountItem(ItemId.Diamond),
                "No diamonds should have been added");
        }
    }
}

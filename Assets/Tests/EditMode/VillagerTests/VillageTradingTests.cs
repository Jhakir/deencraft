using System;
using DeenCraft.Player;
using DeenCraft.Villager;
using NUnit.Framework;

namespace DeenCraft.Tests.EditMode
{
    public class VillageTradingTests
    {
        [Test]
        public void DefaultTrades_CanEarnCurrencyWithCollectedWood()
        {
            var profile = VillagerProfile.CreateDefault(new Random(42));
            var sale = Array.Find(profile.Trades, t => t.RequestedItem == ItemId.Wood && t.OfferedItem == ItemId.Gold);
            Assert.IsNotNull(sale, "Children must be able to earn gold from obtainable materials.");
            var inventory = new Inventory();
            inventory.AddItem(new ItemStack(ItemId.Wood, sale.RequestedCount));
            Assert.IsTrue(TradeSession.Execute(sale, inventory));
            var food = Array.Find(profile.Trades, t => t.RequestedItem == ItemId.Gold && t.OfferedItem == ItemId.Bread);
            Assert.IsNotNull(food);
            Assert.IsTrue(TradeSession.Execute(food, inventory));
            Assert.Greater(inventory.CountItem(ItemId.Bread), 0);
        }

        [Test]
        public void DefaultTrades_IncludeObtainableDiamondAndBuildingSupplies()
        {
            var offers = VillagerProfile.CreateDefault(new Random(42)).Trades;
            Assert.IsTrue(Array.Exists(offers, t => t.RequestedItem == ItemId.Gold && t.OfferedItem == ItemId.Diamond));
            Assert.IsTrue(Array.Exists(offers, t => t.RequestedItem == ItemId.Gold && t.OfferedItem == ItemId.MudBrick));
            Assert.IsTrue(Array.Exists(offers, t => t.RequestedItem == ItemId.Wheat && t.OfferedItem == ItemId.Gold));
            Assert.IsTrue(Array.Exists(offers, t => t.RequestedItem == ItemId.Wool && t.OfferedItem == ItemId.Gold));
        }

        [Test]
        public void FullInventory_TradeCanUseSlotFreedByPayment()
        {
            var inventory = new Inventory();
            for (int i = 0; i < GameConstants.HotbarSlots + GameConstants.BackpackSlots; i++)
                inventory.AddItem(new ItemStack(ItemId.Wood, GameConstants.MaxStackSize));
            var offer = new TradeOffer(ItemId.Wood, GameConstants.MaxStackSize, ItemId.Gold, 1);
            Assert.IsTrue(TradeSession.CanExecute(offer, inventory));
            Assert.IsTrue(TradeSession.Execute(offer, inventory));
            Assert.AreEqual(1, inventory.CountItem(ItemId.Gold));
        }

        [Test]
        public void FullInventory_NoRoomLeavesBothItemsUnchanged()
        {
            var inventory = new Inventory();
            int count = (GameConstants.HotbarSlots + GameConstants.BackpackSlots) * GameConstants.MaxStackSize;
            inventory.AddItem(new ItemStack(ItemId.Wood, count));
            var offer = new TradeOffer(ItemId.Wood, 1, ItemId.Gold, 1);
            Assert.IsFalse(TradeSession.CanExecute(offer, inventory));
            Assert.IsFalse(TradeSession.Execute(offer, inventory));
            Assert.AreEqual(count, inventory.CountItem(ItemId.Wood));
            Assert.AreEqual(0, inventory.CountItem(ItemId.Gold));
        }
    }
}

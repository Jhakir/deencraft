// Assets/Tests/EditMode/VillagerTests/VillagerProfileTests.cs
// 3 EditMode tests for VillagerProfile factory and data integrity.
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft.Villager;

namespace DeenCraft.Tests.EditMode
{
    [TestFixture]
    public class VillagerProfileTests
    {
        [Test]
        public void GetRandomName_ReturnsNonEmptyString()
        {
            string name = VillagerProfile.GetRandomName(new System.Random(0));
            Assert.IsFalse(string.IsNullOrEmpty(name),
                "GetRandomName must return a non-empty string");
        }

        [Test]
        public void CreateDefault_HasAtLeastOneTrade()
        {
            var profile = VillagerProfile.CreateDefault(new System.Random(0));
            Assert.IsNotNull(profile.Trades,           "Trades array must not be null");
            Assert.Greater(profile.Trades.Length, 0,   "Default profile must include at least one trade");
        }

        [Test]
        public void TradeOffer_HasPositiveCounts()
        {
            var profile = VillagerProfile.CreateDefault(new System.Random(0));
            foreach (var trade in profile.Trades)
            {
                Assert.Greater(trade.RequestedCount, 0,
                    $"RequestedCount must be > 0 (trade: {trade.RequestedItem} → {trade.OfferedItem})");
                Assert.Greater(trade.OfferedCount, 0,
                    $"OfferedCount must be > 0 (trade: {trade.RequestedItem} → {trade.OfferedItem})");
            }
        }
    }
}

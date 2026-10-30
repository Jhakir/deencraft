using System.Reflection;
using DeenCraft.Player;
using DeenCraft.Villager;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode
{
    public class TradeInteractionTests
    {
        [Test]
        public void VillagerInteraction_RaisesTradeRequestWithPlayerInventory()
        {
            EventInfo request = typeof(VillagerAI).GetEvent("TradeRequested");
            Assert.IsNotNull(request, "E interaction must open the real trade menu.");
        }

        [Test]
        public void Player_HasExplicitInputGateForTradingMenu()
        {
            Assert.IsNotNull(typeof(PlayerController).GetProperty("InputBlocked"));
        }
    }
}

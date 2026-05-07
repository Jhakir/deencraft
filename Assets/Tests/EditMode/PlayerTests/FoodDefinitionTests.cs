// Assets/Tests/EditMode/PlayerTests/FoodDefinitionTests.cs
using NUnit.Framework;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode.PlayerTests
{
    public class FoodDefinitionTests
    {
        [Test] public void Bread_Restores_5_Hunger()   => Assert.AreEqual(5f, FoodDefinition.HungerRestored(ItemId.Bread));
        [Test] public void Date_Restores_3_Hunger()    => Assert.AreEqual(3f, FoodDefinition.HungerRestored(ItemId.Date));
        [Test] public void Fig_Restores_2_Hunger()     => Assert.AreEqual(2f, FoodDefinition.HungerRestored(ItemId.Fig));
        [Test] public void Falafel_Restores_6_Hunger() => Assert.AreEqual(6f, FoodDefinition.HungerRestored(ItemId.Falafel));
        [Test] public void Fish_Restores_4_Hunger()    => Assert.AreEqual(4f, FoodDefinition.HungerRestored(ItemId.Fish));
        [Test] public void Olive_Restores_2_Hunger()   => Assert.AreEqual(2f, FoodDefinition.HungerRestored(ItemId.Olive));
        [Test] public void Apple_Restores_3_Hunger()   => Assert.AreEqual(3f, FoodDefinition.HungerRestored(ItemId.Apple));
        [Test] public void NonFood_Restores_0_Hunger() => Assert.AreEqual(0f, FoodDefinition.HungerRestored(ItemId.Stone));
        [Test] public void IsFoodItem_True_ForBread()  => Assert.IsTrue(FoodDefinition.IsFood(ItemId.Bread));
        [Test] public void IsFoodItem_False_ForStone() => Assert.IsFalse(FoodDefinition.IsFood(ItemId.Stone));
    }
}

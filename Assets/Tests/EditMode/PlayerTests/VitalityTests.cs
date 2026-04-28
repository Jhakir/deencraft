// Assets/Tests/EditMode/PlayerTests/VitalityTests.cs
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    public class VitalityTests
    {
        [Test]
        public void TakeDamage_ReducesHealth()
        {
            var v = new VitalityData();
            v.TakeDamage(5f);
            Assert.AreEqual(GameConstants.MaxHealth - 5f, v.Health, 0.001f);
        }

        [Test]
        public void TakeDamage_KillsPlayerWhenHealthReachesZero()
        {
            var v = new VitalityData();
            v.TakeDamage(GameConstants.MaxHealth + 1f);
            Assert.IsFalse(v.IsAlive);
            Assert.AreEqual(0f, v.Health, 0.001f);
        }

        [Test]
        public void Heal_IncreasesHealth_ClampedToMax()
        {
            var v = new VitalityData();
            v.TakeDamage(5f);
            v.Heal(10f);
            Assert.AreEqual(GameConstants.MaxHealth, v.Health, 0.001f);
        }

        [Test]
        public void Tick_DrainsHunger_WhenMoving()
        {
            var v = new VitalityData();
            float before = v.Hunger;
            v.Tick(1f, isMoving: true);
            // Moving doubles drain rate: 0.05 * 2 * 1 = 0.1
            Assert.AreEqual(before - GameConstants.HungerDrainRate * 2f, v.Hunger, 0.001f);
        }

        [Test]
        public void Tick_DrainsHunger_WhenNotMoving()
        {
            var v = new VitalityData();
            float before = v.Hunger;
            v.Tick(1f, isMoving: false);
            Assert.AreEqual(before - GameConstants.HungerDrainRate, v.Hunger, 0.001f);
        }

        [Test]
        public void Tick_CausesStarvationDamage_WhenHungerIsZero()
        {
            var v = new VitalityData();
            v.SetHunger(0f);
            v.Tick(2f, isMoving: false);
            // 0.5 damage/s * 2s = 1 damage
            Assert.AreEqual(GameConstants.MaxHealth - 1f, v.Health, 0.001f);
        }

        [Test]
        public void Tick_RegenHealth_WhenHungerAboveThreshold()
        {
            var v = new VitalityData();
            v.TakeDamage(5f);
            v.SetHunger(GameConstants.HungerRegenThresh); // exactly at threshold
            float healthBefore = v.Health;
            v.Tick(1f, isMoving: false);
            Assert.Greater(v.Health, healthBefore);
        }

        [Test]
        public void Eat_RestoresHunger_ClampedToMax()
        {
            var v = new VitalityData();
            v.SetHunger(18f);
            v.Eat(10f); // would be 28 but max is 20
            Assert.AreEqual(GameConstants.MaxHunger, v.Hunger, 0.001f);
        }
    }
}

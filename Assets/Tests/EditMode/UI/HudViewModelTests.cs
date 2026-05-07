using NUnit.Framework;
using DeenCraft.UI;

namespace DeenCraft.Tests.EditMode.UI
{
    public class HudViewModelTests
    {
        // ── HealthToHearts ────────────────────────────────────────────────────

        [Test]
        public void FullHealth_IsMaxHearts()
        {
            Assert.AreEqual(10, HudViewModel.HealthToHearts(20f));
        }

        [Test]
        public void ZeroHealth_IsZeroHearts()
        {
            Assert.AreEqual(0, HudViewModel.HealthToHearts(0f));
        }

        [Test]
        public void HalfHealth_Is5Hearts()
        {
            Assert.AreEqual(5, HudViewModel.HealthToHearts(10f));
        }

        [Test]
        public void NegativeHealth_ClampsToZeroHearts()
        {
            Assert.AreEqual(0, HudViewModel.HealthToHearts(-10f));
        }

        // ── HungerFraction ────────────────────────────────────────────────────

        [Test]
        public void FullHunger_ReturnsOne()
        {
            Assert.AreEqual(1f, HudViewModel.HungerFraction(20f), 0.001f);
        }

        [Test]
        public void ZeroHunger_ReturnsZero()
        {
            Assert.AreEqual(0f, HudViewModel.HungerFraction(0f), 0.001f);
        }

        [Test]
        public void HungerFraction_ClampedAboveOne()
        {
            Assert.AreEqual(1f, HudViewModel.HungerFraction(99f), 0.001f);
        }

        // ── TimeLabel ────────────────────────────────────────────────────────

        [Test]
        public void TimeLabel_Midday_ReturnsMidday()
        {
            Assert.AreEqual("Midday", HudViewModel.TimeLabel(0.5f));
        }

        [Test]
        public void TimeLabel_Midnight_ReturnsNight()
        {
            Assert.AreEqual("Night", HudViewModel.TimeLabel(0.0f));
        }

        [Test]
        public void TimeLabel_Morning_ReturnsMorning()
        {
            Assert.AreEqual("Morning", HudViewModel.TimeLabel(0.25f));
        }
    }
}

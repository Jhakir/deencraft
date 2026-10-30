using DeenCraft.Player;
using NUnit.Framework;

namespace DeenCraft.Tests.EditMode
{
    public class FlightInputTests
    {
        [Test]
        public void SingleTap_DoesNotEnableFlight()
        {
            var input = new FlightInput();
            Assert.IsFalse(input.PressJump(0f));
            Assert.IsFalse(input.IsFlying);
        }

        [Test]
        public void DoubleTap_EnablesFlight()
        {
            var input = new FlightInput();
            input.PressJump(0f);
            Assert.IsTrue(input.PressJump(0.2f));
            Assert.IsTrue(input.IsFlying);
        }

        [Test]
        public void SlowTaps_StillJumpInsteadOfFlying()
        {
            var input = new FlightInput();
            input.PressJump(1f);
            Assert.IsFalse(input.PressJump(2f));
            Assert.IsFalse(input.IsFlying);
        }

        [Test]
        public void SecondDoubleTap_DisablesFlight()
        {
            var input = new FlightInput();
            input.PressJump(0f);
            input.PressJump(0.2f);
            input.PressJump(1f);
            Assert.IsTrue(input.PressJump(1.2f));
            Assert.IsFalse(input.IsFlying);
        }

        [Test]
        public void ThirdTap_DoesNotImmediatelyUndoFlight()
        {
            var input = new FlightInput();
            input.PressJump(0f);
            input.PressJump(0.1f);
            Assert.IsFalse(input.PressJump(0.2f));
            Assert.IsTrue(input.IsFlying);
        }

        [TestCase(0.3f, true)]
        [TestCase(0.301f, false)]
        public void DoubleTap_UsesThreeHundredMillisecondThreshold(float time, bool expected)
        {
            var input = new FlightInput();
            input.PressJump(0f);
            Assert.AreEqual(expected, input.PressJump(time));
            Assert.AreEqual(expected, input.IsFlying);
        }
    }
}

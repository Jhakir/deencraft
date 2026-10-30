// Assets/Tests/EditMode/PlayerTests/HotbarInputTests.cs
using NUnit.Framework;
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode
{
    public class HotbarInputTests
    {
        // ── Number keys ──────────────────────────────────────────────────────

        [Test]
        public void ResolveSlot_NumberKey_SelectsThatSlot()
        {
            Assert.AreEqual(4, HotbarInput.ResolveSlot(current: 0, scrollDelta: 0f, numberKeySlot: 4));
        }

        [Test]
        public void ResolveSlot_NumberKey_TakesPriorityOverScroll()
        {
            Assert.AreEqual(2, HotbarInput.ResolveSlot(current: 7, scrollDelta: 1f, numberKeySlot: 2));
        }

        [Test]
        public void ResolveSlot_OutOfRangeNumberKey_IsIgnored()
        {
            Assert.AreEqual(3, HotbarInput.ResolveSlot(current: 3, scrollDelta: 0f, numberKeySlot: 9));
            Assert.AreEqual(3, HotbarInput.ResolveSlot(current: 3, scrollDelta: 0f, numberKeySlot: -2));
        }

        // ── Scroll wheel ─────────────────────────────────────────────────────

        [Test]
        public void ResolveSlot_ScrollUp_MovesToPreviousSlot()
        {
            Assert.AreEqual(2, HotbarInput.ResolveSlot(current: 3, scrollDelta: 0.1f, numberKeySlot: -1));
        }

        [Test]
        public void ResolveSlot_ScrollDown_MovesToNextSlot()
        {
            Assert.AreEqual(4, HotbarInput.ResolveSlot(current: 3, scrollDelta: -0.1f, numberKeySlot: -1));
        }

        [Test]
        public void ResolveSlot_ScrollWrapsAroundBothEnds()
        {
            // Scrolling up from the first slot wraps to the last
            Assert.AreEqual(GameConstants.HotbarSlots - 1,
                HotbarInput.ResolveSlot(current: 0, scrollDelta: 1f, numberKeySlot: -1));

            // Scrolling down from the last slot wraps to the first
            Assert.AreEqual(0,
                HotbarInput.ResolveSlot(current: GameConstants.HotbarSlots - 1,
                                        scrollDelta: -1f, numberKeySlot: -1));
        }

        [Test]
        public void ResolveSlot_NoInput_KeepsCurrentSlot()
        {
            Assert.AreEqual(5, HotbarInput.ResolveSlot(current: 5, scrollDelta: 0f, numberKeySlot: -1));
        }

        [Test]
        public void ResolveSlot_AlwaysReturnsValidSlotIndex()
        {
            // Even from a corrupt starting index, the result must be in range.
            int result = HotbarInput.ResolveSlot(current: 99, scrollDelta: -1f, numberKeySlot: -1);
            Assert.GreaterOrEqual(result, 0);
            Assert.Less(result, GameConstants.HotbarSlots);
        }
    }
}

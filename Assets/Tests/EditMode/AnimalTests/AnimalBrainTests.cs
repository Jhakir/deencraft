// Assets/Tests/EditMode/AnimalTests/AnimalBrainTests.cs
// 10 EditMode tests for AnimalBrainData state-machine logic.
// Uses a seeded System.Random for deterministic wander-target generation.
using NUnit.Framework;
using UnityEngine;
using DeenCraft;
using DeenCraft.Animals;

namespace DeenCraft.Tests.EditMode
{
    [TestFixture]
    public class AnimalBrainTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────
        private static AnimalBrainData MakeBrain(int seed = 0) =>
            new AnimalBrainData(new System.Random(seed));

        private static readonly Vector3 Origin = Vector3.zero;
        private static readonly Vector3 Near   = new Vector3(5f, 0f, 0f); // within AnimalFollowRadius (8)
        private static readonly Vector3 Far    = new Vector3(20f, 0f, 0f); // beyond AnimalFollowRadius

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void InitialState_IsIdle()
        {
            var brain = MakeBrain();
            Assert.AreEqual(AnimalState.Idle, brain.CurrentState);
        }

        [Test]
        public void TriggerFlee_ChangesStateToFlee()
        {
            var brain = MakeBrain();
            brain.TriggerFlee(new Vector3(3f, 0f, 0f));
            Assert.AreEqual(AnimalState.Flee, brain.CurrentState);
        }

        [Test]
        public void TriggerFlee_SetsFleeFromPosition()
        {
            var brain = MakeBrain();
            var from  = new Vector3(7f, 0f, 2f);
            brain.TriggerFlee(from);
            Assert.AreEqual(from, brain.FleeFromPos);
        }

        [Test]
        public void Tick_InFleeState_ReturnsToIdle_WhenTimerExpires()
        {
            // FleeTime constant is 4 s — tick 5 s to ensure expiry.
            var brain = MakeBrain();
            brain.TriggerFlee(Origin);
            brain.Tick(5f, Origin, Far, playerHasFood: false);
            Assert.AreEqual(AnimalState.Idle, brain.CurrentState);
        }

        [Test]
        public void Tick_WithPlayerFoodNearby_TransitionsToFollow()
        {
            var brain = MakeBrain();
            // Advance enough that idle timer fires to confirm Follow takes priority
            brain.Tick(0.1f, Origin, Near, playerHasFood: true);
            Assert.AreEqual(AnimalState.Follow, brain.CurrentState);
        }

        [Test]
        public void Tick_InFollow_ReturnsToIdle_WhenPlayerFarAway()
        {
            var brain = MakeBrain();
            brain.Tick(0.1f, Origin, Near, playerHasFood: true);
            Assert.AreEqual(AnimalState.Follow, brain.CurrentState);

            // Move the "player" far away and tick
            brain.Tick(0.1f, Origin, Far, playerHasFood: true);
            Assert.AreEqual(AnimalState.Idle, brain.CurrentState);
        }

        [Test]
        public void Tick_InFollow_ReturnsToIdle_WhenPlayerNoLongerHasFood()
        {
            var brain = MakeBrain();
            brain.Tick(0.1f, Origin, Near, playerHasFood: true);
            Assert.AreEqual(AnimalState.Follow, brain.CurrentState);

            brain.Tick(0.1f, Origin, Near, playerHasFood: false);
            Assert.AreEqual(AnimalState.Idle, brain.CurrentState);
        }

        [Test]
        public void ForceWanderTarget_SetsWanderState()
        {
            var brain  = MakeBrain();
            var target = new Vector3(6f, 0f, 4f);
            brain.ForceWanderTarget(target);

            Assert.AreEqual(AnimalState.Wander, brain.CurrentState);
            Assert.AreEqual(target, brain.WanderTarget);
        }

        [Test]
        public void Tick_InWander_TransitionsToIdle_WhenTargetReached()
        {
            var brain  = MakeBrain();
            var target = new Vector3(0.3f, 0f, 0f); // within ReachedDist (0.5)
            brain.ForceWanderTarget(target);

            // Tick once: current position == Origin, target is within 0.5 units
            brain.Tick(0.1f, Origin, Far, playerHasFood: false);
            Assert.AreEqual(AnimalState.Idle, brain.CurrentState);
        }

        [Test]
        public void Tick_InIdle_StartsWander_WhenTimerExpires()
        {
            var brain = MakeBrain();
            Assert.AreEqual(AnimalState.Idle, brain.CurrentState);

            // Idle timer is between 2–5 s.  Advance 6 s to guarantee expiry.
            brain.Tick(6f, Origin, Far, playerHasFood: false);
            Assert.AreEqual(AnimalState.Wander, brain.CurrentState);
        }
    }
}

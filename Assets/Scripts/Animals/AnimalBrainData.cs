// Assets/Scripts/Animals/AnimalBrainData.cs
// Pure C# — no MonoBehaviour, no UnityEngine calls beyond Vector3 (available in EditMode).
// Inject System.Random for deterministic unit tests.
using UnityEngine;
using DeenCraft;

namespace DeenCraft.Animals
{
    /// <summary>
    /// Finite-state machine for passive animal behaviour.
    /// Idle → Wander (after idle timer) → Idle (cycle)
    /// Any state → Follow (when player is within range AND carries food)
    /// Any state → Flee (triggered externally, e.g. player attacks)
    /// Graze is an optional slow-idle variant used by Cow and Sheep controllers.
    /// </summary>
    public class AnimalBrainData
    {
        // ── Timing constants ─────────────────────────────────────────────────
        private const float IdleMinTime   =  2f;
        private const float IdleMaxTime   =  5f;
        private const float WanderTimeout = 15f; // give up on a wander target after this long
        private const float FleeTime      =  4f;
        private const float GrazeMinTime  =  5f;
        private const float GrazeMaxTime  = 10f;
        private const float WanderMinDist =  2f;
        private const float ReachedDist   =  0.5f; // "close enough" to wander target

        // ── State ─────────────────────────────────────────────────────────────
        public AnimalState CurrentState { get; private set; } = AnimalState.Idle;

        /// <summary>World-space target position when wandering.</summary>
        public Vector3 WanderTarget { get; private set; }

        /// <summary>World-space position the animal is fleeing FROM.</summary>
        public Vector3 FleeFromPos  { get; private set; }

        /// <summary>World-space target the animal is following (updated each Tick).</summary>
        public Vector3 FollowTarget { get; private set; }

        private float _stateTimer;
        private readonly System.Random _rng;

        // ── Construction ──────────────────────────────────────────────────────
        public AnimalBrainData(System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
            _stateTimer = RandomRange(IdleMinTime, IdleMaxTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advance the state machine by <paramref name="deltaTime"/> seconds.
        /// </summary>
        /// <param name="deltaTime">Elapsed seconds since last tick.</param>
        /// <param name="currentPos">Animal's current world position.</param>
        /// <param name="playerPos">Player's current world position.</param>
        /// <param name="playerHasFood">True when the player holds a food item this animal likes.</param>
        public void Tick(float deltaTime, Vector3 currentPos, Vector3 playerPos, bool playerHasFood)
        {
            _stateTimer -= deltaTime;

            switch (CurrentState)
            {
                case AnimalState.Idle:
                case AnimalState.Graze:
                    if (playerHasFood && Vector3.Distance(currentPos, playerPos) <= GameConstants.AnimalFollowRadius)
                    {
                        TransitionTo(AnimalState.Follow);
                        FollowTarget = playerPos;
                    }
                    else if (_stateTimer <= 0f)
                    {
                        StartWander(currentPos);
                    }
                    break;

                case AnimalState.Wander:
                    if (playerHasFood && Vector3.Distance(currentPos, playerPos) <= GameConstants.AnimalFollowRadius)
                    {
                        TransitionTo(AnimalState.Follow);
                        FollowTarget = playerPos;
                    }
                    else if (Vector3.Distance(currentPos, WanderTarget) <= ReachedDist || _stateTimer <= 0f)
                    {
                        TransitionTo(AnimalState.Idle);
                    }
                    break;

                case AnimalState.Flee:
                    if (_stateTimer <= 0f)
                        TransitionTo(AnimalState.Idle);
                    break;

                case AnimalState.Follow:
                    FollowTarget = playerPos; // keep target updated
                    if (!playerHasFood ||
                        Vector3.Distance(currentPos, playerPos) > GameConstants.AnimalFollowRadius + 2f)
                    {
                        TransitionTo(AnimalState.Idle);
                    }
                    break;
            }
        }

        /// <summary>
        /// Force the animal to flee away from <paramref name="fleeFromPosition"/>.
        /// Overrides any current state.
        /// </summary>
        public void TriggerFlee(Vector3 fleeFromPosition)
        {
            FleeFromPos  = fleeFromPosition;
            CurrentState = AnimalState.Flee;
            _stateTimer  = FleeTime;
        }

        /// <summary>
        /// Directly set a wander target (used by tests and debug tools).
        /// </summary>
        public void ForceWanderTarget(Vector3 target)
        {
            WanderTarget = target;
            CurrentState = AnimalState.Wander;
            _stateTimer  = WanderTimeout;
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void StartWander(Vector3 currentPos)
        {
            float angle  = (float)(_rng.NextDouble() * System.Math.PI * 2.0);
            float dist   = WanderMinDist + (float)(_rng.NextDouble() *
                           (GameConstants.AnimalWanderRadius - WanderMinDist));
            WanderTarget = currentPos + new Vector3(
                (float)System.Math.Cos(angle) * dist,
                0f,
                (float)System.Math.Sin(angle) * dist);
            CurrentState = AnimalState.Wander;
            _stateTimer  = WanderTimeout;
        }

        private void TransitionTo(AnimalState newState)
        {
            CurrentState = newState;
            switch (newState)
            {
                case AnimalState.Idle:  _stateTimer = RandomRange(IdleMinTime, IdleMaxTime);   break;
                case AnimalState.Graze: _stateTimer = RandomRange(GrazeMinTime, GrazeMaxTime); break;
                // Wander / Flee timers are set at the call site
            }
        }

        private float RandomRange(float min, float max) =>
            min + (float)(_rng.NextDouble() * (max - min));
    }
}

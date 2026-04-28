// Assets/Scripts/Player/VitalityData.cs
using DeenCraft;

namespace DeenCraft.Player
{
    /// <summary>
    /// Pure-C# health/hunger model. No Unity dependency — fully unit-testable.
    /// Call Tick(dt) every frame to advance hunger drain and health regen.
    /// </summary>
    public class VitalityData
    {
        public float Health  { get; private set; } = GameConstants.MaxHealth;
        public float Hunger  { get; private set; } = GameConstants.MaxHunger;
        public bool  IsAlive { get; private set; } = true;

        /// <summary>
        /// Advance vitality by deltaTime seconds.
        /// isMoving: true if player moved this frame (hunger drains faster while moving).
        /// </summary>
        public void Tick(float deltaTime, bool isMoving)
        {
            if (!IsAlive) return;

            // Drain hunger
            float drainMultiplier = isMoving ? 2f : 1f;
            Hunger -= GameConstants.HungerDrainRate * drainMultiplier * deltaTime;
            if (Hunger < 0f) Hunger = 0f;

            // Starvation: take damage when hunger is 0
            if (Hunger <= 0f)
            {
                Health -= 0.5f * deltaTime;
                if (Health <= 0f)
                {
                    Health  = 0f;
                    IsAlive = false;
                }
            }
            // Regen: heal when hunger is high
            else if (Hunger >= GameConstants.HungerRegenThresh)
            {
                Health += GameConstants.HealthRegenRate * deltaTime;
                if (Health > GameConstants.MaxHealth)
                    Health = GameConstants.MaxHealth;
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            Health -= amount;
            if (Health <= 0f)
            {
                Health  = 0f;
                IsAlive = false;
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health += amount;
            if (Health > GameConstants.MaxHealth)
                Health = GameConstants.MaxHealth;
        }

        /// <summary>Eat food, restoring hunger. Returns actual hunger restored.</summary>
        public float Eat(float restoreAmount)
        {
            float before = Hunger;
            Hunger += restoreAmount;
            if (Hunger > GameConstants.MaxHunger)
                Hunger = GameConstants.MaxHunger;
            return Hunger - before;
        }

        // For testing and save/load
        public void SetHealth(float value) => Health = System.Math.Max(0, System.Math.Min(GameConstants.MaxHealth, value));
        public void SetHunger(float value) => Hunger = System.Math.Max(0, System.Math.Min(GameConstants.MaxHunger, value));
    }
}

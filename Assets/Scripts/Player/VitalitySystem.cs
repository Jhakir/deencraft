// Assets/Scripts/Player/VitalitySystem.cs
using UnityEngine;
using DeenCraft;

namespace DeenCraft.Player
{
    /// <summary>
    /// MonoBehaviour wrapper for VitalityData.
    /// Attach to the Player GameObject.
    /// </summary>
    public class VitalitySystem : MonoBehaviour
    {
        public VitalityData Data { get; } = new VitalityData();

        private PlayerController _controller;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            bool isMoving = _controller != null && _controller.IsMoving;
            Data.Tick(Time.deltaTime, isMoving);
        }

        public void TakeDamage(float amount) => Data.TakeDamage(amount);
        public void Heal(float amount)        => Data.Heal(amount);

        /// <summary>Call when player eats a food item.</summary>
        public void Eat(ItemId foodItem)
        {
            float restore = GetFoodRestoreAmount(foodItem);
            Data.Eat(restore);
        }

        private float GetFoodRestoreAmount(ItemId foodItem) => FoodDefinition.HungerRestored(foodItem);

        public float Health  => Data.Health;
        public float Hunger  => Data.Hunger;
        public bool  IsAlive => Data.IsAlive;
    }
}

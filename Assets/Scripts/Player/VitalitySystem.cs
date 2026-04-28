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

        private float GetFoodRestoreAmount(ItemId foodItem)
        {
            switch (foodItem)
            {
                case ItemId.Bread:   return 5f;
                case ItemId.Date:    return 3f;
                case ItemId.Fig:     return 2f;
                case ItemId.Falafel: return 6f;
                default:             return 0f;
            }
        }

        public float Health  => Data.Health;
        public float Hunger  => Data.Hunger;
        public bool  IsAlive => Data.IsAlive;
    }
}

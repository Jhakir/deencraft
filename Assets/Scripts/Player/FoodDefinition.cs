// Assets/Scripts/Player/FoodDefinition.cs
// Central nutrition lookup — single source of truth for all food item hunger values.
// Used by VitalitySystem and any future crafting/trade UI that displays food stats.
namespace DeenCraft.Player
{
    /// <summary>
    /// Maps food <see cref="ItemId"/> values to the hunger points they restore.
    /// All food nutrition values live here — do not hard-code them elsewhere.
    /// </summary>
    public static class FoodDefinition
    {
        /// <summary>
        /// Returns the hunger points restored when the player consumes this item.
        /// Returns 0 for non-food items.
        /// </summary>
        public static float HungerRestored(ItemId itemId)
        {
            switch (itemId)
            {
                case ItemId.Bread:   return 5f;
                case ItemId.Date:    return 3f;
                case ItemId.Fig:     return 2f;
                case ItemId.Falafel: return 6f;
                case ItemId.Fish:    return 4f;
                case ItemId.Olive:   return 2f;
                case ItemId.Apple:   return 3f;
                default:             return 0f;
            }
        }

        /// <summary>Returns true if the item is consumable food (i.e. restores any hunger).</summary>
        public static bool IsFood(ItemId itemId) => HungerRestored(itemId) > 0f;
    }
}

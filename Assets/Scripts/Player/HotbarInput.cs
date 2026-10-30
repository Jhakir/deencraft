// Assets/Scripts/Player/HotbarInput.cs
// Pure C# — no Unity dependencies, fully unit-testable in EditMode.
using DeenCraft;

namespace DeenCraft.Player
{
    /// <summary>
    /// Resolves which hotbar slot should be selected from raw input.
    /// Kept free of UnityEngine types so the selection rules can be unit tested.
    /// </summary>
    public static class HotbarInput
    {
        /// <summary>
        /// Returns the hotbar slot index to select.
        /// A valid <paramref name="numberKeySlot"/> (0-8) wins outright.
        /// Otherwise the scroll wheel steps one slot, wrapping at both ends.
        /// Scrolling up (positive delta) moves toward slot 0, matching Minecraft.
        /// The result is always a valid index.
        /// </summary>
        public static int ResolveSlot(int current, float scrollDelta, int numberKeySlot)
        {
            int slots = GameConstants.HotbarSlots;

            if (numberKeySlot >= 0 && numberKeySlot < slots)
                return numberKeySlot;

            int next = current;
            if (scrollDelta > 0f)      next = current - 1;
            else if (scrollDelta < 0f) next = current + 1;

            // Wrap into range (handles negatives and out-of-range starting values).
            next %= slots;
            if (next < 0) next += slots;
            return next;
        }
    }
}

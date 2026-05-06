// Assets/Scripts/Villager/TradeSession.cs
// Pure C# — no UnityEngine dependency.
// Security: All inputs validated; trade is atomic (space checked before any mutation).
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Villager
{
    /// <summary>
    /// Executes a barter trade between a <see cref="TradeOffer"/> and a player's
    /// <see cref="Inventory"/>.  All operations are atomic: the inventory is never
    /// left in a partially-modified state.
    ///
    /// Security checklist:
    /// - Null inputs → rejected
    /// - Zero or negative item counts → rejected (prevents zero-item exploits)
    /// - Insufficient requested items → rejected without mutation
    /// - Insufficient inventory space for offered items → rejected without mutation
    /// </summary>
    public static class TradeSession
    {
        /// <summary>
        /// Returns true when the trade can be executed without actually modifying the inventory.
        /// </summary>
        public static bool CanExecute(TradeOffer offer, Inventory inventory)
        {
            if (offer == null || inventory == null)         return false;
            if (offer.RequestedCount <= 0)                  return false; // security: reject zero/negative
            if (offer.OfferedCount   <= 0)                  return false; // security: reject zero/negative
            return inventory.CountItem(offer.RequestedItem) >= offer.RequestedCount;
        }

        /// <summary>
        /// Atomically executes the trade.
        /// Returns true and mutates <paramref name="inventory"/> on success.
        /// Returns false and leaves <paramref name="inventory"/> unchanged on failure.
        /// </summary>
        public static bool Execute(TradeOffer offer, Inventory inventory)
        {
            if (!CanExecute(offer, inventory)) return false;

            // Pre-check: ensure there is room for the offered items before removing
            // the requested items.  This prevents the player losing items without
            // receiving the trade goods.
            if (!HasRoomForItem(offer.OfferedItem, offer.OfferedCount, inventory)) return false;

            // Atomically remove then add — order matters: remove first so space
            // freed by identical items (e.g., exchanging one food for another) is
            // counted accurately by AddItem's internal merge logic.
            bool removed = inventory.RemoveItem(offer.RequestedItem, offer.RequestedCount);
            if (!removed)
            {
                // Should never reach here after CanExecute — but guard anyway.
                return false;
            }

            int overflow = inventory.AddItem(new ItemStack(offer.OfferedItem, offer.OfferedCount));
            if (overflow > 0)
            {
                // HasRoomForItem said there was space but AddItem still overflowed
                // (e.g., a race condition in multiplayer — impossible in v1 single-player
                // but defended against for correctness).  Roll back by re-adding.
                inventory.AddItem(new ItemStack(offer.RequestedItem, offer.RequestedCount));
                return false;
            }

            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the inventory can hold <paramref name="count"/> more
        /// items of type <paramref name="itemId"/> without any being lost.
        /// </summary>
        private static bool HasRoomForItem(ItemId itemId, int count, Inventory inventory)
        {
            int available = 0;

            // Count space in existing stacks of the same type
            for (int i = 0; i < GameConstants.HotbarSlots; i++)
            {
                var slot = inventory.GetHotbarSlot(i);
                if (slot.IsEmpty)
                    available += GameConstants.MaxStackSize;
                else if (slot.ItemId == itemId)
                    available += GameConstants.MaxStackSize - slot.Count;

                if (available >= count) return true;
            }

            for (int i = 0; i < GameConstants.BackpackSlots; i++)
            {
                var slot = inventory.GetBackpackSlot(i);
                if (slot.IsEmpty)
                    available += GameConstants.MaxStackSize;
                else if (slot.ItemId == itemId)
                    available += GameConstants.MaxStackSize - slot.Count;

                if (available >= count) return true;
            }

            return available >= count;
        }
    }
}

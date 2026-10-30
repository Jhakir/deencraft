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
            if (offer.RequestedItem == ItemId.None || offer.OfferedItem == ItemId.None) return false;
            return inventory.CountItem(offer.RequestedItem) >= offer.RequestedCount
                && HasRoomAfterPayment(offer, inventory);
        }

        /// <summary>
        /// Atomically executes the trade.
        /// Returns true and mutates <paramref name="inventory"/> on success.
        /// Returns false and leaves <paramref name="inventory"/> unchanged on failure.
        /// </summary>
        public static bool Execute(TradeOffer offer, Inventory inventory)
        {
            if (!CanExecute(offer, inventory)) return false;

            // Atomically remove then add — order matters: remove first so space
            // freed by identical items (e.g., exchanging one food for another) is
            // counted accurately by AddItem's internal merge logic.
            bool removed = inventory.RemoveItem(offer.RequestedItem, offer.RequestedCount);
            if (!removed)
            {
                throw new System.InvalidOperationException("Validated trade payment could not be removed.");
            }

            int overflow = inventory.AddItem(new ItemStack(offer.OfferedItem, offer.OfferedCount));
            if (overflow > 0)
            {
                throw new System.InvalidOperationException("Validated trade reward exceeded inventory capacity.");
            }

            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Simulates payment in the same slot order as Inventory.RemoveItem before counting reward space.
        /// </summary>
        private static bool HasRoomAfterPayment(TradeOffer offer, Inventory inventory)
        {
            int available = 0;
            int paymentRemaining = offer.RequestedCount;

            for (int i = 0; i < GameConstants.HotbarSlots + GameConstants.BackpackSlots; i++)
            {
                var slot = i < GameConstants.HotbarSlots
                    ? inventory.GetHotbarSlot(i)
                    : inventory.GetBackpackSlot(i - GameConstants.HotbarSlots);
                int remaining = slot.Count;
                if (slot.ItemId == offer.RequestedItem && paymentRemaining > 0)
                {
                    int removed = System.Math.Min(paymentRemaining, remaining);
                    paymentRemaining -= removed;
                    remaining -= removed;
                }
                if (slot.IsEmpty || remaining == 0)
                    available += GameConstants.MaxStackSize;
                else if (slot.ItemId == offer.OfferedItem)
                    available += GameConstants.MaxStackSize - remaining;

                if (available >= offer.OfferedCount) return true;
            }
            return false;
        }
    }
}

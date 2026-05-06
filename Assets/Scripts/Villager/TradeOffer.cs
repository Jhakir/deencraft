// Assets/Scripts/Villager/TradeOffer.cs
// Pure C# — no UnityEngine dependency.
using DeenCraft.Player;

namespace DeenCraft.Villager
{
    /// <summary>
    /// Represents a single barter offer: the villager takes <c>RequestedCount</c> of
    /// <c>RequestedItem</c> and gives <c>OfferedCount</c> of <c>OfferedItem</c>.
    ///
    /// Security: Both counts MUST be positive; zero or negative trades are rejected by
    /// <see cref="TradeSession"/> to prevent inventory exploits.
    /// </summary>
    public sealed class TradeOffer
    {
        public ItemId RequestedItem  { get; }
        public int    RequestedCount { get; }
        public ItemId OfferedItem    { get; }
        public int    OfferedCount   { get; }

        public TradeOffer(ItemId requestedItem, int requestedCount,
                          ItemId offeredItem,   int offeredCount)
        {
            RequestedItem  = requestedItem;
            RequestedCount = requestedCount;
            OfferedItem    = offeredItem;
            OfferedCount   = offeredCount;
        }
    }
}

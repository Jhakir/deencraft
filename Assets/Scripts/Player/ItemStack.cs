// Assets/Scripts/Player/ItemStack.cs
namespace DeenCraft.Player
{
    [System.Serializable]
    public struct ItemStack
    {
        public ItemId ItemId;
        public int Count;

        public bool IsEmpty => ItemId == ItemId.None || Count <= 0;

        public static readonly ItemStack Empty = new ItemStack { ItemId = ItemId.None, Count = 0 };

        public ItemStack(ItemId itemId, int count)
        {
            ItemId = itemId;
            Count  = count;
        }

        public override string ToString() => IsEmpty ? "Empty" : $"{ItemId}x{Count}";
    }
}

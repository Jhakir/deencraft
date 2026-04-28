// Assets/Scripts/Player/Inventory.cs
using DeenCraft;

namespace DeenCraft.Player
{
    public class Inventory
    {
        private const int HotbarSize  = GameConstants.HotbarSlots;   // 9
        private const int BackpackSize = GameConstants.BackpackSlots; // 27

        private readonly ItemStack[] _hotbar   = new ItemStack[HotbarSize];
        private readonly ItemStack[] _backpack = new ItemStack[BackpackSize];

        public int SelectedSlot { get; private set; } = 0;

        public ItemStack ActiveItem => _hotbar[SelectedSlot];

        public Inventory()
        {
            for (int i = 0; i < HotbarSize;  i++) _hotbar[i]   = ItemStack.Empty;
            for (int i = 0; i < BackpackSize; i++) _backpack[i] = ItemStack.Empty;
        }

        /// <summary>Returns hotbar slot contents (index 0-8).</summary>
        public ItemStack GetHotbarSlot(int index) => _hotbar[index];

        /// <summary>Returns backpack slot contents (index 0-26).</summary>
        public ItemStack GetBackpackSlot(int index) => _backpack[index];

        /// <summary>Sets selected hotbar slot (0-8).</summary>
        public void SetSelectedSlot(int index)
        {
            if (index >= 0 && index < HotbarSize)
                SelectedSlot = index;
        }

        /// <summary>
        /// Adds items to inventory. Fills hotbar first, then backpack.
        /// Merges into existing stacks before using new slots.
        /// Returns the number of items that could NOT be added (overflow).
        /// </summary>
        public int AddItem(ItemStack stack)
        {
            int remaining = stack.Count;
            remaining = FillSlots(_hotbar,   stack.ItemId, remaining);
            remaining = FillSlots(_backpack, stack.ItemId, remaining);
            return remaining;
        }

        private int FillSlots(ItemStack[] slots, ItemId itemId, int remaining)
        {
            // Pass 1: merge into existing stacks
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].ItemId == itemId && slots[i].Count < GameConstants.MaxStackSize)
                {
                    int space = GameConstants.MaxStackSize - slots[i].Count;
                    int toAdd = System.Math.Min(space, remaining);
                    slots[i] = new ItemStack(itemId, slots[i].Count + toAdd);
                    remaining -= toAdd;
                }
            }
            // Pass 2: fill empty slots
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int toAdd = System.Math.Min(GameConstants.MaxStackSize, remaining);
                    slots[i] = new ItemStack(itemId, toAdd);
                    remaining -= toAdd;
                }
            }
            return remaining;
        }

        /// <summary>
        /// Removes count items of itemId. Scans hotbar first, then backpack.
        /// Returns true if all items were removed, false if not enough items.
        /// Does NOT partially remove on failure.
        /// </summary>
        public bool RemoveItem(ItemId itemId, int count)
        {
            if (CountItem(itemId) < count) return false;

            int remaining = count;
            remaining = DrainSlots(_hotbar,   itemId, remaining);
            remaining = DrainSlots(_backpack, itemId, remaining);
            return remaining == 0;
        }

        private int DrainSlots(ItemStack[] slots, ItemId itemId, int remaining)
        {
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].ItemId == itemId)
                {
                    int take = System.Math.Min(slots[i].Count, remaining);
                    int left = slots[i].Count - take;
                    slots[i] = left > 0 ? new ItemStack(itemId, left) : ItemStack.Empty;
                    remaining -= take;
                }
            }
            return remaining;
        }

        /// <summary>Returns total count of itemId across all slots.</summary>
        public int CountItem(ItemId itemId)
        {
            int total = 0;
            foreach (var s in _hotbar)   if (s.ItemId == itemId) total += s.Count;
            foreach (var s in _backpack) if (s.ItemId == itemId) total += s.Count;
            return total;
        }

        /// <summary>Returns true if all slots are empty.</summary>
        public bool IsEmpty()
        {
            foreach (var s in _hotbar)   if (!s.IsEmpty) return false;
            foreach (var s in _backpack) if (!s.IsEmpty) return false;
            return true;
        }

        /// <summary>Sets a hotbar slot directly (used by CraftingGrid output).</summary>
        internal void SetHotbarSlot(int index, ItemStack stack) => _hotbar[index] = stack;

        /// <summary>Sets a backpack slot directly (used by CraftingGrid output).</summary>
        internal void SetBackpackSlot(int index, ItemStack stack) => _backpack[index] = stack;
    }
}

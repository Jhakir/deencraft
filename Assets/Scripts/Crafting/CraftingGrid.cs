// Assets/Scripts/Crafting/CraftingGrid.cs
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// Pure-C# 3x3 crafting grid model.
    /// Call TryCraft(inventory) to consume ingredients and return the crafted item.
    /// </summary>
    public class CraftingGrid
    {
        private const int GridSize = GameConstants.CraftingGridSize; // 3

        private readonly ItemStack[] _slots = new ItemStack[GridSize * GridSize];

        public CraftingGrid()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = ItemStack.Empty;
        }

        /// <summary>Set a grid slot (row 0-2, col 0-2).</summary>
        public void SetSlot(int row, int col, ItemStack stack)
        {
            _slots[row * GridSize + col] = stack;
        }

        /// <summary>Get a grid slot (row 0-2, col 0-2).</summary>
        public ItemStack GetSlot(int row, int col) => _slots[row * GridSize + col];

        /// <summary>Returns the result preview without consuming anything. Null if no match.</summary>
        public ItemStack? Preview()
        {
            var recipe = RecipeDatabase.FindMatch(_slots);
            return recipe?.Result;
        }

        /// <summary>
        /// If a matching recipe exists and the inventory has room for the result,
        /// consume the ingredients from the grid and add the result to inventory.
        /// Returns the crafted ItemStack, or ItemStack.Empty if crafting failed.
        /// </summary>
        public ItemStack TryCraft(Inventory inventory)
        {
            if (inventory == null) return ItemStack.Empty;

            var recipe = RecipeDatabase.FindMatch(_slots);
            if (recipe == null) return ItemStack.Empty;

            // Check inventory has room
            int overflow = SimulateAdd(inventory, recipe.Result);
            if (overflow > 0) return ItemStack.Empty;

            // Consume ingredients from grid
            ConsumeIngredients(recipe.Ingredients);

            // Add result to inventory
            inventory.AddItem(recipe.Result);
            return recipe.Result;
        }

        private int SimulateAdd(Inventory inventory, ItemStack result)
        {
            // Count available space: existing stack space + empty slots
            int existingSpace = 0;
            int emptySlots    = 0;

            for (int i = 0; i < GameConstants.HotbarSlots; i++)
            {
                var slot = inventory.GetHotbarSlot(i);
                if (slot.IsEmpty) emptySlots++;
                else if (slot.ItemId == result.ItemId)
                    existingSpace += GameConstants.MaxStackSize - slot.Count;
            }
            for (int i = 0; i < GameConstants.BackpackSlots; i++)
            {
                var slot = inventory.GetBackpackSlot(i);
                if (slot.IsEmpty) emptySlots++;
                else if (slot.ItemId == result.ItemId)
                    existingSpace += GameConstants.MaxStackSize - slot.Count;
            }
            int totalSpace = existingSpace + emptySlots * GameConstants.MaxStackSize;
            return System.Math.Max(0, result.Count - totalSpace);
        }

        private void ConsumeIngredients(ItemId[] recipeIngredients)
        {
            // Build required counts
            var required = new System.Collections.Generic.Dictionary<ItemId, int>();
            foreach (var id in recipeIngredients)
            {
                if (id == ItemId.None) continue;
                if (!required.ContainsKey(id)) required[id] = 0;
                required[id]++;
            }

            // Consume from grid slots
            foreach (var kv in required)
            {
                int remaining = kv.Value;
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    if (_slots[i].ItemId == kv.Key)
                    {
                        int take = System.Math.Min(_slots[i].Count, remaining);
                        int left = _slots[i].Count - take;
                        _slots[i] = left > 0 ? new ItemStack(kv.Key, left) : ItemStack.Empty;
                        remaining -= take;
                    }
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = ItemStack.Empty;
        }
    }
}

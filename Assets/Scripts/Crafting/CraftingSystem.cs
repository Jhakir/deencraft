// Assets/Scripts/Crafting/CraftingSystem.cs
using UnityEngine;
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// MonoBehaviour wrapper for CraftingGrid.
    /// Attach to the Player or a UI Canvas root.
    /// UI scripts call SetSlot/TryCraft/GetPreview.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public CraftingGrid Grid { get; } = new CraftingGrid();

        private Inventory _inventory;

        private void Awake()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                _inventory = player.GetComponent<InventoryHolder>()?.Inventory;
        }

        public ItemStack GetPreview() => Grid.Preview() ?? ItemStack.Empty;

        public ItemStack TryCraft()
        {
            if (_inventory == null) return ItemStack.Empty;
            return Grid.TryCraft(_inventory);
        }

        public void SetSlot(int row, int col, ItemStack stack) => Grid.SetSlot(row, col, stack);
        public ItemStack GetSlot(int row, int col)              => Grid.GetSlot(row, col);
        public void Clear()                                     => Grid.Clear();

        public void SetInventory(Inventory inventory) => _inventory = inventory;
    }
}

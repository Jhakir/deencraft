// Assets/Scripts/Player/HotbarController.cs
// Attach to the Player GameObject alongside InventoryHolder.
using UnityEngine;
using DeenCraft;

namespace DeenCraft.Player
{
    /// <summary>
    /// Drives hotbar slot selection from keyboard (1-9) and the mouse scroll wheel.
    /// Selection rules live in <see cref="HotbarInput"/> so they can be unit tested.
    /// </summary>
    [RequireComponent(typeof(InventoryHolder))]
    public sealed class HotbarController : MonoBehaviour
    {
        private InventoryHolder _inventoryHolder;
        private PlayerController _controller;

        private void Awake()
        {
            _inventoryHolder = GetComponent<InventoryHolder>();
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_inventoryHolder == null) return;
            if (_controller != null && !_controller.CanProcessGameplayInput) return;

            var inventory = _inventoryHolder.Inventory;
            int resolved  = HotbarInput.ResolveSlot(
                inventory.SelectedSlot,
                Input.mouseScrollDelta.y,
                ReadNumberKeySlot());

            if (resolved != inventory.SelectedSlot)
                inventory.SetSelectedSlot(resolved);
        }

        /// <summary>Returns 0-8 for the Alpha1-Alpha9 keys, or -1 when none is pressed.</summary>
        private static int ReadNumberKeySlot()
        {
            for (int i = 0; i < GameConstants.HotbarSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    return i;
            }
            return -1;
        }
    }
}

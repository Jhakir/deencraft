// Assets/Scripts/UI/HotbarSlotUI.cs
// Per-slot display component for the hotbar.
// Attach to each of the 9 hotbar slot GameObjects inside the HUD Canvas.
using UnityEngine;
using UnityEngine.UI;
using DeenCraft.Player;
using DeenCraft.World;

namespace DeenCraft.UI
{
    /// <summary>
    /// Displays a single hotbar slot: icon visibility and stack-count label.
    /// Wire <see cref="_icon"/> and <see cref="_countLabel"/> in the Inspector.
    /// Refreshed every frame by <see cref="HudController"/>.
    /// </summary>
    public sealed class HotbarSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text  _countLabel;

        /// <summary>Tint used for items that are not placeable blocks (tools, food).</summary>
        private static readonly Color NonBlockTint = new Color(0.85f, 0.75f, 0.45f, 1f);

        /// <summary>Updates visuals to reflect the given stack.</summary>
        public void Refresh(ItemStack stack)
        {
            bool hasItem = stack.ItemId != ItemId.None && stack.Count > 0;

            if (_icon != null)
            {
                _icon.enabled = hasItem;
                if (hasItem) _icon.color = TintFor(stack.ItemId);
            }

            if (_countLabel != null) _countLabel.text = hasItem && stack.Count > 1
                                                            ? stack.Count.ToString()
                                                            : "";
        }

        /// <summary>
        /// Blocks are tinted with their in-world colour so the held item is
        /// recognisable without dedicated item icon art.
        /// </summary>
        private static Color TintFor(ItemId itemId)
        {
            BlockType block = BlockItemMap.ItemIdToBlockType(itemId);
            if (block == BlockType.Air) return NonBlockTint;
            return ChunkMesher.GetBlockColour(block);
        }
    }
}

// Assets/Scripts/Player/BlockItemMap.cs
// Explicit bidirectional mapping between BlockType (World) and ItemId (Player).
// BlockType integer values and ItemId integer values are NOT the same — never cast
// between them directly.  Always use the methods in this class.
using DeenCraft.World;

namespace DeenCraft.Player
{
    public static class BlockItemMap
    {
        /// <summary>
        /// Returns the ItemId that drops when the given block is mined.
        /// Returns ItemId.None for blocks that are not minable (Water, Mosque, Air, etc.).
        /// </summary>
        public static ItemId BlockTypeToItemId(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.Grass:       return ItemId.Grass;
                case BlockType.Dirt:        return ItemId.Dirt;
                case BlockType.Stone:       return ItemId.Stone;
                case BlockType.Sand:        return ItemId.Sand;
                case BlockType.Snow:        return ItemId.SnowBlock;
                case BlockType.Wood:        return ItemId.Wood;
                case BlockType.Leaves:      return ItemId.Leaves;
                case BlockType.Ice:         return ItemId.IceBlock;
                case BlockType.Wheat:       return ItemId.Wheat;
                case BlockType.MudBrick:    return ItemId.MudBrick;
                case BlockType.PalmWood:    return ItemId.PalmWood;
                case BlockType.OliveLeaves: return ItemId.OliveLeaf;
                case BlockType.Flower:      return ItemId.Flower;
                case BlockType.Thatch:      return ItemId.Thatch;
                // Non-minable blocks
                case BlockType.Air:
                case BlockType.Water:
                case BlockType.Mosque:
                case BlockType.Moss:
                case BlockType.Cactus:
                case BlockType.Boat:
                default:
                    return ItemId.None;
            }
        }

        /// <summary>
        /// Returns the BlockType that gets placed in the world when the given item is used.
        /// Returns BlockType.Air for non-placeable items (tools, food, etc.).
        /// </summary>
        public static BlockType ItemIdToBlockType(ItemId itemId)
        {
            switch (itemId)
            {
                case ItemId.Grass:       return BlockType.Grass;
                case ItemId.Dirt:        return BlockType.Dirt;
                case ItemId.Stone:       return BlockType.Stone;
                case ItemId.Sand:        return BlockType.Sand;
                case ItemId.Wood:        return BlockType.Wood;
                case ItemId.Leaves:      return BlockType.Leaves;
                case ItemId.OliveLeaf:   return BlockType.OliveLeaves;
                case ItemId.PalmWood:    return BlockType.PalmWood;
                case ItemId.MudBrick:    return BlockType.MudBrick;
                case ItemId.SnowBlock:   return BlockType.Snow;
                case ItemId.IceBlock:    return BlockType.Ice;
                case ItemId.Flower:      return BlockType.Flower;
                case ItemId.Wheat:       return BlockType.Wheat;
                case ItemId.Thatch:      return BlockType.Thatch;
                // Non-placeable items (tools, food, currency, animal drops, etc.)
                default:
                    return BlockType.Air;
            }
        }

        /// <summary>Returns true if the item can be placed as a block in the world.</summary>
        public static bool IsPlaceable(ItemId itemId) => ItemIdToBlockType(itemId) != BlockType.Air;
    }
}

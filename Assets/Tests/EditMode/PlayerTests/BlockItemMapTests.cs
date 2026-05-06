// Assets/Tests/EditMode/PlayerTests/BlockItemMapTests.cs
// 6 EditMode tests verifying the explicit BlockType ↔ ItemId mapping table.
using NUnit.Framework;
using DeenCraft.World;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode
{
    [TestFixture]
    public class BlockItemMapTests
    {
        // ── BlockType → ItemId ────────────────────────────────────────────────

        [Test]
        public void BlockTypeToItemId_Grass_ReturnsGrass()
        {
            Assert.AreEqual(ItemId.Grass, BlockItemMap.BlockTypeToItemId(BlockType.Grass));
        }

        [Test]
        public void BlockTypeToItemId_Wood_ReturnsWood_NotWater()
        {
            // Wood is BlockType 7 but ItemId.Wood is 5 — NOT a direct int cast.
            ItemId result = BlockItemMap.BlockTypeToItemId(BlockType.Wood);
            Assert.AreEqual(ItemId.Wood, result,
                "BlockType.Wood must map to ItemId.Wood (not ItemId.Water)");
        }

        [Test]
        public void BlockTypeToItemId_Water_ReturnsNone()
        {
            Assert.AreEqual(ItemId.None, BlockItemMap.BlockTypeToItemId(BlockType.Water),
                "Water is not minable");
        }

        [Test]
        public void BlockTypeToItemId_Mosque_ReturnsNone()
        {
            Assert.AreEqual(ItemId.None, BlockItemMap.BlockTypeToItemId(BlockType.Mosque),
                "Mosque blocks are not droppable items");
        }

        // ── ItemId → BlockType ────────────────────────────────────────────────

        [Test]
        public void ItemIdToBlockType_Wood_ReturnsBlockWood()
        {
            // ItemId.Wood = 5, BlockType.Wood = 7 — not the same integer.
            BlockType result = BlockItemMap.ItemIdToBlockType(ItemId.Wood);
            Assert.AreEqual(BlockType.Wood, result);
        }

        [Test]
        public void ItemIdToBlockType_Diamond_ReturnsAir_NotPlaceable()
        {
            Assert.AreEqual(BlockType.Air, BlockItemMap.ItemIdToBlockType(ItemId.Diamond),
                "Currency items must not be placeable as blocks");
        }
    }
}

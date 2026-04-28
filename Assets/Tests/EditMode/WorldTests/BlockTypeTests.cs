using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    public class BlockTypeTests
    {
        [Test]
        public void Air_HasIdZero() => Assert.AreEqual(0, (int)BlockType.Air);

        [Test]
        public void Grass_HasIdOne() => Assert.AreEqual(1, (int)BlockType.Grass);

        [Test]
        public void Water_IdMatchesGameConstants() =>
            Assert.AreEqual(GameConstants.BlockWater, (byte)BlockType.Water);

        [Test]
        public void Stone_IdMatchesGameConstants() =>
            Assert.AreEqual(GameConstants.BlockStone, (byte)BlockType.Stone);

        [Test]
        public void AllBlockIds_AreSequentialFromZero()
        {
            var values = System.Enum.GetValues(typeof(BlockType));
            for (int i = 0; i < values.Length; i++)
                Assert.AreEqual(i, (int)values.GetValue(i),
                    $"BlockType value at index {i} should be {i}");
        }

        [Test]
        public void BlockTypeCount_IsThirteen() =>
            Assert.AreEqual(13, System.Enum.GetValues(typeof(BlockType)).Length);

        [Test]
        public void SeaLevel_IsPositiveAndBelowMaxHeight() =>
            Assert.IsTrue(GameConstants.SeaLevel > 0 && GameConstants.SeaLevel < GameConstants.MaxTerrainHeight);

        [Test]
        public void TerrainHeightRange_IsValid() =>
            Assert.Greater(GameConstants.MaxTerrainHeight, GameConstants.MinTerrainHeight);
    }
}

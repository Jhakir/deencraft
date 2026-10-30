using System.Reflection;
using NUnit.Framework;
using DeenCraft.World;

namespace DeenCraft.Tests.EditMode
{
    public class WideHouseTests
    {
        [Test]
        public void House_IsNineBlocksWideAndHasOneByThreeDoorway()
        {
            Assert.AreEqual(9, GameConstants.HouseWidth);
            var chunk = House();
            int doorX = GameConstants.HouseAnchorX + GameConstants.HouseWidth / 2;
            for (int y = 71; y <= 73; y++)
            {
                Assert.AreEqual(BlockType.Air, chunk.GetBlock(doorX, y, GameConstants.HouseAnchorZ));
                Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(doorX - 1, y, GameConstants.HouseAnchorZ));
                Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(doorX + 1, y, GameConstants.HouseAnchorZ));
            }
            Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(12, 71, 6));
            Assert.AreEqual(BlockType.Thatch, chunk.GetBlock(12, 75, 6));
            Assert.AreEqual(BlockType.Air, chunk.GetBlock(13, 75, 6));
        }

        [Test]
        public void House_ClearsExistingTerrainAndTreesFromEntireInterior()
        {
            var chunk = new ChunkData();
            for (int x = 4; x <= 12; x++)
            for (int z = 4; z <= 8; z++)
            for (int y = 71; y <= 74; y++)
                chunk.SetBlock(x, y, z, BlockType.Leaves);
            Place(chunk);
            for (int x = 5; x <= 11; x++)
            for (int z = 5; z <= 7; z++)
            for (int y = 71; y <= 74; y++)
                Assert.AreEqual(BlockType.Air, chunk.GetBlock(x, y, z),
                    $"Furniture must not be embedded in old trees/terrain at {x},{y},{z}.");
        }

        [Test]
        public void GeneratedStartingChunk_HasAnAccessibleNearbyHouse()
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 42);
            var property = typeof(ChunkData).GetProperty("HouseFloorY");
            Assert.IsNotNull(property, "Village population needs generated house metadata.");
            Assert.IsNotNull(property.GetValue(chunk), "A starter house should be available near spawn.");
        }

        private static ChunkData House()
        {
            var chunk = new ChunkData();
            Place(chunk);
            return chunk;
        }

        private static void Place(ChunkData chunk) => typeof(FeatureGenerator)
            .GetMethod("PlaceHouse", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { chunk, GameConstants.HouseAnchorX, 70, GameConstants.HouseAnchorZ });
    }
}

using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;

namespace DeenCraft.Tests.EditMode
{
    public class DoorwayTests
    {
        [TestCase("PlaceHouse", 5)]
        [TestCase("PlaceMosque", 7)]
        public void Doorway_IsThreeBlocksTallWithWallAbove(string methodName, int depth)
        {
            const int anchor = 2;
            const int floor = 70;
            var chunk = new ChunkData();
            typeof(FeatureGenerator).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { chunk, anchor, floor, anchor });
            int width = methodName == "PlaceHouse" ? GameConstants.HouseWidth : GameConstants.MosqueCourtyard;
            int doorX = anchor + width / 2;

            for (int height = 1; height <= 3; height++)
                Assert.AreEqual(BlockType.Air, chunk.GetBlock(doorX, floor + height, anchor),
                    $"Doorway must be clear at height {height}.");
            Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(doorX, floor + 4, anchor));
            Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(doorX - 1, floor + 3, anchor));
            Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(doorX + 1, floor + 3, anchor));
            Assert.AreEqual(BlockType.MudBrick, chunk.GetBlock(doorX, floor + 3, anchor + depth - 1));
        }
    }
}

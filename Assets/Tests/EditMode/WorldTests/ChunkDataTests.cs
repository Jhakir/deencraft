using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;
using System;

namespace DeenCraft.Tests.EditMode
{
    public class ChunkDataTests
    {
        private ChunkData _chunk;

        [SetUp]
        public void SetUp() => _chunk = new ChunkData();

        [Test]
        public void NewChunk_IsAllAir()
        {
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                Assert.AreEqual(BlockType.Air, _chunk.GetBlock(x, 0, z));
        }

        [Test]
        public void SetAndGetBlock_RoundTrips()
        {
            _chunk.SetBlock(3, 10, 7, BlockType.Stone);
            Assert.AreEqual(BlockType.Stone, _chunk.GetBlock(3, 10, 7));
        }

        [Test]
        public void SetBlock_DoesNotCorruptNeighbors()
        {
            _chunk.SetBlock(5, 5, 5, BlockType.Dirt);
            Assert.AreEqual(BlockType.Air, _chunk.GetBlock(4, 5, 5));
            Assert.AreEqual(BlockType.Air, _chunk.GetBlock(6, 5, 5));
            Assert.AreEqual(BlockType.Air, _chunk.GetBlock(5, 5, 4));
            Assert.AreEqual(BlockType.Air, _chunk.GetBlock(5, 5, 6));
        }

        [Test]
        public void GetBlock_OutOfBoundsX_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _chunk.GetBlock(99, 0, 0));

        [Test]
        public void GetBlock_OutOfBoundsY_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _chunk.GetBlock(0, 999, 0));

        [Test]
        public void SetBlock_OutOfBounds_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _chunk.SetBlock(0, 999, 0, BlockType.Stone));

        [Test]
        public void IsInBounds_ReturnsTrueForValidCoords() =>
            Assert.IsTrue(_chunk.IsInBounds(0, 0, 0));

        [Test]
        public void IsInBounds_ReturnsFalseForNegative() =>
            Assert.IsFalse(_chunk.IsInBounds(-1, 0, 0));

        [Test]
        public void IsInBounds_ReturnsFalseForXAtChunkWidth() =>
            Assert.IsFalse(_chunk.IsInBounds(GameConstants.ChunkWidth, 0, 0));

        [Test]
        public void IsInBounds_ReturnsTrueForMaxValidCoords() =>
            Assert.IsTrue(_chunk.IsInBounds(
                GameConstants.ChunkWidth - 1,
                GameConstants.ChunkHeight - 1,
                GameConstants.ChunkDepth - 1));

        [Test]
        public void ChunkSize_MatchesGameConstants()
        {
            Assert.AreEqual(
                GameConstants.ChunkWidth * GameConstants.ChunkHeight * GameConstants.ChunkDepth,
                _chunk.BlockCount);
        }

        [Test]
        public void SetMultipleBlocks_AllRetainValues()
        {
            _chunk.SetBlock(0, 0, 0, BlockType.Stone);
            _chunk.SetBlock(15, 255, 15, BlockType.Sand);
            _chunk.SetBlock(8, 128, 8, BlockType.Water);

            Assert.AreEqual(BlockType.Stone, _chunk.GetBlock(0, 0, 0));
            Assert.AreEqual(BlockType.Sand, _chunk.GetBlock(15, 255, 15));
            Assert.AreEqual(BlockType.Water, _chunk.GetBlock(8, 128, 8));
        }
    }
}

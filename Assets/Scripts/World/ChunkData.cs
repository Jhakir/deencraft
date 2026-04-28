using System;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// Stores the 16×256×16 voxel block data for one chunk.
    /// Pure C# — no Unity dependencies. Safe for EditMode tests.
    /// Index formula: x + ChunkWidth * (y + ChunkHeight * z)
    /// </summary>
    public sealed class ChunkData
    {
        private readonly byte[] _blocks;

        public int BlockCount => _blocks.Length;

        public ChunkData()
        {
            _blocks = new byte[GameConstants.ChunkWidth * GameConstants.ChunkHeight * GameConstants.ChunkDepth];
        }

        public bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < GameConstants.ChunkWidth
                && y >= 0 && y < GameConstants.ChunkHeight
                && z >= 0 && z < GameConstants.ChunkDepth;
        }

        public BlockType GetBlock(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
                throw new ArgumentOutOfRangeException(
                    $"Block position ({x},{y},{z}) is outside chunk bounds " +
                    $"(0–{GameConstants.ChunkWidth - 1}, 0–{GameConstants.ChunkHeight - 1}, 0–{GameConstants.ChunkDepth - 1}).");
            return (BlockType)_blocks[FlatIndex(x, y, z)];
        }

        public void SetBlock(int x, int y, int z, BlockType blockType)
        {
            if (!IsInBounds(x, y, z))
                throw new ArgumentOutOfRangeException(
                    $"Block position ({x},{y},{z}) is outside chunk bounds " +
                    $"(0–{GameConstants.ChunkWidth - 1}, 0–{GameConstants.ChunkHeight - 1}, 0–{GameConstants.ChunkDepth - 1}).");
            _blocks[FlatIndex(x, y, z)] = (byte)blockType;
        }

        private static int FlatIndex(int x, int y, int z)
            => x + GameConstants.ChunkWidth * (y + GameConstants.ChunkHeight * z);
    }
}

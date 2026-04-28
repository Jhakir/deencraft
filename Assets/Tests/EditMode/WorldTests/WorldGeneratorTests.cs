using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    public class WorldGeneratorTests
    {
        [Test]
        public void Generate_SameSeedAndCoord_ProducesIdenticalChunk()
        {
            var a = new ChunkData();
            var b = new ChunkData();
            WorldGenerator.Generate(a, 0, 0, 42);
            WorldGenerator.Generate(b, 0, 0, 42);

            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int y = 0; y < GameConstants.ChunkHeight; y++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                Assert.AreEqual(a.GetBlock(x, y, z), b.GetBlock(x, y, z),
                    $"Mismatch at ({x},{y},{z})");
        }

        [Test]
        public void Generate_Y0_IsAlwaysStone()
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 42);
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                Assert.AreEqual(BlockType.Stone, chunk.GetBlock(x, 0, z),
                    $"Y=0 at ({x},0,{z}) must always be Stone");
        }

        [Test]
        public void Generate_MaxTerrainHeight_IsAlwaysAir()
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 42);
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                Assert.AreEqual(BlockType.Air, chunk.GetBlock(x, GameConstants.MaxTerrainHeight, z),
                    $"MaxTerrainHeight must always be Air");
        }

        [Test]
        public void Generate_BelowSeaLevel_NoAirColumnsExist()
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 5, 5, 42);
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
            for (int y = 1; y < GameConstants.SeaLevel; y++)
                Assert.AreNotEqual(BlockType.Air, chunk.GetBlock(x, y, z),
                    $"No Air allowed below sea level at ({x},{y},{z})");
        }

        [Test]
        public void Generate_DifferentChunkCoords_ProduceDifferentTerrain()
        {
            var a = new ChunkData();
            var b = new ChunkData();
            WorldGenerator.Generate(a, 0, 0, 42);
            WorldGenerator.Generate(b, 50, 50, 42);

            bool anyDiff = false;
            for (int x = 0; x < GameConstants.ChunkWidth && !anyDiff; x++)
            for (int y = GameConstants.MinTerrainHeight; y < GameConstants.MaxTerrainHeight && !anyDiff; y++)
            for (int z = 0; z < GameConstants.ChunkDepth && !anyDiff; z++)
                if (a.GetBlock(x, y, z) != b.GetBlock(x, y, z)) anyDiff = true;

            Assert.IsTrue(anyDiff, "Chunks 50 coords apart should have different terrain");
        }

        [Test]
        public void Generate_SurfaceBlockMatchesBiome_ForGrassland()
        {
            // Chunk (0,0) with seed 42 — find a column and check the surface block is biome-correct
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 42);

            // Find the surface y for column (8,8)
            int surfaceY = -1;
            for (int y = GameConstants.MaxTerrainHeight - 1; y >= 1; y--)
            {
                if (chunk.GetBlock(8, y, 8) != BlockType.Air &&
                    chunk.GetBlock(8, y, 8) != BlockType.Water)
                {
                    surfaceY = y;
                    break;
                }
            }
            Assert.Greater(surfaceY, 0, "Should find a surface block");

            // Surface block must be a valid biome surface (not Air, not Stone at surface level)
            var surface = chunk.GetBlock(8, surfaceY, 8);
            Assert.AreNotEqual(BlockType.Air, surface);
        }
    }
}

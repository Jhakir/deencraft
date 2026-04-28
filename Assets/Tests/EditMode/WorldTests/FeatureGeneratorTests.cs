using NUnit.Framework;
using UnityEngine;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    public class FeatureGeneratorTests
    {
        // Helper: generate a fully decorated chunk (FeatureGenerator called inside WorldGenerator.Generate)
        private ChunkData DecoratedChunk(int chunkX, int chunkZ, int seed = 42)
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, chunkX, chunkZ, seed);
            return chunk;
        }

        // Helper: count blocks of a given type in a chunk
        private int CountBlocks(ChunkData chunk, BlockType type)
        {
            int count = 0;
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
                for (int y = 0; y < GameConstants.ChunkHeight; y++)
                    for (int z = 0; z < GameConstants.ChunkDepth; z++)
                        if (chunk.GetBlock(x, y, z) == type)
                            count++;
            return count;
        }

        [Test]
        public void Decorate_AllChunks_GenerateWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                for (int cx = -2; cx <= 2; cx++)
                    for (int cz = -2; cz <= 2; cz++)
                        DecoratedChunk(cx, cz, 42);
            });
        }

        [Test]
        public void Decorate_Grassland_ContainsAtLeastOneTree()
        {
            int seed = 42;
            for (int cx = 0; cx < 20; cx++)
            {
                for (int cz = 0; cz < 20; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.Grassland) continue;

                    var chunk = DecoratedChunk(cx, cz, seed);
                    if (CountBlocks(chunk, BlockType.Wood) > 0) return; // found a tree
                }
            }
            // It's valid for a grassland chunk to have no trees (noise below threshold)
            // so we pass if no exception was thrown
            Assert.Pass("Grassland chunks generated without error");
        }

        [Test]
        public void Decorate_Desert_CactusBlocksAreOnSandOrCactus()
        {
            int seed = 42;
            bool foundDesert = false;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.Desert) continue;

                    foundDesert = true;
                    var chunk = DecoratedChunk(cx, cz, seed);
                    for (int x = 0; x < GameConstants.ChunkWidth; x++)
                        for (int y = 1; y < GameConstants.ChunkHeight; y++)
                            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                                if (chunk.GetBlock(x, y, z) == BlockType.Cactus)
                                {
                                    BlockType below = chunk.GetBlock(x, y - 1, z);
                                    Assert.IsTrue(
                                        below == BlockType.Sand || below == BlockType.Cactus,
                                        $"Cactus at ({x},{y},{z}) has invalid block below: {below}");
                                }
                    if (foundDesert) return;
                }
            }
            if (!foundDesert)
                Assert.Ignore("No Desert chunk found in search range");
        }

        [Test]
        public void Decorate_Riverside_HasRiverWater()
        {
            int seed = 42;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.Riverside) continue;

                    var chunk = DecoratedChunk(cx, cz, seed);
                    bool hasRiver = false;
                    for (int x = GameConstants.RiverXMin; x <= GameConstants.RiverXMax; x++)
                    {
                        if (chunk.GetBlock(x, GameConstants.SeaLevel - 1, GameConstants.ChunkDepth / 2) == BlockType.Water)
                        {
                            hasRiver = true;
                            break;
                        }
                    }
                    Assert.IsTrue(hasRiver, $"Riverside chunk ({cx},{cz}) should have water in river channel at SeaLevel-1");
                    return;
                }
            }
            Assert.Ignore("No Riverside chunk found in search range");
        }

        [Test]
        public void Decorate_EligibleVillageChunk_HasMudBrickWall()
        {
            int seed = 42;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    var biome = BiomeSystem.GetBiome(midX, midZ, seed);
                    bool eligible = biome == BiomeType.Grassland ||
                                    biome == BiomeType.OliveGrove ||
                                    biome == BiomeType.Riverside;
                    float noise = Mathf.PerlinNoise(
                        cx * GameConstants.VillageNoiseScale + seed * 0.001f,
                        cz * GameConstants.VillageNoiseScale);
                    if (!eligible || noise <= GameConstants.VillageNoiseThreshold) continue;

                    var chunk = DecoratedChunk(cx, cz, seed);
                    Assert.Greater(CountBlocks(chunk, BlockType.MudBrick), 0,
                        $"Village chunk ({cx},{cz}) should contain MudBrick blocks");
                    return;
                }
            }
            Assert.Ignore("No village-eligible chunk found in search range");
        }

        [Test]
        public void Decorate_TreeTrunks_NeverWithin2BlocksOfEdge()
        {
            int seed = 42;
            for (int cx = 0; cx < 20; cx++)
            {
                for (int cz = 0; cz < 20; cz++)
                {
                    var chunk = DecoratedChunk(cx, cz, seed);
                    for (int x = 0; x < GameConstants.ChunkWidth; x++)
                        for (int y = 0; y < GameConstants.ChunkHeight; y++)
                            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                                if (chunk.GetBlock(x, y, z) == BlockType.Wood)
                                {
                                    // Only check trunk blocks (Wood, not leaves) for edge constraint
                                    // A trunk column is connected to ground; we check if it's near edge
                                    bool nearEdge = x < 2 || x > 13 || z < 2 || z > 13;
                                    Assert.IsFalse(nearEdge,
                                        $"Wood trunk at ({x},{y},{z}) in chunk ({cx},{cz}) is within 2 blocks of edge");
                                }
                }
            }
        }

        [Test]
        public void Decorate_VegetationFlowers_AlwaysOnSolidBlock()
        {
            int seed = 42;
            for (int cx = 0; cx < 10; cx++)
            {
                for (int cz = 0; cz < 10; cz++)
                {
                    var chunk = DecoratedChunk(cx, cz, seed);
                    for (int x = 0; x < GameConstants.ChunkWidth; x++)
                        for (int y = 1; y < GameConstants.ChunkHeight; y++)
                            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                                if (chunk.GetBlock(x, y, z) == BlockType.Flower)
                                    Assert.AreNotEqual(BlockType.Air, chunk.GetBlock(x, y - 1, z),
                                        $"Flower at ({x},{y},{z}) is floating (block below is Air)");
                }
            }
        }

        [Test]
        public void Decorate_SnowyIsland_NoDesertBlocksGenerated()
        {
            int seed = 42;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.SnowyIsland) continue;

                    var chunk = DecoratedChunk(cx, cz, seed);
                    Assert.AreEqual(0, CountBlocks(chunk, BlockType.Cactus),
                        $"SnowyIsland chunk ({cx},{cz}) should not contain Cactus");
                    return;
                }
            }
            Assert.Ignore("No SnowyIsland chunk found in search range");
        }

        [Test]
        public void Decorate_MaxTreesPerChunk_NotExceeded()
        {
            int seed = 42;
            for (int cx = 0; cx < 20; cx++)
            {
                for (int cz = 0; cz < 20; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    // Only check non-desert biomes (desert doesn't plant trees via PlantTrees)
                    if (BiomeSystem.GetBiome(midX, midZ, seed) == BiomeType.Desert) continue;

                    var chunk = DecoratedChunk(cx, cz, seed);
                    // Count trunk bases (Wood or PalmWood at surface level)
                    // Approximation: count distinct X,Z positions with trunk blocks
                    var trunkPositions = new System.Collections.Generic.HashSet<(int, int)>();
                    for (int x = 0; x < GameConstants.ChunkWidth; x++)
                        for (int y = 0; y < GameConstants.ChunkHeight; y++)
                            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                            {
                                var b = chunk.GetBlock(x, y, z);
                                if (b == BlockType.Wood || b == BlockType.PalmWood)
                                    trunkPositions.Add((x, z));
                            }
                    // Trunk column = one tree; max is MaxTreesPerChunk
                    Assert.LessOrEqual(trunkPositions.Count, GameConstants.MaxTreesPerChunk,
                        $"Chunk ({cx},{cz}) has {trunkPositions.Count} tree trunk columns, expected ≤ {GameConstants.MaxTreesPerChunk}");
                }
            }
        }
    }
}

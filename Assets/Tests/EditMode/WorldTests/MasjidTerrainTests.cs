using System;
using System.Reflection;
using DeenCraft;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MasjidTerrainTests
    {
        private static MethodInfo TerrainMethod()
        {
            MethodInfo method = typeof(WorldGenerator).GetMethod("GenerateTerrain",
                new[] { typeof(ChunkData), typeof(int), typeof(int), typeof(int) });
            Assert.That(method, Is.Not.Null, "Terrain must be available independently of decorations.");
            return method;
        }

        [Test]
        public void GenerateTerrain_DoesNotPlaceHouseOrVegetation()
        {
            var chunk = new ChunkData();
            TerrainMethod().Invoke(null, new object[] { chunk, 0, 0, 42 });
            Assert.That(chunk.HouseFloorY, Is.Null);
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
            for (int y = 0; y < GameConstants.ChunkHeight; y++)
            {
                BlockType block = chunk.GetBlock(x, y, z);
                Assert.That(block == BlockType.Air || block == BlockType.Stone ||
                    block == BlockType.Dirt || block == BlockType.Grass || block == BlockType.Snow ||
                    block == BlockType.Sand || block == BlockType.Water, Is.True, block.ToString());
            }
        }

        [TestCase(BiomeType.Desert)]
        [TestCase(BiomeType.Riverside)]
        [TestCase(BiomeType.Grassland)]
        [TestCase(BiomeType.SnowyIsland)]
        [TestCase(BiomeType.OliveGrove)]
        public void EffectiveSurface_MatchesAllGeneratedColumns(BiomeType biome)
        {
            MethodInfo sampler = typeof(WorldGenerator).GetMethod("GetTerrainSurfaceY",
                new[] { typeof(int), typeof(int), typeof(int) });
            Assert.That(sampler, Is.Not.Null, "Exact terrain sampling must include dunes and river carving.");
            Vector2Int coord = FindBiome(biome);
            var chunk = new ChunkData();
            TerrainMethod().Invoke(null, new object[] { chunk, coord.x, coord.y, 42 });
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
            {
                int expected = GameConstants.ChunkHeight - 1;
                while (expected > 0 && chunk.GetBlock(x, expected, z) == BlockType.Air) expected--;
                int actual = (int)sampler.Invoke(null,
                    new object[] { coord.x * GameConstants.ChunkWidth + x,
                        coord.y * GameConstants.ChunkDepth + z, 42 });
                Assert.That(actual, Is.EqualTo(expected), $"column {coord}:{x},{z}");
            }
        }

        private static Vector2Int FindBiome(BiomeType type)
        {
            for (int x = -80; x <= 80; x++)
            for (int z = -80; z <= 80; z++)
                if (BiomeSystem.GetBiome(x * 16 + 8, z * 16 + 8, 42) == type)
                    return new Vector2Int(x, z);
            throw new InvalidOperationException("The test search did not find " + type);
        }
    }
}

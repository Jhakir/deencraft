// Assets/Tests/EditMode/WorldTests/AppleTreeTests.cs
using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class AppleTreeTests
    {
        private bool ChunkContains(ChunkData chunk, BlockType type)
        {
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
                for (int z = 0; z < GameConstants.ChunkDepth; z++)
                    for (int y = 0; y < GameConstants.ChunkHeight; y++)
                        if (chunk.GetBlock(x, y, z) == type) return true;
            return false;
        }

        [Test]
        public void Grassland_Biome_ContainsAppleWood_InSomeChunk()
        {
            // Search a 30×30 grid for at least one Grassland chunk containing AppleWood.
            int seed = 42;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.Grassland) continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, seed);

                    if (ChunkContains(chunk, BlockType.AppleWood)) return; // PASS
                }
            }
            Assert.Fail("No AppleWood block found in any Grassland chunk across a 30×30 grid (seed 42).");
        }

        [Test]
        public void Grassland_Biome_ContainsAppleLeaves_InSomeChunk()
        {
            int seed = 42;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.Grassland) continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, seed);

                    if (ChunkContains(chunk, BlockType.AppleLeaves)) return; // PASS
                }
            }
            Assert.Fail("No AppleLeaves block found in any Grassland chunk across a 30×30 grid (seed 42).");
        }

        [Test]
        public void AppleWood_NeverAppearsInDesert()
        {
            int seed = 42;
            for (int cx = 0; cx < 20; cx++)
            {
                for (int cz = 0; cz < 20; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.Desert) continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, seed);

                    Assert.IsFalse(ChunkContains(chunk, BlockType.AppleWood),
                        $"Apple tree appeared in Desert chunk ({cx},{cz})");
                }
            }
        }
    }
}

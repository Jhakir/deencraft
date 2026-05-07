// Assets/Tests/EditMode/WorldTests/MosqueTests.cs
using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MosqueTests
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
        public void Mosque_BlocksAppear_InSomeVillageChunk()
        {
            // Search a 30×30 grid for any Grassland/OliveGrove chunk that contains Minaret/Dome.
            int seed = 42;
            for (int cx = 0; cx < 30; cx++)
            {
                for (int cz = 0; cz < 30; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    var biome = BiomeSystem.GetBiome(midX, midZ, seed);
                    if (biome != BiomeType.Grassland && biome != BiomeType.OliveGrove) continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, seed);

                    if (ChunkContains(chunk, BlockType.Minaret) ||
                        ChunkContains(chunk, BlockType.Dome))
                        return; // PASS
                }
            }
            Assert.Fail("No Minaret or Dome block found in any Grassland/OliveGrove chunk across a 30×30 grid (seed 42).");
        }

        [Test]
        public void Mosque_NeverAppearsInDesert()
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

                    Assert.IsFalse(ChunkContains(chunk, BlockType.Minaret),
                        $"Minaret appeared in Desert chunk ({cx},{cz})");
                    Assert.IsFalse(ChunkContains(chunk, BlockType.Dome),
                        $"Dome appeared in Desert chunk ({cx},{cz})");
                }
            }
        }

        [Test]
        public void Mosque_NeverAppearsInSnowyIsland()
        {
            int seed = 42;
            for (int cx = 0; cx < 20; cx++)
            {
                for (int cz = 0; cz < 20; cz++)
                {
                    int midX = cx * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2;
                    int midZ = cz * GameConstants.ChunkDepth + GameConstants.ChunkDepth / 2;
                    if (BiomeSystem.GetBiome(midX, midZ, seed) != BiomeType.SnowyIsland) continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, seed);

                    Assert.IsFalse(ChunkContains(chunk, BlockType.Minaret),
                        $"Minaret appeared in SnowyIsland chunk ({cx},{cz})");
                }
            }
        }
    }
}

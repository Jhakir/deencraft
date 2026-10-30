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
        public void Mosque_BlocksAppear_InEveryReservedDistrictComplex()
        {
            var district = MasjidDistrictPlanner.Create(42, bounds => false);
            foreach (MasjidComplexLayout complex in district.Complexes)
            {
                var coord = ChunkManager.WorldToChunkCoord(complex.Bounds.min +
                    new UnityEngine.Vector3Int(5, 0, 5));
                var chunk = new ChunkData();
                WorldGenerator.Generate(chunk, coord.x, coord.y, 42, district);
                Assert.That(ChunkContains(chunk, BlockType.Minaret), Is.True, complex.Style.ToString());
            }
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

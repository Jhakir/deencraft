using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    public class ChunkMesherTests
    {
        [Test]
        public void AllAirChunk_ProducesEmptyMesh()
        {
            var chunk = new ChunkData(); // default: all Air
            var mesh  = ChunkMesher.BuildMesh(chunk, null);
            Assert.AreEqual(0, mesh.Vertices.Length,  "Air-only chunk must have 0 vertices");
            Assert.AreEqual(0, mesh.Triangles.Length, "Air-only chunk must have 0 triangles");
        }

        [Test]
        public void SingleSolidBlock_ProducesTwentyFourVertices()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(0, 1, 0, BlockType.Stone);
            var mesh = ChunkMesher.BuildMesh(chunk, null);
            // 6 exposed faces × 4 vertices = 24 (greedy won't merge different axis faces)
            Assert.AreEqual(24, mesh.Vertices.Length,
                "Single isolated block must produce 24 vertices (6 faces × 4)");
            Assert.AreEqual(36, mesh.Triangles.Length,
                "Single isolated block must produce 36 triangle indices (6 faces × 6)");
        }

        [Test]
        public void TwoAdjacentSameBlocks_FewerVerticesThanTwoIsolatedBlocks()
        {
            var adjacent = new ChunkData();
            adjacent.SetBlock(0, 1, 0, BlockType.Stone);
            adjacent.SetBlock(1, 1, 0, BlockType.Stone);

            var single = new ChunkData();
            single.SetBlock(0, 1, 0, BlockType.Stone);

            var meshAdjacent = ChunkMesher.BuildMesh(adjacent, null);
            var meshSingle   = ChunkMesher.BuildMesh(single,   null);

            // Two adjacent blocks share an internal face; greedy should merge external top faces
            Assert.Less(meshAdjacent.Vertices.Length, meshSingle.Vertices.Length * 2,
                "Two adjacent same-type blocks must have fewer vertices than two isolated blocks");
        }

        [Test]
        public void WaterBlock_ProducesFaces()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, BlockType.Water);
            var mesh = ChunkMesher.BuildMesh(chunk, null);
            Assert.Greater(mesh.Vertices.Length, 0, "Water block must produce faces");
        }

        [Test]
        public void FullChunk_DoesNotThrow()
        {
            var chunk = new ChunkData();
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int y = 0; y < GameConstants.ChunkHeight; y++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                chunk.SetBlock(x, y, z, BlockType.Stone);

            // A completely solid chunk has no exposed faces from outside (all internal faces culled)
            Assert.DoesNotThrow(() => ChunkMesher.BuildMesh(chunk, null),
                "Building mesh for a full chunk must not throw");
        }

        [Test]
        public void MeshData_Empty_HasZeroArrays()
        {
            Assert.AreEqual(0, MeshData.Empty.Vertices.Length);
            Assert.AreEqual(0, MeshData.Empty.Triangles.Length);
            Assert.AreEqual(0, MeshData.Empty.UVs.Length);
        }
    }
}

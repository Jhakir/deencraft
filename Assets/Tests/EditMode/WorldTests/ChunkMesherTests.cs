using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;
using UnityEngine;

namespace DeenCraft.Tests.EditMode
{
    public class ChunkMesherTests
    {
        [Test]
        public void SingleBlock_AllTrianglesFaceAwayFromBlockCentre()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, BlockType.Stone);
            var mesh = ChunkMesher.BuildMesh(chunk, null);
            var centre = new Vector3(5.5f, 5.5f, 5.5f);

            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[i]];
                Vector3 b = mesh.Vertices[mesh.Triangles[i + 1]];
                Vector3 c = mesh.Vertices[mesh.Triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Assert.Greater(Vector3.Dot(normal, (a + b + c) / 3f - centre), 0f,
                    $"Triangle {i / 3} faces inside the block.");
            }
        }

        [Test]
        public void ChunkCollider_CatchesPlayerRayFromAbove()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, BlockType.Stone);
            var go = new GameObject("Terrain collision test");
            Mesh mesh = null;
            bool originalBackfaces = Physics.queriesHitBackfaces;
            try
            {
                Physics.queriesHitBackfaces = false;
                var view = go.AddComponent<ChunkView>();
                view.ApplyMesh(ChunkMesher.BuildMesh(chunk, null));
                mesh = go.GetComponent<MeshFilter>().sharedMesh;
                Physics.SyncTransforms();

                Assert.IsTrue(go.GetComponent<MeshCollider>().Raycast(
                    new Ray(new Vector3(5.5f, 10f, 5.5f), Vector3.down),
                    out RaycastHit hit, 10f), "Ground must collide from above, not only below.");
                Assert.AreEqual(6f, hit.point.y, 0.001f);
                Assert.Greater(hit.normal.y, 0f);
            }
            finally
            {
                Physics.queriesHitBackfaces = originalBackfaces;
                Object.DestroyImmediate(go);
                if (mesh != null) Object.DestroyImmediate(mesh);
            }
        }

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

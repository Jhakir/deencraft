using System.Collections.Generic;
using System.Reflection;
using DeenCraft;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode
{
    public class ChunkStreamingTests
    {
        private GameObject _root;
        private ChunkManager _manager;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Chunk streaming test");
            _manager = _root.AddComponent<ChunkManager>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var filter in _root.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null) Object.DestroyImmediate(filter.sharedMesh);
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Refresh_LoadsPlayerChunkBeforeDistantChunks()
        {
            var centre = new Vector2Int(-3, 5);
            Refresh(centre);
            Assert.AreEqual(centre, Queue.Peek());
        }

        [Test]
        public void Refresh_DropsPendingChunksOutsideNewView()
        {
            Refresh(Vector2Int.zero);
            var centre = new Vector2Int(100, 100);
            Refresh(centre);
            Assert.AreEqual(centre, Queue.Peek());
        }

        [Test]
        public void SetSeed_CanQueuePreviouslyPendingChunksAgain()
        {
            Refresh(Vector2Int.zero);
            _manager.SetSeed(123);
            Refresh(Vector2Int.zero);
            Assert.Greater(Queue.Count, 0, "A world reset must not retain stale queue membership.");
        }

        [Test]
        public void SurfaceQuery_UsesLoadedBlocksIncludingRaisedTerrain()
        {
            var data = new ChunkData();
            data.SetBlock(5, 120, 5, BlockType.Sand);
            AddTerrain(new Vector2Int(-1, -1), data);
            Assert.IsTrue(_manager.TryGetSurfaceHeight(-11f, -11f, out float height));
            Assert.AreEqual(121f, height);
        }

        [Test]
        public void SurfaceQuery_RejectsMissingCollider()
        {
            var cache = Field<Dictionary<Vector2Int, ChunkData>>("_chunkDataCache");
            var data = new ChunkData();
            data.SetBlock(5, 80, 5, BlockType.Stone);
            cache[Vector2Int.zero] = data;
            Assert.IsFalse(_manager.TryGetSurfaceHeight(5f, 5f, out _),
                "Cached blocks alone must not release spawn protection.");
        }

        [Test]
        public void SurfaceQuery_RejectsEmptyColumn()
        {
            AddTerrain(Vector2Int.zero, new ChunkData());
            Assert.IsFalse(_manager.TryGetSurfaceHeight(5f, 5f, out _));
        }

        private void AddTerrain(Vector2Int coord, ChunkData data)
        {
            var viewObject = new GameObject("Loaded test chunk");
            viewObject.transform.SetParent(_root.transform);
            viewObject.transform.position = new Vector3(
                coord.x * GameConstants.ChunkWidth, 0f, coord.y * GameConstants.ChunkDepth);
            var view = viewObject.AddComponent<ChunkView>();
            view.ApplyMesh(ChunkMesher.BuildMesh(data, null));
            Field<Dictionary<Vector2Int, ChunkData>>("_chunkDataCache")[coord] = data;
            Field<Dictionary<Vector2Int, ChunkView>>("_activeViews")[coord] = view;
        }

        private T Field<T>(string name) => (T)typeof(ChunkManager)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);

        private Queue<Vector2Int> Queue => (Queue<Vector2Int>)typeof(ChunkManager)
            .GetField("_loadQueue", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_manager);

        private void Refresh(Vector2Int centre)
        {
            typeof(ChunkManager).GetMethod("RefreshChunkSet",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_manager, new object[] { centre });
        }
    }
}

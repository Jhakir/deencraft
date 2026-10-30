using System.Collections.Generic;
using System.Reflection;
using DeenCraft.Player;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode
{
    public class PlayerRecoveryTests
    {
        private GameObject _world;
        private GameObject _player;
        private PlayerController _controller;
        private ChunkManager _manager;
        private Mesh _mesh;

        [SetUp]
        public void SetUp()
        {
            _world = new GameObject("Recovery test world");
            _manager = _world.AddComponent<ChunkManager>();
            _player = new GameObject("Recovery test player");
            _player.SetActive(false);
            var cc = _player.AddComponent<CharacterController>();
            cc.height = 2f;
            _controller = _player.AddComponent<PlayerController>();
            Set("_cc", cc);
            Set("_chunkManager", _manager);
            Set("_spawnReady", true);
            _player.transform.position = new Vector3(5.5f, 100.5f, 5.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_world);
            if (_mesh != null) Object.DestroyImmediate(_mesh);
        }

        [Test]
        public void InsideSolidTerrain_ReturnsToActualTopSurface()
        {
            LoadGround();
            Call("GuardAgainstVoidFall");
            Assert.IsFalse(Get<bool>("_spawnReady"), "Being inside terrain must trigger recovery.");
            Call("WaitForSpawnGround");
            Assert.AreEqual(102.1f, _player.transform.position.y, 0.001f);
            Assert.AreEqual(0f, Get<float>("_verticalVelocity"));
        }

        [Test]
        public void Spawn_NeverReleasesBeforeTerrainLoadsEvenAfterTimeout()
        {
            Set("_spawnReady", false);
            Set("_spawnCheckTimer", 20f);
            Call("WaitForSpawnGround");
            Assert.IsFalse(Get<bool>("_spawnReady"));
        }

        [Test]
        public void VoidFall_ReturnsToActualTopSurface()
        {
            LoadGround();
            _player.transform.position = new Vector3(5.5f, -10f, 5.5f);
            Set("_verticalVelocity", -100f);
            Call("GuardAgainstVoidFall");
            Call("WaitForSpawnGround");
            Assert.AreEqual(102.1f, _player.transform.position.y, 0.001f);
            Assert.AreEqual(0f, Get<float>("_verticalVelocity"));
        }

        [Test]
        public void AirAboveGround_DoesNotTriggerRecovery()
        {
            LoadGround();
            _player.transform.position = new Vector3(5.5f, 110f, 5.5f);
            Call("GuardAgainstVoidFall");
            Assert.IsTrue(Get<bool>("_spawnReady"));
        }

        private void LoadGround()
        {
            var data = new ChunkData();
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                data.SetBlock(x, 100, z, BlockType.Sand);
            var go = new GameObject("Recovery terrain");
            go.transform.SetParent(_world.transform);
            var view = go.AddComponent<ChunkView>();
            view.ApplyMesh(ChunkMesher.BuildMesh(data, null));
            _mesh = go.GetComponent<MeshFilter>().sharedMesh;
            WorldField<Dictionary<Vector2Int, ChunkData>>("_chunkDataCache")[Vector2Int.zero] = data;
            WorldField<Dictionary<Vector2Int, ChunkView>>("_activeViews")[Vector2Int.zero] = view;
            Physics.SyncTransforms();
        }

        private T WorldField<T>(string name) => (T)typeof(ChunkManager)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);

        private T Get<T>(string name) => (T)typeof(PlayerController)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller);

        private void Set(string name, object value) => typeof(PlayerController)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_controller, value);

        private void Call(string name) => typeof(PlayerController)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_controller, null);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeenCraft.Player;
using DeenCraft.World;
using DeenCraft.Animals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeenCraft.Tests.PlayMode
{
    public class PlayerMovementTests
    {
        private GameObject _world;
        private GameObject _player;
        private GameObject _camera;
        private PlayerController _controller;
        private Mesh _mesh;
        private int _previousCaptureFramerate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = 60;
            _world = new GameObject("Movement test world");
            _world.SetActive(false);
            var manager = _world.AddComponent<ChunkManager>();
            Set(manager, "_viewDistance", 1);
            Set(manager, "_poolSize", 9);
            manager.enabled = false;
            _world.SetActive(true);

            var data = new ChunkData();
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
                data.SetBlock(x, 10, z, BlockType.Stone);
            var terrain = new GameObject("Movement test ground");
            terrain.transform.SetParent(_world.transform);
            var view = terrain.AddComponent<ChunkView>();
            view.ApplyMesh(ChunkMesher.BuildMesh(data, null));
            _mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Get<Dictionary<Vector2Int, ChunkData>>(manager, "_chunkDataCache")[Vector2Int.zero] = data;
            Get<Dictionary<Vector2Int, ChunkView>>(manager, "_activeViews")[Vector2Int.zero] = view;

            _camera = new GameObject("Movement test camera");
            _player = new GameObject("Movement test player");
            _player.SetActive(false);
            new GameObject("PlayerBody").transform.SetParent(_player.transform);
            var cc = _player.AddComponent<CharacterController>();
            cc.height = 2f;
            _controller = _player.AddComponent<PlayerController>();
            Set(_controller, "_cameraTarget", _camera.transform);
            Set(_controller, "_spawnReady", true);
            _controller.enabled = false;
            _player.transform.position = new Vector3(8f, 16f, 8f);
            _player.SetActive(true);
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = _previousCaptureFramerate;
            Object.Destroy(_player);
            Object.Destroy(_camera);
            Object.Destroy(_world);
            Object.Destroy(_mesh);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Flying_HoversAndTurningOffFlightRestoresGravity()
        {
            var flight = Get<FlightInput>(_controller, "_flightInput");
            flight.PressJump(0f);
            flight.PressJump(0.1f);
            float startY = _player.transform.position.y;
            for (int i = 0; i < 10; i++)
            {
                Move();
                yield return null;
            }
            Assert.IsTrue(_controller.IsFlying);
            Assert.AreEqual(startY, _player.transform.position.y, 0.001f);
            flight.PressJump(1f);
            flight.PressJump(1.1f);
            Move();
            Assert.IsFalse(_controller.IsFlying);
            Assert.Less(Get<float>(_controller, "_verticalVelocity"), 0f);
            Assert.Less(_player.transform.position.y, startY);
        }

        [UnityTest]
        public IEnumerator Falling_LandsOnTopOfTerrainInsteadOfInsideIt()
        {
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                Move();
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(12f, _player.transform.position.y, 0.15f);
            Assert.IsTrue(_player.GetComponent<CharacterController>().isGrounded);
        }

        [Test]
        public void StreamingBoundary_RejectsUnloadedGround()
        {
            var method = typeof(PlayerController).GetMethod("HasTerrainAtPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsTrue((bool)method.Invoke(_controller, new object[] { new Vector3(8f, 16f, 8f) }));
            Assert.IsFalse((bool)method.Invoke(_controller, new object[] { new Vector3(16f, 16f, 8f) }));
        }

        [UnityTest]
        public IEnumerator Animal_WandersOnGroundAndStopsAtUnloadedBoundary()
        {
            var animalObject = new GameObject("Wandering sheep test");
            animalObject.SetActive(false);
            animalObject.transform.SetParent(_world.transform);
            animalObject.transform.position = new Vector3(8f, 11.1f, 8f);
            var collider = animalObject.AddComponent<CharacterController>();
            collider.height = 1f;
            collider.radius = 0.3f;
            collider.center = Vector3.up * 0.5f;
            var animal = animalObject.AddComponent<AnimalController>();
            animal.Configure(AnimalType.Sheep);
            animal.enabled = false;
            animalObject.SetActive(true);
            typeof(AnimalController).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(animal, null);
            animal.Brain.ForceWanderTarget(new Vector3(25f, 11f, 8f));
            var move = typeof(AnimalController).GetMethod("Move", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 180; i++)
            {
                move.Invoke(animal, null);
                yield return null;
            }
            Assert.Greater(animalObject.transform.position.x, 10f);
            Assert.Less(animalObject.transform.position.x, 16f - collider.radius);
            Assert.AreEqual(11f, animalObject.transform.position.y, 0.15f);
        }

        private void Move() => typeof(PlayerController).GetMethod("HandleMovement",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_controller, null);

        private static T Get<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}

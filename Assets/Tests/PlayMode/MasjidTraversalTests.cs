using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeenCraft.Player;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeenCraft.Tests.PlayMode
{
    public class MasjidTraversalTests
    {
        private GameObject _root;
        private GameObject _player;
        private PlayerController _controller;
        private Camera _camera;
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private int _capture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _capture = Time.captureFramerate;
            Time.captureFramerate = 60;
            _root = new GameObject("Masjid traversal tests");
            _player = new GameObject("Player");
            _player.transform.SetParent(_root.transform);
            _player.SetActive(false);
            new GameObject("PlayerBody").transform.SetParent(_player.transform);
            _player.AddComponent<CharacterController>().height = 2f;
            _controller = _player.AddComponent<PlayerController>();
            _controller.enabled = false;
            _camera = new GameObject("Camera").AddComponent<Camera>();
            _camera.transform.SetParent(_root.transform);
            Set("_cameraTarget", _camera.transform);
            _player.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = _capture;
            Object.Destroy(_root);
            foreach (Mesh mesh in _meshes) Object.Destroy(mesh);
            _meshes.Clear();
            yield return null;
        }

        private void Set(string name, object value) =>
            typeof(PlayerController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_controller, value);

        [UnityTest]
        public IEnumerator AllFourComplexes_AllowWalkingThroughDoorHallAndLearningRoom()
        {
            var district = MasjidDistrictPlanner.Create(42, b => false);
            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(_root.transform);
            var loaded = new HashSet<Vector2Int>();
            foreach (MasjidComplexLayout complex in district.Complexes)
            {
                Vector2Int min = ChunkManager.WorldToChunkCoord(complex.Bounds.min);
                Vector2Int max = ChunkManager.WorldToChunkCoord(complex.Bounds.max - Vector3Int.one);
                for (int x = min.x; x <= max.x; x++)
                for (int z = min.y; z <= max.y; z++)
                {
                    if (!loaded.Add(new Vector2Int(x, z))) continue;
                    var data = new ChunkData();
                    WorldGenerator.Generate(data, x, z, 42, district);
                    var go = new GameObject("Masjid chunk");
                    go.transform.SetParent(terrain.transform);
                    go.transform.position = new Vector3(x * 16, 0f, z * 16);
                    ChunkView view = go.AddComponent<ChunkView>();
                    view.ApplyMesh(ChunkMesher.BuildMesh(data, null));
                    _meshes.Add(go.GetComponent<MeshFilter>().sharedMesh);
                }
                _player.transform.position = new Vector3(complex.Bounds.xMin + 15.5f,
                    complex.FloorY + 2.05f, complex.Bounds.zMin + 4f);
                Physics.SyncTransforms();
                CharacterController cc = _player.GetComponent<CharacterController>();
                for (int i = 0; i < 215; i++)
                {
                    cc.Move(new Vector3(0f, -0.02f, 0.08f));
                    yield return null;
                }
                Assert.That(_player.transform.position.z, Is.GreaterThan(complex.Bounds.zMin + 20f), complex.Style.ToString());
                Assert.That(_player.transform.position.y, Is.EqualTo(complex.FloorY + 2f).Within(0.2f));
                _player.transform.position = new Vector3(complex.Bounds.xMin + 15.5f,
                    complex.FloorY + 2.05f, complex.Bounds.zMin + 20.5f);
                Physics.SyncTransforms();
                for (int i = 0; i < 110; i++)
                {
                    cc.Move(new Vector3(0.08f, -0.02f, 0f));
                    yield return null;
                }
                Assert.That(_player.transform.position.x, Is.GreaterThan(complex.Bounds.xMin + 23f),
                    complex.Style + " classroom must be reachable without jumping.");
                Set("_yaw", 90f);
                typeof(PlayerController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_controller, null);
                Assert.That(_camera.transform.position.x, Is.GreaterThan(complex.Bounds.xMin + 19f),
                    "Camera must not pass through the classroom partition.");
            }
        }

        [UnityTest]
        public IEnumerator Approach_CanBeWalkedWithoutJumpingOrFlying()
        {
            MasjidDistrictLayout district = MasjidDistrictPlanner.Create(42, b => false);
            Mesh mesh = MasjidApproachPath.BuildMesh(district);
            _meshes.Add(mesh);
            var approach = new GameObject("Approach", typeof(MeshCollider));
            approach.transform.SetParent(_root.transform);
            approach.GetComponent<MeshCollider>().sharedMesh = mesh;
            Vector3 direction = district.ApproachEnd - district.ApproachStart;
            direction.y = 0f;
            direction.Normalize();
            _player.transform.position = district.ApproachStart + direction * 0.5f + Vector3.up * 1.02f;
            Physics.SyncTransforms();
            var cc = _player.GetComponent<CharacterController>();
            int frames = Mathf.FloorToInt(Vector2.Distance(
                new Vector2(district.ApproachStart.x, district.ApproachStart.z),
                new Vector2(district.ApproachEnd.x, district.ApproachEnd.z)) / 0.08f) - 12;
            for (int i = 0; i < frames; i++)
            {
                cc.Move(direction * 0.08f + Vector3.down * 0.04f);
                yield return null;
            }
            Assert.That(Vector2.Distance(new Vector2(_player.transform.position.x, _player.transform.position.z),
                new Vector2(district.ApproachEnd.x, district.ApproachEnd.z)), Is.LessThan(1.5f));
            Assert.That(_player.transform.position.y, Is.EqualTo(district.FloorY + 2f).Within(0.6f));
        }
    }
}

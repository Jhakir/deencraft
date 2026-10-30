using System.Collections.Generic;
using System.Reflection;
using DeenCraft.Animals;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode
{
    public class AnimalSpawnGroundTests
    {
        private GameObject _world;
        private GameObject _population;
        private GameObject _player;
        private ChunkManager _manager;
        private AnimalSpawner _spawner;
        private Material _material;
        private Mesh _groundMesh;

        [SetUp]
        public void SetUp()
        {
            _world = new GameObject("Animal terrain test");
            _manager = _world.AddComponent<ChunkManager>();
            _population = new GameObject("Animal spawner test");
            _population.SetActive(false);
            _player = new GameObject("Animal test player");
            _player.transform.position = new Vector3(0f, 72f, 0f);
            _spawner = _population.AddComponent<AnimalSpawner>();
            _material = new Material(Shader.Find("DeenCraft/VertexColor"));
            Set("_animalMaterial", _material);
            Set("_chunkManager", _manager);
            Set("_playerTransform", _player.transform);
            Call("CreatePool");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var filter in _population.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null) Object.DestroyImmediate(filter.sharedMesh);
            Object.DestroyImmediate(_population);
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_world);
            if (_groundMesh != null) Object.DestroyImmediate(_groundMesh);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void NoLoadedTerrain_LeavesAllAnimalsInactive()
        {
            Call("RefreshPopulation");
            Assert.AreEqual(0, ActiveAnimals());
        }

        [Test]
        public void WaterSurface_DoesNotSpawnLandAnimals()
        {
            LoadGround(BlockType.Water);
            Call("RefreshPopulation");
            Assert.AreEqual(0, ActiveAnimals());
        }

        [Test]
        public void LoadedGrass_SpawnsAllSpeciesAtGroundHeight()
        {
            LoadGround(BlockType.Grass);
            Call("RefreshPopulation");
            Assert.AreEqual(15, ActiveAnimals());
            foreach (var animal in _population.GetComponentsInChildren<AnimalController>(true))
            {
                Assert.AreEqual(71.1f, animal.transform.position.y, 0.001f);
                Assert.LessOrEqual(Vector2.Distance(
                    new Vector2(animal.transform.position.x, animal.transform.position.z), Vector2.zero), 36f);
            }
        }

        [Test]
        public void MovingFarAway_ReusesExistingAnimalObjects()
        {
            LoadGround(BlockType.Grass);
            Call("RefreshPopulation");
            var before = _population.GetComponentsInChildren<AnimalController>(true);
            _player.transform.position = new Vector3(10000f, 72f, 10000f);
            Call("RefreshPopulation");
            Assert.AreEqual(0, ActiveAnimals());
            CollectionAssert.AreEqual(before, _population.GetComponentsInChildren<AnimalController>(true));
        }

        private void LoadGround(BlockType block)
        {
            var data = new ChunkData();
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                data.SetBlock(x, 70, z, block);
            MeshData geometry = ChunkMesher.BuildMesh(data, null);
            _groundMesh = new Mesh { vertices = geometry.Vertices, triangles = geometry.Triangles };
            for (int x = -3; x <= 3; x++)
            for (int z = -3; z <= 3; z++)
            {
                var coord = new Vector2Int(x, z);
                var go = new GameObject("Animal spawn terrain");
                go.transform.SetParent(_world.transform);
                go.transform.position = new Vector3(x * GameConstants.ChunkWidth, 0f, z * GameConstants.ChunkDepth);
                var view = go.AddComponent<ChunkView>();
                go.GetComponent<MeshCollider>().sharedMesh = _groundMesh;
                Field<Dictionary<Vector2Int, ChunkData>>("_chunkDataCache")[coord] = data;
                Field<Dictionary<Vector2Int, ChunkView>>("_activeViews")[coord] = view;
            }
        }

        private int ActiveAnimals()
        {
            int count = 0;
            foreach (var animal in _population.GetComponentsInChildren<AnimalController>(true))
                if (animal.gameObject.activeSelf) count++;
            return count;
        }

        private T Field<T>(string name) => (T)typeof(ChunkManager)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);

        private void Set(string name, object value) => typeof(AnimalSpawner)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_spawner, value);

        private void Call(string name) => typeof(AnimalSpawner)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_spawner, null);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeenCraft.Player;
using DeenCraft.UI;
using DeenCraft.Villager;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeenCraft.Tests.PlayMode
{
    public class VillageGameplayTests
    {
        private GameObject _root;
        private GameObject _player;
        private GameObject _camera;
        private VillagerAI _villager;
        private PlayerController _controller;
        private VillagerTradePanel _menu;
        private Mesh _mesh;
        private int _previousCaptureFramerate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = 60;
            _root = new GameObject("Village gameplay tests");
            _player = Child("Player");
            _player.SetActive(false);
            Child("PlayerBody").transform.SetParent(_player.transform, false);
            _player.AddComponent<CharacterController>().height = 2f;
            _controller = _player.AddComponent<PlayerController>();
            _controller.enabled = false;
            _camera = Child("Camera");
            _camera.AddComponent<Camera>();
            Set(_controller, "_cameraTarget", _camera.transform);
            _player.AddComponent<InventoryHolder>();
            var interactor = _player.AddComponent<BlockInteractor>();
            Set(interactor, "_playerCamera", _camera.GetComponent<Camera>());
            _player.SetActive(true);
            interactor.enabled = false;
            var villager = Child("Villager");
            _villager = villager.AddComponent<VillagerAI>();
            _villager.SetProfile(VillagerProfile.CreateDefault());
            _villager.enabled = false;
            villager.AddComponent<BoxCollider>().size = new Vector3(0.7f, 2f, 0.7f);
            _menu = Child("UI").AddComponent<VillagerTradePanel>();
            Set(_menu, "_font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = _previousCaptureFramerate;
            Object.Destroy(_root);
            if (_mesh != null) Object.Destroy(_mesh);
            yield return null;
        }

        [Test]
        public void TradeMenu_OpensFromVillagerExecutesTradeAndRestoresBlockedState()
        {
            _player.GetComponent<InventoryHolder>().Inventory.AddItem(new ItemStack(ItemId.Wood, 4));
            _controller.InputBlocked = false;
            _villager.Interact(_player);
            Assert.IsTrue(_menu.IsOpen);
            Assert.IsTrue(_controller.InputBlocked);
            _menu.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.AreEqual(0, _player.GetComponent<InventoryHolder>().Inventory.CountItem(ItemId.Wood));
            Assert.AreEqual(1, _player.GetComponent<InventoryHolder>().Inventory.CountItem(ItemId.Gold));
            Assert.IsFalse(_menu.TryTrade(0));
            Assert.AreEqual(1, _player.GetComponent<InventoryHolder>().Inventory.CountItem(ItemId.Gold));
            _menu.Close();
            Assert.IsFalse(_controller.InputBlocked);
            Assert.IsFalse(_menu.IsOpen);
            PropertyInfo inputAllowed = typeof(PlayerController).GetProperty("CanProcessGameplayInput");
            Assert.IsNotNull(inputAllowed, "Closing a menu must suppress gameplay for the closing click's frame.");
            Assert.IsFalse((bool)inputAllowed.GetValue(_controller));
            _controller.InputBlocked = true;
            _villager.Interact(_player);
            _menu.Close();
            Assert.IsTrue(_controller.InputBlocked);
        }

        [Test]
        public void ThirdPersonInteraction_IgnoresOwnBodyButNotWalls()
        {
            _player.transform.position = new Vector3(0f, 1f, 0f);
            _camera.transform.position = new Vector3(0f, 1f, -5f);
            _camera.transform.rotation = Quaternion.identity;
            _villager.transform.position = new Vector3(0f, 1f, 3f);
            _player.GetComponent<CharacterController>().enabled = true;
            _player.AddComponent<BoxCollider>().size = new Vector3(0.6f, 2f, 0.6f);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(_camera.transform.position, Vector3.forward, out RaycastHit first, 10f));
            Assert.AreEqual(_player, first.collider.gameObject, "Test ray must first encounter the player's own body.");
            Invoke(_player.GetComponent<BlockInteractor>(), "TryInteract");
            Assert.IsTrue(_menu.IsOpen, "Third-person camera must interact past the player's own collider.");
            _menu.Close();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(0f, 1f, 1.5f);
            Physics.SyncTransforms();
            Invoke(_player.GetComponent<BlockInteractor>(), "TryInteract");
            Assert.IsFalse(_menu.IsOpen, "Villagers must not be reachable through a wall.");
        }

        [UnityTest]
        public IEnumerator ActualController_FitsThroughOneBlockDoorAndFurnishedCentreAisle()
        {
            var data = new ChunkData();
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
            for (int z = 0; z < GameConstants.ChunkDepth; z++)
                data.SetBlock(x, 10, z, BlockType.Grass);
            typeof(FeatureGenerator).GetMethod("PlaceHouse", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { data, GameConstants.HouseAnchorX, 10, GameConstants.HouseAnchorZ });
            var terrain = Child("Terrain").AddComponent<ChunkView>();
            terrain.ApplyMesh(ChunkMesher.BuildMesh(data, null));
            _mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            var village = Child("Furniture");
            village.SetActive(false);
            var spawner = village.AddComponent<VillageSpawner>();
            Invoke(spawner, "CreatePool");
            Transform house = village.transform.GetChild(0);
            house.position = new Vector3(8.5f, 11f, 4.5f);
            house.gameObject.SetActive(true);
            spawner.enabled = false;
            village.SetActive(true);
            _villager.gameObject.SetActive(false);
            _player.transform.position = new Vector3(8.5f, 12.05f, 3.2f);
            Physics.SyncTransforms();
            var cc = _player.GetComponent<CharacterController>();
            for (int i = 0; i < 80; i++)
            {
                cc.Move(new Vector3(0f, -0.02f, 0.05f));
                yield return null;
            }
            Assert.Greater(_player.transform.position.z, 6.8f,
                "The production-size character must fit through the doorway and walk onto the rug.");
            Assert.AreEqual(12f, _player.transform.position.y, 0.15f);
            Set(_controller, "_yaw", 90f);
            Invoke(_controller, "LateUpdate");
            Assert.Greater(_camera.transform.position.x, 5f,
                "The third-person camera must stay inside the house instead of clipping through its side wall.");
            _player.transform.position = new Vector3(8.5f, 13.95f, 6.5f);
            Set(_controller, "_pitch", 60f);
            Physics.SyncTransforms();
            Invoke(_controller, "LateUpdate");
            Vector3Int cameraBlock = Vector3Int.FloorToInt(_camera.transform.position);
            Assert.AreEqual(BlockType.Air, data.GetBlock(cameraBlock.x, cameraBlock.y, cameraBlock.z),
                "When flying against the ceiling, the camera's cast origin must remain inside the controller, not inside the roof.");
            Assert.Less(_camera.transform.position.y, 15f,
                "The camera must stay beneath the ceiling rather than bypass it from an overlapping cast origin.");
        }

        [Test]
        public void VillagePool_ReusesHouseAndVillagerOnlyWhileItsChunkIsReady()
        {
            var world = Child("Streaming world");
            world.SetActive(false);
            var manager = world.AddComponent<ChunkManager>();
            manager.enabled = false;
            world.SetActive(true);
            var data = new ChunkData();
            typeof(FeatureGenerator).GetMethod("PlaceHouse", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { data, 4, 10, 4 });
            var terrain = Child("Loaded house").AddComponent<ChunkView>();
            terrain.ApplyMesh(ChunkMesher.BuildMesh(data, null));
            _mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            var cache = Get<Dictionary<Vector2Int, ChunkData>>(manager, "_chunkDataCache");
            var active = Get<Dictionary<Vector2Int, ChunkView>>(manager, "_activeViews");
            cache[Vector2Int.zero] = data;
            active[Vector2Int.zero] = terrain;
            var pool = Child("Village pool");
            pool.SetActive(false);
            var spawner = pool.AddComponent<VillageSpawner>();
            Set(spawner, "_playerTransform", _player.transform);
            Set(spawner, "_chunkManager", manager);
            Invoke(spawner, "CreatePool");
            Invoke(spawner, "RefreshPopulation");
            var house = pool.transform.GetChild(0);
            Assert.IsTrue(house.gameObject.activeSelf);
            var villager = house.GetComponentInChildren<VillagerAI>(true);
            Assert.AreEqual(new Vector3(9.5f, 11f, 3.5f), villager.transform.position,
                "The villager belongs beside the front doorway on its cleared landing.");
            active.Clear();
            Invoke(spawner, "RefreshPopulation");
            Assert.IsFalse(house.gameObject.activeSelf);
            active[Vector2Int.zero] = terrain;
            Invoke(spawner, "RefreshPopulation");
            Assert.IsTrue(house.gameObject.activeSelf);
            Assert.AreSame(villager, house.GetComponentInChildren<VillagerAI>(true));
            Assert.AreEqual(12, pool.transform.childCount);
            var regenerated = new ChunkData();
            typeof(FeatureGenerator).GetMethod("PlaceHouse", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { regenerated, 4, 12, 4 });
            cache[Vector2Int.zero] = regenerated;
            Invoke(spawner, "RefreshPopulation");
            Assert.AreEqual(13f, house.position.y,
                "Replacing world data at the same coordinate must move the pooled house to its new floor.");
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform, false);
            return child;
        }
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        private static T Get<T>(object target, string field) => (T)target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}

using System;
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
    public class MasjidProtectionGameplayTests
    {
        [UnityTest]
        public IEnumerator MiningAndPlacement_OnRealLandmarkCollider_DoNotChangeInventoryOrTerrain()
        {
            var root = new GameObject("Masjid mining test");
            Mesh mesh = null;
            Action<GameObject, string> listener = null;
            try
            {
                var world = new GameObject("World");
                world.transform.SetParent(root.transform);
                world.SetActive(false);
                ChunkManager manager = world.AddComponent<ChunkManager>();
                Set(manager, "_poolSize", 1);
                Set(manager, "_viewDistance", 0);
                manager.enabled = false;
                world.SetActive(true);
                MasjidDistrictLayout district = manager.DistrictLayout;
                Assert.That(district, Is.Not.Null);
                MasjidComplexLayout complex = district.Complexes[1];
                Vector3Int target = new Vector3Int(complex.Bounds.xMin + 8,
                    complex.FloorY + 2, complex.Bounds.zMin + 6);
                Vector2Int coord = ChunkManager.WorldToChunkCoord(target);
                var data = new ChunkData();
                WorldGenerator.Generate(data, coord.x, coord.y, 42, district);
                var cache = (Dictionary<Vector2Int, ChunkData>)typeof(ChunkManager)
                    .GetField("_chunkDataCache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
                cache[coord] = data;
                var chunk = new GameObject("Landmark collision");
                chunk.transform.SetParent(root.transform);
                chunk.transform.position = new Vector3(coord.x * 16, 0f, coord.y * 16);
                ChunkView view = chunk.AddComponent<ChunkView>();
                view.ApplyMesh(ChunkMesher.BuildMesh(data, null));
                mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform);
                player.SetActive(false);
                new GameObject("PlayerBody").transform.SetParent(player.transform);
                player.AddComponent<CharacterController>().height = 2f;
                PlayerController controller = player.AddComponent<PlayerController>();
                controller.enabled = false;
                Camera camera = new GameObject("Camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform);
                Set(controller, "_cameraTarget", camera.transform);
                var holder = player.AddComponent<InventoryHolder>();
                BlockInteractor interactor = player.AddComponent<BlockInteractor>();
                interactor.enabled = false;
                Set(interactor, "_playerCamera", camera);
                player.transform.position = target + Vector3.back * 2f + new Vector3(0.5f, 0.1f, 0f);
                camera.transform.position = target + Vector3.back * 4f + new Vector3(0.5f, 0.1f, 0f);
                camera.transform.rotation = Quaternion.identity;
                player.SetActive(true);
                holder.Inventory.AddItem(new ItemStack(ItemId.Wood, 3));
                int notices = 0;
                listener = (sender, message) => { if (sender == player && message.Contains("protected")) notices++; };
                BlockInteractor.BlockEditDenied += listener;
                Physics.SyncTransforms();
                yield return null;
                BlockType original = manager.GetBlock(target.x, target.y, target.z);
                Assert.That(original, Is.Not.EqualTo(BlockType.Air));
                Invoke(interactor, "TryMine");
                Invoke(interactor, "TryPlace");
                Assert.That(manager.GetBlock(target.x, target.y, target.z), Is.EqualTo(original));
                Assert.That(holder.Inventory.CountItem(ItemId.Wood), Is.EqualTo(3));
                Assert.That(holder.Inventory.CountItem(ItemId.MudBrick), Is.EqualTo(0));
                Assert.That(notices, Is.EqualTo(2), "Both actions must visibly explain protection.");
            }
            finally
            {
                if (listener != null) BlockInteractor.BlockEditDenied -= listener;
                UnityEngine.Object.Destroy(root);
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
            }
            yield return null;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}

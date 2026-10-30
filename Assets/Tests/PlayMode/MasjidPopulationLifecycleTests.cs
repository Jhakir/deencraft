using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeenCraft.Villager;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeenCraft.Tests.PlayMode
{
    public class MasjidPopulationLifecycleTests
    {
        [UnityTest]
        public IEnumerator Population_ReusesSameContentAfterSupportingChunkUnloadAndWorldReset()
        {
            var root = new GameObject("Masjid streaming test");
            try
            {
                var worldObject = new GameObject("World");
                worldObject.transform.SetParent(root.transform);
                worldObject.SetActive(false);
                ChunkManager world = worldObject.AddComponent<ChunkManager>();
                Set(world, "_poolSize", 1);
                Set(world, "_viewDistance", 0);
                world.enabled = false;
                worldObject.SetActive(true);
                MasjidComplexLayout complex = world.DistrictLayout.Complexes[0];
                Vector2Int first = AddSupportChunks(root.transform, world, complex);
                var populationObject = new GameObject("Population");
                populationObject.transform.SetParent(root.transform);
                populationObject.SetActive(false);
                MasjidDistrictSpawner population = populationObject.AddComponent<MasjidDistrictSpawner>();
                Set(population, "_playerTransform", root.transform);
                Set(population, "_chunkManager", world);
                populationObject.SetActive(true);
                yield return null;
                Transform content = population.transform.GetChild(0);
                int instance = content.GetInstanceID();
                Assert.That(content.gameObject.activeSelf, Is.True);
                Invoke(world, "UnloadChunk", first);
                Assert.That(content.gameObject.activeSelf, Is.False,
                    "Furniture must disappear before its supporting collision is removed.");
                world.SetSeed(42);
                Assert.That(content.gameObject.activeSelf, Is.False);
                AddSupportChunks(root.transform, world, world.DistrictLayout.Complexes[0]);
                Invoke(population, "RefreshPopulation");
                Assert.That(content.gameObject.activeSelf, Is.True);
                Assert.That(content.GetInstanceID(), Is.EqualTo(instance));
                Assert.That(population.transform.childCount, Is.EqualTo(4));
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        private static Vector2Int AddSupportChunks(Transform root, ChunkManager world, MasjidComplexLayout complex)
        {
            var cache = (Dictionary<Vector2Int, ChunkData>)Field(world, "_chunkDataCache").GetValue(world);
            var active = (Dictionary<Vector2Int, ChunkView>)Field(world, "_activeViews").GetValue(world);
            Vector2Int min = ChunkManager.WorldToChunkCoord(complex.Bounds.min);
            Vector2Int max = ChunkManager.WorldToChunkCoord(complex.Bounds.max - Vector3Int.one);
            for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                var coord = new Vector2Int(x, z);
                var data = new ChunkData();
                data.SetBlock(0, complex.FloorY, 0, BlockType.Stone);
                cache[coord] = data;
                var go = new GameObject("Supporting collision");
                go.transform.SetParent(root);
                var view = go.AddComponent<ChunkView>();
                view.ApplyMesh(ChunkMesher.BuildMesh(data, null));
                active[coord] = view;
            }
            return min;
        }

        private static FieldInfo Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
        private static void Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}

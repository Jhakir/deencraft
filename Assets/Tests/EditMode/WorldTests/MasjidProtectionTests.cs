using System;
using System.Collections.Generic;
using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MasjidProtectionTests
    {
        private GameObject _root;
        private ChunkManager _manager;
        private MasjidDistrictLayout _layout;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("District protection tests");
            _root.SetActive(false);
            _manager = _root.AddComponent<ChunkManager>();
            Field("_poolSize").SetValue(_manager, 1);
            Field("_viewDistance").SetValue(_manager, 0);
            _root.SetActive(true);
            _manager.SetSeed(42);
            _layout = MasjidDistrictPlanner.Create(42, b => false);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

        private static FieldInfo Field(string name) =>
            typeof(ChunkManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private string Edit(Vector3Int p, BlockType block)
        {
            MethodInfo method = typeof(ChunkManager).GetMethod("TrySetBlock");
            Assert.That(method, Is.Not.Null, "Edits must report success before inventory changes.");
            return method.Invoke(_manager, new object[] { p, block }).ToString();
        }

        private void Cache(Vector3Int p)
        {
            var data = new ChunkData();
            Vector2Int coord = ChunkManager.WorldToChunkCoord(p);
            ((Dictionary<Vector2Int, ChunkData>)Field("_chunkDataCache").GetValue(_manager))[coord] = data;
        }

        [Test]
        public void Mutation_RejectsInteriorAirFoundationAndRoofWithoutChangingBlocks()
        {
            foreach (MasjidComplexLayout complex in _layout.Complexes)
            {
                var points = new[] { complex.Bounds.min,
                    complex.Bounds.max - Vector3Int.one,
                    complex.HallClearance.min, complex.EntranceClearance.min };
                foreach (Vector3Int p in points)
                {
                    Cache(p);
                    Assert.That(Edit(p, BlockType.Wood), Is.EqualTo("Protected"), p.ToString());
                    Assert.That(_manager.GetBlock(p.x, p.y, p.z), Is.EqualTo(BlockType.Air));
                }
            }
        }

        [Test]
        public void Mutation_AllowsGardenAndOrdinaryTerrainButReportsUnavailableTargets()
        {
            var garden = new Vector3Int(_layout.Origin.x + 40, _layout.FloorY, _layout.Origin.y + 40);
            Cache(garden);
            Assert.That(Edit(garden, BlockType.Stone), Is.EqualTo("Success"));
            Assert.That(_manager.GetBlock(garden.x, garden.y, garden.z), Is.EqualTo(BlockType.Stone));
            Assert.That(Edit(new Vector3Int(-10000, 50, -10000), BlockType.Wood), Is.EqualTo("ChunkUnavailable"));
            Assert.That(Edit(new Vector3Int(0, -1, 0), BlockType.Wood), Is.EqualTo("OutOfBounds"));
        }

        [Test]
        public void WorldReset_IncrementsRevisionEvenForSameSeed()
        {
            PropertyInfo revision = typeof(ChunkManager).GetProperty("WorldRevision");
            Assert.That(revision, Is.Not.Null, "Same-coordinate population must detect world replacement.");
            int before = (int)revision.GetValue(_manager);
            _manager.SetSeed(42);
            Assert.That((int)revision.GetValue(_manager), Is.GreaterThan(before));
        }
    }
}

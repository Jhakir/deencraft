using System;
using System.Collections.Generic;
using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MasjidStructureTests
    {
        private readonly Dictionary<Vector2Int, ChunkData> _chunks = new Dictionary<Vector2Int, ChunkData>();
        private MasjidDistrictLayout _layout;

        [SetUp]
        public void SetUp()
        {
            _chunks.Clear();
            _layout = MasjidDistrictPlanner.Create(42, bounds => false);
        }

        private ChunkData Generate(Vector2Int coord)
        {
            MethodInfo method = typeof(WorldGenerator).GetMethod("Generate",
                new[] { typeof(ChunkData), typeof(int), typeof(int), typeof(int), typeof(MasjidDistrictLayout) });
            Assert.That(method, Is.Not.Null, "Cross-chunk district-aware generation is required.");
            var data = new ChunkData();
            method.Invoke(null, new object[] { data, coord.x, coord.y, 42, _layout });
            return data;
        }

        private BlockType Sample(Vector3Int p)
        {
            Vector2Int coord = ChunkManager.WorldToChunkCoord(p);
            if (!_chunks.TryGetValue(coord, out ChunkData data))
            {
                data = Generate(coord);
                _chunks.Add(coord, data);
            }
            return data.GetBlock(p.x - coord.x * 16, p.y, p.z - coord.y * 16);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Complex_HasClearEntranceHallAndClassroom(int index)
        {
            MasjidComplexLayout complex = _layout.Complexes[index];
            var clearances = new List<BoundsInt> { complex.EntranceClearance,
                complex.HallClearance, complex.LearningRoomClearance };
            clearances.AddRange(complex.CirculationClearances);
            foreach (BoundsInt clearance in clearances)
            foreach (Vector3Int p in clearance.allPositionsWithin)
                Assert.That(Sample(p), Is.EqualTo(BlockType.Air), $"{complex.Style} clearance {p}");
            foreach (BoundsInt area in clearances)
            for (int x = area.xMin; x < area.xMax; x++)
            for (int z = area.zMin; z < area.zMax; z++)
                Assert.That(Sample(new Vector3Int(x, complex.FloorY, z)), Is.Not.EqualTo(BlockType.Air));
        }

        [Test]
        public void District_ClearsOldFeaturesAndRecordsNoHouseAssignments()
        {
            foreach (MasjidComplexLayout complex in _layout.Complexes)
            {
                var coord = ChunkManager.WorldToChunkCoord(complex.Bounds.min);
                Assert.That(Generate(coord).HouseFloorY, Is.Null);
                int x = complex.Bounds.xMin + 4;
                int z = complex.Bounds.zMin + 6;
                Assert.That(Sample(new Vector3Int(x, _layout.FloorY + 1, z)), Is.Not.EqualTo(BlockType.Air));
            }
        }

        [Test]
        public void Generation_OrderDoesNotChangeCrossChunkBuilding()
        {
            Vector2Int a = ChunkManager.WorldToChunkCoord(_layout.Complexes[0].Bounds.min);
            Vector2Int b = a + Vector2Int.right;
            ChunkData first = Generate(a);
            Generate(b);
            ChunkData second = Generate(a);
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
            for (int y = _layout.FloorY; y < _layout.FloorY + 24; y++)
                Assert.That(first.GetBlock(x, y, z), Is.EqualTo(second.GetBlock(x, y, z)));
        }

        [Test]
        public void District_HasFourDifferentRoofSilhouettes()
        {
            var shapes = new HashSet<string>();
            foreach (MasjidComplexLayout complex in _layout.Complexes)
            {
                var columns = new List<int>();
                for (int x = 4; x < 28; x++)
                {
                    int height = 0;
                    for (int y = complex.FloorY + 23; y > complex.FloorY; y--)
                        if (Sample(new Vector3Int(complex.Bounds.xMin + x, y,
                            complex.Bounds.zMin + 21)) != BlockType.Air) { height = y; break; }
                    columns.Add(height);
                }
                Assert.That(shapes.Add(string.Join(",", columns)), Is.True,
                    complex.Style + " needs a distinct roof silhouette, not just another color.");
            }
        }

        [Test]
        public void EveryEntrance_HasContinuousPavedWalkableRouteToSharedGarden()
        {
            var destination = new Vector2Int(_layout.Origin.x + 40, _layout.Origin.y + 34);
            foreach (MasjidComplexLayout complex in _layout.Complexes)
            {
                var start = new Vector2Int(complex.Bounds.xMin + 15, complex.Bounds.zMin + 4);
                var queue = new Queue<Vector2Int>();
                var visited = new HashSet<Vector2Int> { start };
                queue.Enqueue(start);
                bool reached = false;
                while (queue.Count > 0)
                {
                    Vector2Int p = queue.Dequeue();
                    if (p == destination) { reached = true; break; }
                    foreach (Vector2Int direction in new[] { Vector2Int.left, Vector2Int.right,
                        Vector2Int.up, Vector2Int.down })
                    {
                        Vector2Int next = p + direction;
                        if (next.x < _layout.Bounds.xMin || next.x >= _layout.Bounds.xMax ||
                            next.y < _layout.Bounds.zMin || next.y >= _layout.Bounds.zMax ||
                            !visited.Add(next)) continue;
                        BlockType floor = Sample(new Vector3Int(next.x, _layout.FloorY, next.y));
                        if (floor != BlockType.Mosque && floor != BlockType.StoneArch) continue;
                        if (Sample(new Vector3Int(next.x, _layout.FloorY + 1, next.y)) != BlockType.Air ||
                            Sample(new Vector3Int(next.x, _layout.FloorY + 2, next.y)) != BlockType.Air) continue;
                        queue.Enqueue(next);
                    }
                }
                Assert.That(reached, Is.True, complex.Style + " entrance must connect to the garden on paving, not through walls.");
            }
        }
    }
}

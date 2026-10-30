using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MasjidDistrictLayoutTests
    {
        private static object Create(int seed, Func<BoundsInt, bool> conflicts)
        {
            Type planner = typeof(WorldGenerator).Assembly.GetType("DeenCraft.World.MasjidDistrictPlanner");
            Assert.That(planner, Is.Not.Null, "A deterministic district planner is required.");
            MethodInfo method = planner.GetMethod("Create", new[] { typeof(int), typeof(Func<BoundsInt, bool>) });
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new object[] { seed, conflicts });
        }

        private static T Read<T>(object instance, string name)
        {
            PropertyInfo property = instance.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return (T)property.GetValue(instance);
        }

        private static object[] Complexes(object layout) =>
            ((IEnumerable)Read<object>(layout, "Complexes")).Cast<object>().ToArray();

        [Test]
        public void Create_ContainsAllFourDistinctStyles()
        {
            object[] complexes = Complexes(Create(42, bounds => false));
            Assert.That(complexes.Length, Is.EqualTo(4));
            Assert.That(complexes.Select(c => Read<object>(c, "Style").ToString()),
                Is.EquivalentTo(new[] { "Saudi", "Iraqi", "Pakistani", "Turkish" }));
        }

        [Test]
        public void Create_PlotsAreDisjointAndAtLeastThirtyTwoBlocksWide()
        {
            object[] complexes = Complexes(Create(42, bounds => false));
            for (int i = 0; i < complexes.Length; i++)
            {
                BoundsInt a = Read<BoundsInt>(complexes[i], "Bounds");
                Assert.That(a.size.x, Is.EqualTo(32));
                Assert.That(a.size.z, Is.EqualTo(32));
                for (int j = i + 1; j < complexes.Length; j++)
                {
                    BoundsInt b = Read<BoundsInt>(complexes[j], "Bounds");
                    Assert.That(a.xMax <= b.xMin || b.xMax <= a.xMin ||
                        a.zMax <= b.zMin || b.zMax <= a.zMin, Is.True);
                }
            }
        }

        [TestCase(42)]
        [TestCase(-2147483648)]
        [TestCase(2147483647)]
        public void Create_IsDeterministicAndNearInitialSpawn(int seed)
        {
            object first = Create(seed, bounds => false);
            object second = Create(seed, bounds => false);
            Assert.That(Read<Vector2Int>(first, "Origin"), Is.EqualTo(Read<Vector2Int>(second, "Origin")));
            Assert.That(Read<int>(first, "FloorY"), Is.EqualTo(Read<int>(second, "FloorY")));
            Assert.That(Read<float>(first, "DistanceFromInitialSpawn"), Is.LessThanOrEqualTo(128f));
            Assert.That(Read<int>(first, "FloorY"), Is.InRange(1, 230));
        }

        [Test]
        public void Create_RejectsConflictingFirstCandidateWithoutWritingTerrain()
        {
            object layout = Create(42, bounds => bounds.xMin >= 0);
            Assert.That(Read<Vector2Int>(layout, "Origin"), Is.EqualTo(new Vector2Int(-112, -40)));
        }

        [Test]
        public void Create_AllCandidatesConflict_FailsExplicitly()
        {
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(
                () => Create(42, bounds => true));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(error.InnerException.Message, Does.Contain("district"));
        }

        [Test]
        public void Create_NullConflictQuery_IsRejected()
        {
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(() => Create(42, null));
            Assert.That(error.InnerException, Is.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Create_DeclaresSpaciousHallAndEntranceClearances()
        {
            foreach (object complex in Complexes(Create(42, bounds => false)))
            {
                BoundsInt door = Read<BoundsInt>(complex, "EntranceClearance");
                BoundsInt hall = Read<BoundsInt>(complex, "HallClearance");
                BoundsInt plot = Read<BoundsInt>(complex, "Bounds");
                Assert.That(door.size.x, Is.GreaterThanOrEqualTo(3));
                Assert.That(door.size.y, Is.GreaterThanOrEqualTo(4));
                Assert.That(hall.size.x, Is.GreaterThanOrEqualTo(7));
                Assert.That(hall.size.z, Is.GreaterThanOrEqualTo(9));
                Assert.That(plot.Contains(hall.min) && plot.Contains(hall.max - Vector3Int.one), Is.True);
            }
        }
    }
}

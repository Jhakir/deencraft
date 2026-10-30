using System;
using System.Reflection;
using DeenCraft.Auth.Models;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MasjidPersistenceTests
    {
        [Test]
        public void WorldMetadata_PreservesDistrictSelectionThroughJson()
        {
            const string json = "{\"masjidDistrictVersion\":1,\"masjidDistrictOriginX\":-112," +
                "\"masjidDistrictOriginZ\":-40,\"masjidDistrictFloorY\":90}";
            var save = JsonUtility.FromJson<WorldSaveData>(json);
            string encoded = JsonUtility.ToJson(save);
            Assert.That(encoded, Does.Contain("\"masjidDistrictVersion\":1"));
            Assert.That(encoded, Does.Contain("\"masjidDistrictOriginX\":-112"));
            Assert.That(encoded, Does.Contain("\"masjidDistrictOriginZ\":-40"));
            Assert.That(encoded, Does.Contain("\"masjidDistrictFloorY\":90"));
        }

        [Test]
        public void Restore_RejectsUnsupportedVersionAndInvalidOrigin()
        {
            MethodInfo restore = typeof(MasjidDistrictPlanner).GetMethod("Restore");
            Assert.That(restore, Is.Not.Null);
            var unsupported = Assert.Throws<TargetInvocationException>(() =>
                restore.Invoke(null, new object[] { 42, 999, new Vector2Int(32, -40), 90 }));
            Assert.That(unsupported.InnerException, Is.TypeOf<ArgumentException>());
            var invalid = Assert.Throws<TargetInvocationException>(() =>
                restore.Invoke(null, new object[] { 42, 1, new Vector2Int(0, 0), 90 }));
            Assert.That(invalid.InnerException, Is.TypeOf<ArgumentException>());
        }

        [Test]
        public void Restore_RetainsExactSelectedOriginAndFloor()
        {
            var selected = MasjidDistrictPlanner.Create(42, b => b.xMin >= 0);
            MethodInfo restore = typeof(MasjidDistrictPlanner).GetMethod("Restore");
            Assert.That(restore, Is.Not.Null);
            var result = (MasjidDistrictLayout)restore.Invoke(null,
                new object[] { 42, selected.Version, selected.Origin, selected.FloorY });
            Assert.That(result.Origin, Is.EqualTo(selected.Origin));
            Assert.That(result.FloorY, Is.EqualTo(selected.FloorY));
        }
    }
}

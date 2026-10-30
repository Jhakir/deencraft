using System;
using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MasjidApproachTests
    {
        [Test]
        public void Approach_HasContinuousWalkableSurfaceFromStartingAreaToDistrict()
        {
            Type path = typeof(WorldGenerator).Assembly.GetType("DeenCraft.World.MasjidApproachPath");
            Assert.That(path, Is.Not.Null, "A walkable approach must connect the raised district to the starting area.");
            var district = MasjidDistrictPlanner.Create(42, b => false);
            MethodInfo build = path.GetMethod("BuildMesh", BindingFlags.Public | BindingFlags.Static);
            Assert.That(build, Is.Not.Null);
            var mesh = (Mesh)build.Invoke(null, new object[] { district });
            try
            {
                Assert.That(mesh.vertexCount, Is.GreaterThanOrEqualTo(4));
                Assert.That(mesh.triangles.Length, Is.GreaterThanOrEqualTo(6));
                foreach (Vector3 normal in mesh.normals) Assert.That(normal.y, Is.GreaterThan(0.7f));
                Vector3 start = mesh.vertices[0];
                Vector3 end = mesh.vertices[2];
                Assert.That(Vector2.Distance(new Vector2(start.x, start.z), Vector2.zero), Is.LessThan(10f));
                Assert.That(end.y, Is.EqualTo(district.FloorY + 1.03f).Within(0.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DeenCraft.Villager;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeenCraft.Tests.EditMode
{
    public class MasjidPopulationTests
    {
        [Test]
        public void Population_ProvidesUnityFrameMeasurementForBrowserValidation()
        {
            Type type = typeof(VillageSpawner).Assembly.GetType("DeenCraft.Villager.MasjidDistrictSpawner");
            var root = new GameObject("Frame sample test");
            try
            {
                var world = root.AddComponent<ChunkManager>();
                world.SetSeed(42);
                var actor = new GameObject("Player");
                actor.transform.SetParent(root.transform);
                actor.transform.position = world.DistrictLayout.Complexes[0].HallClearance.min;
                var spawner = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo seconds = type.GetField("_sampleSeconds", flags);
                Assert.That(seconds, Is.Not.Null,
                    "Performance must be measured from Unity frames, not inferred from browser RAF callbacks.");
                seconds.SetValue(spawner, 60f);
                type.GetField("_sampleFrames", flags).SetValue(spawner, 3600);
                type.GetField("_chunkManager", flags).SetValue(spawner, world);
                type.GetField("_playerTransform", flags).SetValue(spawner, actor.transform);
                LogAssert.Expect(LogType.Log, new Regex(@"\[MasjidDistrict\] Performance: 60\.\d FPS"));
                type.GetMethod("LateUpdate", flags).Invoke(spawner, null);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void Pool_HasFourFurnishedComplexesWithTeachersBoardsAndBoundedLights()
        {
            Type type = typeof(VillageSpawner).Assembly.GetType("DeenCraft.Villager.MasjidDistrictSpawner");
            Assert.That(type, Is.Not.Null, "Masjids need pooled exploration content.");
            var root = new GameObject("Masjid pool tests");
            root.SetActive(false);
            try
            {
                var spawner = root.AddComponent(type);
                type.GetMethod("CreatePool", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
                Assert.That(root.transform.childCount, Is.EqualTo(4));
                foreach (Transform complex in root.transform)
                {
                    Assert.That(complex.gameObject.activeSelf, Is.False);
                    var names = complex.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
                    foreach (string required in new[] { "Teacher", "Architecture board", "Community board",
                        "Prayer rugs", "Bookshelf", "Learning table", "Washing area" })
                        Assert.That(names, Does.Contain(required), complex.name);
                    Light[] lights = complex.GetComponentsInChildren<Light>(true);
                    Assert.That(lights.Length, Is.InRange(1, 2));
                    Assert.That(lights.All(l => l.shadows == LightShadows.None), Is.True);
                    Renderer head = complex.Find("Teacher/Head").GetComponent<Renderer>();
                    Assert.That(head.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,
                        Is.GreaterThanOrEqualTo(0.05f), "Teachers must remain visible when both room lights are behind them.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}

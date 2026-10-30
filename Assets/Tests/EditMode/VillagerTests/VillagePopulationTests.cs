using System;
using System.Reflection;
using DeenCraft.Villager;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeenCraft.Tests.EditMode
{
    public class VillagePopulationTests
    {
        [Test]
        public void VillagePool_HasNamedVillagersAndAllFurnitureWithClearCentreAisle()
        {
            Type type = typeof(VillagerAI).Assembly.GetType("DeenCraft.Villager.VillageSpawner");
            Assert.IsNotNull(type, "Villagers and furnishings must be wired into loaded villages.");
            var root = new GameObject("Village pool test");
            root.SetActive(false);
            try
            {
                var spawner = root.AddComponent(type);
                type.GetMethod("CreatePool", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(spawner, null);
                var villagers = root.GetComponentsInChildren<VillagerAI>(true);
                Assert.AreEqual(12, villagers.Length);
                foreach (var villager in villagers)
                {
                    Assert.IsNotNull(villager.GetComponent<Collider>());
                    Assert.IsNotEmpty(villager.Profile.Name);
                    Assert.IsNotNull(villager.GetComponentInChildren<TextMesh>(true));
                }
                foreach (Transform house in root.transform)
                {
                    Light light = house.GetComponentInChildren<Light>(true);
                    Assert.IsNotNull(light, "Furnished interiors need a light to remain visible beneath their roof.");
                    Assert.AreEqual(LightType.Point, light.type);
                    Assert.AreEqual(LightShadows.None, light.shadows);
                    Assert.Greater(light.intensity, 0f);
                    Assert.GreaterOrEqual(light.range, 7f);
                    foreach (string name in new[] { "Bed", "Table", "Chair", "Chest", "Rug", "Bookshelf" })
                        Assert.IsNotNull(house.Find(name), $"House must contain {name}.");
                    foreach (var collider in house.GetComponentsInChildren<BoxCollider>(true))
                    {
                        if (collider.GetComponent<VillagerAI>() != null) continue;
                        Vector3 local = house.InverseTransformPoint(collider.transform.position);
                        Assert.Greater(Mathf.Abs(local.x), 0.6f,
                            "Furniture colliders must leave the centre aisle clear.");
                    }
                    Assert.IsFalse(house.gameObject.activeSelf);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

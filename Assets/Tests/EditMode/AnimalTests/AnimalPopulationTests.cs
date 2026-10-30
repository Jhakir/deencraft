using System;
using System.Reflection;
using DeenCraft.Animals;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeenCraft.Tests.EditMode
{
    public class AnimalPopulationTests
    {
        [Test]
        public void RecycledAnimal_ResetsFallingSpeedAndWanderState()
        {
            var root = new GameObject("Recycled animal");
            root.SetActive(false);
            try
            {
                var animal = root.AddComponent<AnimalController>();
                typeof(AnimalController).GetField("_verticalVelocity", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(animal, -100f);
                var enable = typeof(AnimalController).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(enable, "Reused animals must reset before respawning.");
                enable.Invoke(animal, null);
                Assert.AreEqual(0f, typeof(AnimalController)
                    .GetField("_verticalVelocity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animal));
                Assert.AreEqual(AnimalState.Idle, animal.Brain.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(AnimalType.Sheep)]
        [TestCase(AnimalType.Cow)]
        [TestCase(AnimalType.Chicken)]
        [TestCase(AnimalType.Cat)]
        [TestCase(AnimalType.Horse)]
        public void EverySpecies_HasColouredBlockGeometry(AnimalType species)
        {
            var mesh = BlockAnimalMesh.Build(species);
            try
            {
                Assert.Greater(mesh.vertexCount, 0);
                Assert.AreEqual(mesh.vertexCount, mesh.colors32.Length);
                Assert.Greater(mesh.bounds.size.y, 0.3f);
                Assert.Greater(mesh.bounds.size.z, 0.3f);
                Assert.GreaterOrEqual(mesh.bounds.min.y, 0f, "Animal roots must sit at their feet.");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Population_IsBoundedAndIncludesAllFiveSpecies()
        {
            Type type = typeof(AnimalSpawner);
            var root = new GameObject("Animal population test");
            root.SetActive(false);
            Material material = null;
            try
            {
                var spawner = root.AddComponent(type);
                material = new Material(Shader.Find("DeenCraft/VertexColor"));
                type.GetField("_animalMaterial", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(spawner, material);
                type.GetMethod("CreatePool", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(spawner, null);
                var animals = root.GetComponentsInChildren<AnimalController>(true);
                Assert.AreEqual(15, animals.Length);
                foreach (AnimalType species in Enum.GetValues(typeof(AnimalType)))
                    Assert.AreEqual(3, Array.FindAll(animals, a => a.AnimalType == species).Length);
                foreach (var animal in animals)
                {
                    Assert.IsFalse(animal.gameObject.activeSelf, "Wait for loaded ground before enabling animals.");
                    Assert.IsNotNull(animal.GetComponent<CharacterController>());
                    Assert.IsNotNull(animal.GetComponentInChildren<MeshFilter>());
                    var properties = new MaterialPropertyBlock();
                    animal.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
                    Assert.AreSame(Texture2D.whiteTexture, properties.GetTexture("_MainTex"),
                        "Animal coats must not inherit the terrain atlas's green tint.");
                }
            }
            finally
            {
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null) Object.DestroyImmediate(filter.sharedMesh);
                Object.DestroyImmediate(root);
                if (material != null) Object.DestroyImmediate(material);
            }
        }
    }
}

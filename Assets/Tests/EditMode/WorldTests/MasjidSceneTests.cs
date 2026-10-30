using System;
using DeenCraft.UI;
using DeenCraft.Villager;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeenCraft.Tests.EditMode
{
    public class MasjidSceneTests
    {
        [Test]
        public void SavedGameplayScene_ContainsMasjidPopulationReadingAndApproachReferences()
        {
            const string path = "Assets/Scenes/Main.slunity.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                Type population = typeof(VillageSpawner).Assembly.GetType("DeenCraft.Villager.MasjidDistrictSpawner");
                Type panel = typeof(VillagerTradePanel).Assembly.GetType("DeenCraft.UI.MasjidInformationPanel");
                Assert.That(population, Is.Not.Null);
                Assert.That(panel, Is.Not.Null);
                Component spawner = null;
                Component reading = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (spawner == null) spawner = root.GetComponentInChildren(population, true);
                    if (reading == null) reading = root.GetComponentInChildren(panel, true);
                }
                Assert.That(spawner, Is.Not.Null, "The population must persist in the gameplay scene.");
                Assert.That(reading, Is.Not.Null);
                Assert.That(new SerializedObject(reading).FindProperty("_font").objectReferenceValue, Is.Not.Null);
                var fields = new SerializedObject(spawner);
                Assert.That(fields.FindProperty("_chunkManager").objectReferenceValue, Is.Not.Null);
                Assert.That(fields.FindProperty("_playerTransform").objectReferenceValue, Is.Not.Null);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}

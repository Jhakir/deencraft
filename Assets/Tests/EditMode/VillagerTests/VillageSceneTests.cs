using DeenCraft.UI;
using DeenCraft.Villager;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DeenCraft.Tests.EditMode
{
    public class VillageSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Main.slunity.unity";

        [Test]
        public void SavedGameplayScene_HasTradeFontAndVillageReferences()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                VillagerTradePanel menu = null;
                VillageSpawner spawner = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (menu == null) menu = root.GetComponentInChildren<VillagerTradePanel>(true);
                    if (spawner == null) spawner = root.GetComponentInChildren<VillageSpawner>(true);
                }
                Assert.IsNotNull(menu);
                Assert.IsNotNull(spawner);
                Assert.IsNotNull(new SerializedObject(menu).FindProperty("_font").objectReferenceValue,
                    "The saved scene must include a real font so E can open trading in WebGL.");
                var serialized = new SerializedObject(spawner);
                Assert.IsNotNull(serialized.FindProperty("_playerTransform").objectReferenceValue);
                Assert.IsNotNull(serialized.FindProperty("_chunkManager").objectReferenceValue);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}

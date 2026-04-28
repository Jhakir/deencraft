#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeenCraft.Editor
{
    /// <summary>
    /// Adds Deencraft-specific menu items to the Unity Editor top bar.
    /// All game-specific editor utilities are gathered here for easy discovery.
    /// </summary>
    public static class DeenCraftMenuItems
    {
        // ── Build ─────────────────────────────────────────────────────────

        [MenuItem("Deencraft/Build/WebGL (Development)", priority = 10)]
        private static void BuildDev() => WebGLBuildConfig.BuildWebGLDevelopment();

        [MenuItem("Deencraft/Build/WebGL (Release)", priority = 11)]
        private static void BuildRelease() => WebGLBuildConfig.BuildWebGLRelease();

        [MenuItem("Deencraft/Build/Open Build Folder", priority = 20)]
        private static void OpenBuildFolder()
        {
            var path = Path.GetFullPath("WebGLBuild");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        // ── Firebase ──────────────────────────────────────────────────────

        [MenuItem("Deencraft/Firebase/Validate Config", priority = 30)]
        private static void ValidateFirebase() => FirebaseConfigValidator.ValidateFromMenu();

        [MenuItem("Deencraft/Firebase/Open StreamingAssets Folder", priority = 31)]
        private static void OpenStreamingAssets()
        {
            EditorUtility.RevealInFinder(Application.streamingAssetsPath);
        }

        // ── Settings ──────────────────────────────────────────────────────

        [MenuItem("Deencraft/Settings/Apply WebGL Settings", priority = 40)]
        private static void ApplySettings() => WebGLBuildConfig.ApplyWebGLSettings();

        [MenuItem("Deencraft/Settings/Open Player Settings", priority = 41)]
        private static void OpenPlayerSettings()
        {
            SettingsService.OpenProjectSettings("Project/Player");
        }

        // ── Info ──────────────────────────────────────────────────────────

        [MenuItem("Deencraft/About", priority = 100)]
        private static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "Deencraft",
                "Deencraft v0.1.0\n" +
                "Islamic-themed Minecraft-style voxel sandbox for children.\n\n" +
                "Tech stack: Unity 2022 LTS · Firebase · WebGL\n" +
                "Repo: https://github.com/Jhakir/deencraft",
                "OK");
        }
    }
}
#endif

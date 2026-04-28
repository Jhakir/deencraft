#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeenCraft.Editor
{
    /// <summary>
    /// Configures Unity PlayerSettings for WebGL and provides a one-click build pipeline.
    ///
    /// CI/CD usage (command line):
    ///   Unity -batchmode -nographics -executeMethod DeenCraft.Editor.WebGLBuildConfig.BuildFromCommandLine -quit
    /// </summary>
    public static class WebGLBuildConfig
    {
        // ── Build Output ─────────────────────────────────────────────────
        private const string BuildOutputPath   = "WebGLBuild";
        private const string ProductName       = "Deencraft";
        private const string CompanyName       = "Deencraft";
        private const string BundleIdentifier  = "com.deencraft.game";
        private const string VersionString     = "0.1.0";

        // ── WebGL Memory ─────────────────────────────────────────────────
        private const int WebGLMemoryMB        = 512;
        private const int WebGLInitialMemoryMB = 32;
        private const int WebGLMaxMemoryMB     = 2048;

        // ── Scenes ───────────────────────────────────────────────────────
        private static readonly string[] BuildScenes =
        {
            "Assets/Scenes/Loading.unity",
            "Assets/Scenes/Auth.unity",
            "Assets/Scenes/Game.unity",
        };

        // ── Apply Settings ────────────────────────────────────────────────

        [MenuItem("Deencraft/Apply WebGL Build Settings")]
        public static void ApplyWebGLSettings()
        {
            PlayerSettings.companyName            = CompanyName;
            PlayerSettings.productName            = ProductName;
            PlayerSettings.bundleVersion          = VersionString;

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.WebGL, BundleIdentifier);

            // WebGL memory configuration
            PlayerSettings.WebGL.memorySize        = WebGLMemoryMB;
            PlayerSettings.WebGL.linkerTarget      = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.dataCaching       = true;
            PlayerSettings.WebGL.debugSymbols      = false;
            PlayerSettings.WebGL.exceptionSupport  = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // Disable Unity splash screen
            PlayerSettings.SplashScreen.show      = false;

            // Color space — Linear gives better visuals for a voxel game
            PlayerSettings.colorSpace             = ColorSpace.Linear;

            // Scripting backend — IL2CPP is required for WebGL
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);

            // API compatibility — .NET Standard 2.1 for Firebase SDK
            PlayerSettings.SetApiCompatibilityLevel(
                BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_Standard_2_0);

            // Run in background (keeps game ticking when browser tab is not focused)
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[DeenCraft] WebGL settings applied successfully.");
        }

        // ── Build ─────────────────────────────────────────────────────────

        [MenuItem("Deencraft/Build WebGL (Development)")]
        public static void BuildWebGLDevelopment()
        {
            ApplyWebGLSettings();
            Build(buildDevelopment: true);
        }

        [MenuItem("Deencraft/Build WebGL (Release)")]
        public static void BuildWebGLRelease()
        {
            ApplyWebGLSettings();
            Build(buildDevelopment: false);
        }

        /// <summary>
        /// Entry point for CI/CD command-line builds.
        /// Reads BUILD_OUTPUT environment variable if set.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            ApplyWebGLSettings();

            var outputPath = Environment.GetEnvironmentVariable("BUILD_OUTPUT") ?? BuildOutputPath;
            var report     = PerformBuild(outputPath, buildDevelopment: false);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[DeenCraft] Build FAILED: {report.summary.result}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[DeenCraft] Build succeeded → {outputPath}");
            EditorApplication.Exit(0);
        }

        // ── Private ───────────────────────────────────────────────────────

        private static void Build(bool buildDevelopment)
        {
            var outputPath = buildDevelopment
                ? Path.Combine(BuildOutputPath, "dev")
                : Path.Combine(BuildOutputPath, "release");

            var report = PerformBuild(outputPath, buildDevelopment);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[DeenCraft] Build succeeded → {outputPath}");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError($"[DeenCraft] Build FAILED. Check the console for errors.");
            }
        }

        private static BuildReport PerformBuild(string outputPath, bool buildDevelopment)
        {
            var options = new BuildPlayerOptions
            {
                scenes           = BuildScenes,
                locationPathName = outputPath,
                target           = BuildTarget.WebGL,
                options          = buildDevelopment
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };
            return BuildPipeline.BuildPlayer(options);
        }
    }
}
#endif

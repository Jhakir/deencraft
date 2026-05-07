// Assets/Editor/WebGLBuildScript.cs
// Run from the command line:
//   Unity.exe -batchmode -quit -projectPath . -executeMethod WebGLBuildScript.Build
// Or from the Editor menu: Deencraft > Build WebGL
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeenCraft.Editor
{
    public static class WebGLBuildScript
    {
        private const string OutputPath = "webgl-build/Build";

        [MenuItem("Deencraft/Build WebGL")]
        public static void Build()
        {
            // ── Configure WebGL settings ──────────────────────────────────
            PlayerSettings.companyName      = "Deencraft";
            PlayerSettings.productName      = "Deencraft";
            PlayerSettings.bundleVersion    = "1.0.0";

            // WebGL-specific settings
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.exceptionSupport   = WebGLExceptionSupport.None;  // smaller build
            PlayerSettings.WebGL.debugSymbolMode    = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.dataCaching        = true;
            PlayerSettings.WebGL.template           = "PROJECT:Deencraft";

            // Memory (64–256 MB for WebGL — adjust based on profile)
            // Unity 2022 uses WasmStreaming + SharedArrayBuffer by default
            PlayerSettings.WebGL.memorySize         = 256;

            // IL2CPP backend (required for WebGL)
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);

            // .NET Standard 2.1
            PlayerSettings.SetApiCompatibilityLevel(
                BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_Standard_2_1);

            // ── Gather scenes ─────────────────────────────────────────────
            string[] scenes = GetBuildScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WebGLBuild] No scenes in Build Settings. Aborting.");
                EditorApplication.Exit(1);
                return;
            }

            // ── Ensure output dir exists ──────────────────────────────────
            string fullOutput = Path.Combine(Directory.GetCurrentDirectory(), OutputPath);
            Directory.CreateDirectory(fullOutput);

            // ── Build ─────────────────────────────────────────────────────
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = fullOutput,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.CleanBuildCache | BuildOptions.StrictMode,
            };

            BuildReport  report  = BuildPipeline.BuildPlayer(opts);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuild] Build succeeded in {summary.totalTime:mm\\:ss} — " +
                          $"{summary.totalSize / (1024 * 1024):0.0} MB → {fullOutput}");
            }
            else
            {
                Debug.LogError($"[WebGLBuild] Build FAILED: {summary.result}");
                EditorApplication.Exit(1);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string[] GetBuildScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                    scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }
    }
}
#endif

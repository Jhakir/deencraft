#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeenCraft.Editor
{
    /// <summary>
    /// Validates that firebase-config.json exists in StreamingAssets and
    /// contains all required keys. Logs clear errors if anything is missing
    /// so developers catch misconfigurations immediately.
    ///
    /// Runs automatically when Unity recompiles scripts (InitializeOnLoad),
    /// and is also callable from the Deencraft menu.
    /// </summary>
    [InitializeOnLoad]
    public static class FirebaseConfigValidator
    {
        private static readonly string[] RequiredKeys =
        {
            "apiKey",
            "authDomain",
            "projectId",
            "storageBucket",
            "messagingSenderId",
            "appId",
        };

        private const string ConfigFileName   = "firebase-config.json";
        private const string TemplateFileName = "firebase-config.template.json";

        private static string ConfigPath =>
            Path.Combine(Application.streamingAssetsPath, ConfigFileName);

        private static string TemplatePath =>
            Path.Combine(Application.streamingAssetsPath, TemplateFileName);

        // Runs once when Unity loads / recompiles
        static FirebaseConfigValidator()
        {
            ValidateOnLoad();
        }

        [MenuItem("Deencraft/Validate Firebase Config")]
        public static void ValidateFromMenu()
        {
            var result = Validate();
            if (result.IsValid)
            {
                EditorUtility.DisplayDialog(
                    "Firebase Config",
                    "✅ firebase-config.json is valid and contains all required keys.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Firebase Config — Issues Found",
                    result.ErrorMessage,
                    "OK");
            }
        }

        public static ValidationResult Validate()
        {
            if (!File.Exists(ConfigPath))
            {
                return ValidationResult.Fail(
                    $"firebase-config.json not found at:\n{ConfigPath}\n\n" +
                    $"Copy {TemplateFileName} to firebase-config.json and fill in your " +
                    $"Firebase project values from the Firebase Console → Project Settings → Your apps.");
            }

            string json;
            try
            {
                json = File.ReadAllText(ConfigPath);
            }
            catch (IOException ex)
            {
                return ValidationResult.Fail($"Could not read firebase-config.json: {ex.Message}");
            }

            foreach (var key in RequiredKeys)
            {
                // Simple substring check — avoids a JSON dependency in editor code
                if (!json.Contains($"\"{key}\""))
                    return ValidationResult.Fail($"Missing required key: \"{key}\" in firebase-config.json");

                // Detect unfilled template placeholders
                if (json.Contains($"\"YOUR_") || json.Contains("YOUR_PROJECT_ID"))
                    return ValidationResult.Fail(
                        "firebase-config.json still contains placeholder values. " +
                        "Replace all YOUR_* values with real Firebase credentials.");
            }

            return ValidationResult.Ok();
        }

        // ── Internal ─────────────────────────────────────────────────────

        private static void ValidateOnLoad()
        {
            // Only warn on load — don't block the editor
            if (!File.Exists(ConfigPath))
            {
                Debug.LogWarning(
                    "[DeenCraft] firebase-config.json not found in StreamingAssets. " +
                    "Run Deencraft → Validate Firebase Config for setup instructions.");
            }
        }

        public readonly struct ValidationResult
        {
            public readonly bool   IsValid;
            public readonly string ErrorMessage;

            private ValidationResult(bool isValid, string errorMessage)
            {
                IsValid      = isValid;
                ErrorMessage = errorMessage;
            }

            public static ValidationResult Ok()             => new(true,  string.Empty);
            public static ValidationResult Fail(string msg) => new(false, msg);
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DeenCraft.Auth.Models;

namespace DeenCraft.Auth
{
    /// <summary>
    /// Stores all game data as JSON files in Application.persistentDataPath.
    /// No internet connection required. Perfect for local development.
    ///
    /// Folder layout under persistentDataPath/deencraft-local/:
    ///   parents/
    ///     {parentUid}/
    ///       account.json
    ///       children/
    ///         {childId}.json
    ///         {childId}/
    ///           saves/
    ///             {saveId}.json
    ///   auth.json   ← stores hashed email→uid mapping for "sign in"
    ///
    /// Security note: passwords are hashed with SHA-256 before storage.
    /// This is intentionally lightweight — for local dev only.
    /// Switch to FirebaseBackend for production.
    /// </summary>
    public sealed class LocalFileBackend : IDataBackend
    {
        // ── Test hook ─────────────────────────────────────────────────────
        // Allows Edit Mode tests to redirect the data root to a temp folder
        // instead of Application.persistentDataPath (not available in tests).
        private static string _testRootOverride;

        /// <summary>
        /// Override the root data path for testing. Pass null to restore normal behaviour.
        /// Only for use in tests — never call this in production code.
        /// </summary>
        public static void OverrideRootForTesting(string path) => _testRootOverride = path;

        // ── Paths ─────────────────────────────────────────────────────────
        private static string RootDir =>
            _testRootOverride != null
                ? Path.Combine(_testRootOverride, "deencraft-local")
                : Path.Combine(Application.persistentDataPath, "deencraft-local");

        private static string AuthFile =>
            Path.Combine(RootDir, "auth.json");

        private static string ParentsDir =>
            Path.Combine(RootDir, "parents");

        private static string ParentDir(string uid) =>
            Path.Combine(ParentsDir, SanitizePath(uid));

        private static string AccountFile(string uid) =>
            Path.Combine(ParentDir(uid), "account.json");

        private static string ChildrenDir(string uid) =>
            Path.Combine(ParentDir(uid), "children");

        private static string ChildFile(string uid, string childId) =>
            Path.Combine(ChildrenDir(uid), SanitizePath(childId) + ".json");

        private static string SavesDir(string uid, string childId) =>
            Path.Combine(ChildrenDir(uid), SanitizePath(childId), "saves");

        private static string SaveFile(string uid, string childId, string saveId) =>
            Path.Combine(SavesDir(uid, childId), SanitizePath(saveId) + ".json");

        // ── Auth store ────────────────────────────────────────────────────
        // Simple email→(uid, passwordHash) lookup — dev only, not for production

        [Serializable]
        private sealed class AuthStore
        {
            public List<AuthEntry> entries = new List<AuthEntry>();
        }

        [Serializable]
        private sealed class AuthEntry
        {
            public string emailHash;    // SHA-256(email) — avoids storing emails in plaintext
            public string uid;
            public string passwordHash; // SHA-256(password)
        }

        // ── IDataBackend: Auth ────────────────────────────────────────────

        public Task<ParentAccount> CreateParentAsync(
            string email, string password, string displayName)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("Password must be >= 8 characters", nameof(password));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("displayName is required", nameof(displayName));

            var store     = LoadAuthStore();
            var emailHash = Hash(email.ToLowerInvariant());

            if (store.entries.Any(e => e.emailHash == emailHash))
                throw new InvalidOperationException("An account with this email already exists.");

            var uid     = Guid.NewGuid().ToString("N");
            var account = new ParentAccount(uid, displayName);

            store.entries.Add(new AuthEntry
            {
                emailHash    = emailHash,
                uid          = uid,
                passwordHash = Hash(password),
            });

            SaveAuthStore(store);
            WriteJson(AccountFile(uid), account);

            return Task.FromResult(account);
        }

        public Task<ParentAccount> SignInParentAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required", nameof(password));

            var store     = LoadAuthStore();
            var emailHash = Hash(email.ToLowerInvariant());
            var entry     = store.entries.FirstOrDefault(e => e.emailHash == emailHash);

            if (entry == null || entry.passwordHash != Hash(password))
                throw new UnauthorizedAccessException("Incorrect email or password.");

            var account = ReadJson<ParentAccount>(AccountFile(entry.uid));
            if (account == null)
                throw new InvalidOperationException("Account data not found. Please re-register.");

            return Task.FromResult(account);
        }

        public Task SignOutAsync() => Task.CompletedTask; // stateless — nothing to do

        public Task InitializeAsync() => Task.CompletedTask; // synchronous setup, nothing async needed

        public Task<ParentAccount> GetCachedParentAsync(string parentUid)
        {
            if (string.IsNullOrEmpty(parentUid)) return Task.FromResult<ParentAccount>(null);
            var account = ReadJson<ParentAccount>(AccountFile(parentUid));
            return Task.FromResult(account);
        }

        // ── IDataBackend: Child Profiles ──────────────────────────────────

        public Task<List<ChildProfile>> GetChildProfilesAsync(string parentUid)
        {
            ValidateUid(parentUid);
            var dir = ChildrenDir(parentUid);
            var result = new List<ChildProfile>();

            if (!Directory.Exists(dir))
                return Task.FromResult(result);

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var profile = ReadJson<ChildProfile>(file);
                if (profile != null && profile.IsValid())
                    result.Add(profile);
            }

            return Task.FromResult(result);
        }

        public Task<ChildProfile> CreateChildProfileAsync(string parentUid, ChildProfile profile)
        {
            ValidateUid(parentUid);
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.IsValid()) throw new ArgumentException("Profile is invalid", nameof(profile));

            WriteJson(ChildFile(parentUid, profile.id), profile);
            return Task.FromResult(profile);
        }

        public Task UpdateChildProfileAsync(string parentUid, ChildProfile profile)
        {
            ValidateUid(parentUid);
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.IsValid()) throw new ArgumentException("Profile is invalid", nameof(profile));

            WriteJson(ChildFile(parentUid, profile.id), profile);
            return Task.CompletedTask;
        }

        public Task DeleteChildProfileAsync(string parentUid, string childId)
        {
            ValidateUid(parentUid);
            var file = ChildFile(parentUid, childId);
            if (File.Exists(file)) File.Delete(file);
            var savesDir = SavesDir(parentUid, childId);
            if (Directory.Exists(savesDir)) Directory.Delete(savesDir, recursive: true);
            return Task.CompletedTask;
        }

        // ── IDataBackend: World Saves ─────────────────────────────────────

        public Task<WorldSaveData> GetWorldSaveAsync(
            string parentUid, string childId, string saveId)
        {
            ValidateUid(parentUid);
            var save = ReadJson<WorldSaveData>(SaveFile(parentUid, childId, saveId));
            if (save == null)
                throw new FileNotFoundException($"Save not found: {saveId}");
            return Task.FromResult(save);
        }

        public Task SaveWorldAsync(string parentUid, string childId, WorldSaveData save)
        {
            ValidateUid(parentUid);
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (!save.IsValid()) throw new ArgumentException("Save data is invalid", nameof(save));

            save.savedAt = DateTime.UtcNow.ToString("o");
            WriteJson(SaveFile(parentUid, childId, save.id), save);
            return Task.CompletedTask;
        }

        public Task<List<WorldSaveData>> ListWorldSavesAsync(string parentUid, string childId)
        {
            ValidateUid(parentUid);
            var dir    = SavesDir(parentUid, childId);
            var result = new List<WorldSaveData>();

            if (!Directory.Exists(dir))
                return Task.FromResult(result);

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var save = ReadJson<WorldSaveData>(file);
                if (save != null && save.IsValid())
                    result.Add(save);
            }

            result.Sort((a, b) => string.Compare(b.savedAt, a.savedAt, StringComparison.Ordinal));
            return Task.FromResult(result);
        }

        public Task DeleteWorldSaveAsync(string parentUid, string childId, string saveId)
        {
            ValidateUid(parentUid);
            var file = SaveFile(parentUid, childId, saveId);
            if (File.Exists(file)) File.Delete(file);
            return Task.CompletedTask;
        }

        // ── Dev Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Wipes all local data. Use from the Editor for clean-slate testing.
        /// Never call in production code.
        /// </summary>
        public void WipeAllData()
        {
            if (Directory.Exists(RootDir))
                Directory.Delete(RootDir, recursive: true);
            Debug.LogWarning("[LocalFileBackend] All local data wiped.");
        }

        /// <summary>Returns the root folder path for inspection in Finder.</summary>
        public string GetDataRootPath() => RootDir;

        // ── Private Helpers ───────────────────────────────────────────────

        private static void WriteJson<T>(string path, T obj)
        {
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(obj, prettyPrint: true));
        }

        private static T ReadJson<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalFileBackend] Failed to parse {path}: {ex.Message}");
                return null;
            }
        }

        private AuthStore LoadAuthStore()
        {
            var file = AuthFile;
            if (!File.Exists(file)) return new AuthStore();
            return ReadJson<AuthStore>(file) ?? new AuthStore();
        }

        private void SaveAuthStore(AuthStore store) => WriteJson(AuthFile, store);

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash  = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string SanitizePath(string segment) =>
            segment.Replace("/", "_").Replace("\\", "_").Replace("..", "__");

        private static void ValidateUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("parentUid must not be empty", nameof(uid));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DeenCraft.Auth.Models;

namespace DeenCraft.Auth
{
    /// <summary>
    /// Manages all data persistence for Deencraft (Auth + save data).
    ///
    /// Backend selection (Inspector field _useLocalBackend):
    ///   true  → LocalFileBackend  — no internet, JSON files, for local development
    ///   false → FirebaseBackend   — Firebase Auth + Firestore, for production
    ///
    /// To switch: flip the checkbox in the Inspector on the GameManager prefab.
    ///
    /// Security:
    ///   - LocalFileBackend is for development ONLY — never ship it as production.
    ///   - Firebase config loaded from StreamingAssets/firebase-config.json (gitignored).
    ///   - PIN hashed with SHA-256 — never stored in plaintext.
    /// </summary>
    public sealed class FirebaseAuthManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────
        private static FirebaseAuthManager _instance;
        public  static FirebaseAuthManager Instance => _instance;

        // ── Inspector Fields ─────────────────────────────────────────────
        [SerializeField] private bool _autoRestoreSession = true;

        /// <summary>
        /// Use the local file backend instead of Firebase.
        /// Enable this for local development (no internet / Firebase project required).
        /// Disable and configure Firebase before shipping.
        /// </summary>
        [SerializeField] private bool _useLocalBackend = true;

        // ── Backend ────────────────────────────────────────────────────────
        private IDataBackend _backend;
        public  IDataBackend Backend => _backend;

        // ── State ─────────────────────────────────────────────────────────
        private bool _isInitialized;
        private bool _isInitializing;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<string> OnError;
        public event Action         OnInitialized;

        // ── Unity Lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _backend = _useLocalBackend
                ? (IDataBackend) new LocalFileBackend()
                : new FirebaseBackend();

            Debug.Log($"[FirebaseAuthManager] Backend: {_backend.GetType().Name}");
        }

        private void Start() => _ = InitializeAsync();

        // ── Initialization ────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            if (_isInitialized || _isInitializing) return;
            _isInitializing = true;
            try
            {
                await _backend.InitializeAsync();
                _isInitialized = true;
                if (_autoRestoreSession) TryRestoreSession();
                OnInitialized?.Invoke();
            }
            catch (Exception ex) { NotifyError($"Init failed: {ex.Message}"); }
            finally { _isInitializing = false; }
        }

        // ── Parent Auth ────────────────────────────────────────────────────

        public async Task<ParentAccount> CreateParentAccountAsync(
            string email, string password, string displayName)
        {
            ValidateInitialized();
            ValidateEmail(email);
            ValidatePassword(password);
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("displayName is required", nameof(displayName));

            var account = await _backend.CreateParentAsync(email, password, displayName);
            SessionManager.SetParent(account);
            PersistParentUid(account.uid);
            return account;
        }

        public async Task<ParentAccount> SignInParentAsync(string email, string password)
        {
            ValidateInitialized();
            ValidateEmail(email);
            ValidatePassword(password);

            var account = await _backend.SignInParentAsync(email, password);
            SessionManager.SetParent(account);
            PersistParentUid(account.uid);
            return account;
        }

        public async Task SignOutAsync()
        {
            await _backend.SignOutAsync();
            SessionManager.Clear();
        }

        // ── Child Profiles ─────────────────────────────────────────────────

        public Task<List<ChildProfile>> LoadChildProfilesAsync()
        {
            ValidateInitialized();
            ValidateParentSignedIn();
            return _backend.GetChildProfilesAsync(SessionManager.ActiveParent.uid);
        }

        public async Task<ChildProfile> CreateChildProfileAsync(
            string username, int avatarIndex, string plainTextPin = "")
        {
            ValidateInitialized();
            ValidateParentSignedIn();
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required", nameof(username));
            if (username.Length > 20)
                throw new ArgumentException("username must be 20 chars or fewer", nameof(username));

            var profile = new ChildProfile(username, avatarIndex);
            if (!string.IsNullOrEmpty(plainTextPin))
            {
                if (plainTextPin.Length != 4 || !IsDigitsOnly(plainTextPin))
                    throw new ArgumentException("PIN must be exactly 4 digits", nameof(plainTextPin));
                profile.pinHash = HashPin(plainTextPin);
            }

            return await _backend.CreateChildProfileAsync(SessionManager.ActiveParent.uid, profile);
        }

        public async Task ActivateChildAsync(ChildProfile profile, string plainTextPin = "")
        {
            ValidateInitialized();
            ValidateParentSignedIn();
            if (profile == null)    throw new ArgumentNullException(nameof(profile));
            if (!profile.IsValid()) throw new ArgumentException("Profile is invalid", nameof(profile));

            if (!string.IsNullOrEmpty(profile.pinHash))
            {
                if (string.IsNullOrEmpty(plainTextPin))
                    throw new InvalidOperationException("This profile requires a PIN.");
                if (HashPin(plainTextPin) != profile.pinHash)
                    throw new UnauthorizedAccessException("Incorrect PIN.");
            }

            profile.lastPlayedAt = DateTime.UtcNow.ToString("o");
            await _backend.UpdateChildProfileAsync(SessionManager.ActiveParent.uid, profile);
            SessionManager.SetActiveChild(profile);
        }

        // ── World Save ─────────────────────────────────────────────────────

        public async Task SaveWorldAsync(WorldSaveData saveData)
        {
            ValidateInitialized();
            ValidateParentSignedIn();
            if (!SessionManager.IsChildActive)
                throw new InvalidOperationException("No active child profile.");
            if (saveData == null)    throw new ArgumentNullException(nameof(saveData));
            if (!saveData.IsValid()) throw new ArgumentException("Save data is invalid", nameof(saveData));

            await _backend.SaveWorldAsync(
                SessionManager.ActiveParent.uid, SessionManager.ActiveChild.id, saveData);
        }

        public Task<WorldSaveData> LoadWorldSaveAsync(string saveId)
        {
            ValidateInitialized();
            ValidateParentSignedIn();
            if (!SessionManager.IsChildActive)
                throw new InvalidOperationException("No active child profile.");

            return _backend.GetWorldSaveAsync(
                SessionManager.ActiveParent.uid, SessionManager.ActiveChild.id, saveId);
        }

        // ── Private ────────────────────────────────────────────────────────

        private void TryRestoreSession()
        {
            var savedUid = PlayerPrefs.GetString(GameConstants.PrefsParentUidKey, string.Empty);
            if (!string.IsNullOrEmpty(savedUid))
                _ = RestoreSessionAsync(savedUid);
        }

        private async Task RestoreSessionAsync(string parentUid)
        {
            try
            {
                var account = await _backend.GetCachedParentAsync(parentUid);
                if (account == null) return;
                SessionManager.SetParent(account);

                var childId = PlayerPrefs.GetString(GameConstants.PrefsActiveChildKey, string.Empty);
                if (!string.IsNullOrEmpty(childId))
                {
                    var profiles = await _backend.GetChildProfilesAsync(parentUid);
                    var child    = profiles.Find(p => p.id == childId);
                    if (child != null) SessionManager.SetActiveChild(child);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAuthManager] Session restore failed: {ex.Message}");
            }
        }

        private static void PersistParentUid(string uid)
        {
            PlayerPrefs.SetString(GameConstants.PrefsParentUidKey, uid);
            PlayerPrefs.Save();
        }

        private void ValidateInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(
                    "FirebaseAuthManager not initialized. Await InitializeAsync() first.");
        }

        private static void ValidateParentSignedIn()
        {
            if (!SessionManager.IsParentLoggedIn)
                throw new InvalidOperationException("No parent is currently signed in.");
        }

        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));
            if (!email.Contains("@") || email.IndexOf('.', email.IndexOf('@')) < 0)
                throw new ArgumentException("Email format is invalid", nameof(email));
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required", nameof(password));
            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters", nameof(password));
        }

        private static bool IsDigitsOnly(string s)
        {
            foreach (char c in s)
                if (c < '0' || c > '9') return false;
            return true;
        }

        private static string HashPin(string pin)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(pin);
            var hash  = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"[FirebaseAuthManager] {message}");
            OnError?.Invoke(message);
        }
    }
}

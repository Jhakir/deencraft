using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DeenCraft.Auth.Models;

// Firebase SDK references — these are conditional so the project compiles
// even before the Firebase package is imported into Unity.
#if FIREBASE_AVAILABLE
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace DeenCraft.Auth
{
    /// <summary>
    /// Manages all Firebase Authentication and Firestore operations for Deencraft.
    ///
    /// Lifecycle:
    ///   1. Call InitializeAsync() at app startup to verify Firebase is ready.
    ///   2. Call SignInParentAsync() or CreateParentAccountAsync() from the login UI.
    ///   3. Call LoadChildProfilesAsync() to populate the child-select screen.
    ///   4. Call ActivateChildAsync() when a child taps their profile.
    ///   5. Call SignOutAsync() when parent taps Sign Out.
    ///
    /// Security:
    ///   - Firebase config is loaded from StreamingAssets/firebase-config.json at runtime.
    ///     This file is .gitignored. The template is committed as firebase-config.template.json.
    ///   - Firestore rules (deployed separately) must restrict child profile access to the
    ///     authenticated parent's UID.
    ///   - PIN is hashed with SHA-256 before storage — never stored in plaintext.
    ///   - All Firestore writes go through server-side validation rules.
    /// </summary>
    public sealed class FirebaseAuthManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────
        private static FirebaseAuthManager _instance;
        public  static FirebaseAuthManager Instance => _instance;

        // ── Inspector Fields ─────────────────────────────────────────────
        [SerializeField] private bool _autoRestoreSession = true;

        // ── State ────────────────────────────────────────────────────────
        private bool _isInitialized;
        private bool _isInitializing;

        // ── Events ───────────────────────────────────────────────────────
        public event Action<string> OnError;
        public event Action         OnInitialized;

        // ── Unity Lifecycle ──────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        // ── Initialization ───────────────────────────────────────────────

        /// <summary>
        /// Initializes Firebase. Must be awaited before any other Firebase call.
        /// Fires OnInitialized on success, OnError on failure.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized || _isInitializing) return;
            _isInitializing = true;

            try
            {
#if FIREBASE_AVAILABLE
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus != DependencyStatus.Available)
                {
                    NotifyError($"Firebase dependencies unavailable: {dependencyStatus}");
                    return;
                }
#endif
                _isInitialized = true;

                if (_autoRestoreSession)
                    TryRestoreSession();

                OnInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                NotifyError($"Firebase init failed: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        // ── Parent Auth ──────────────────────────────────────────────────

        /// <summary>
        /// Creates a new parent account in Firebase Auth and a corresponding
        /// Firestore document.
        /// </summary>
        /// <param name="email">Parent's email address.</param>
        /// <param name="password">Password (min 8 chars enforced here, Firebase also enforces).</param>
        /// <param name="displayName">Name shown in the parent dashboard.</param>
        public async Task<ParentAccount> CreateParentAccountAsync(
            string email,
            string password,
            string displayName)
        {
            ValidateInitialized();
            ValidateEmail(email);
            ValidatePassword(password);
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("displayName is required", nameof(displayName));

#if FIREBASE_AVAILABLE
            var auth   = FirebaseAuth.DefaultInstance;
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            var uid     = result.User.UserId;
            var account = new ParentAccount(uid, displayName);

            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("parents").Document(uid);
            await doc.SetAsync(account.ToDictionary());

            SessionManager.SetParent(account);
            PersistParentUid(uid);
            return account;
#else
            // Stub for compilation without Firebase package
            await Task.CompletedTask;
            var stubAccount = new ParentAccount("stub-uid", displayName);
            SessionManager.SetParent(stubAccount);
            return stubAccount;
#endif
        }

        /// <summary>
        /// Signs in an existing parent with email + password.
        /// Loads their Firestore profile and sets the active session.
        /// </summary>
        public async Task<ParentAccount> SignInParentAsync(string email, string password)
        {
            ValidateInitialized();
            ValidateEmail(email);
            ValidatePassword(password);

#if FIREBASE_AVAILABLE
            var auth   = FirebaseAuth.DefaultInstance;
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            var uid    = result.User.UserId;

            var account = await LoadParentAccountAsync(uid);
            account.lastLoginAt = DateTime.UtcNow.ToString("o");

            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("parents").Document(uid);
            await doc.UpdateAsync("lastLoginAt", account.lastLoginAt);

            SessionManager.SetParent(account);
            PersistParentUid(uid);
            return account;
#else
            await Task.CompletedTask;
            var stubAccount = new ParentAccount("stub-uid", "Stub Parent");
            SessionManager.SetParent(stubAccount);
            return stubAccount;
#endif
        }

        /// <summary>Signs the current parent out and clears the session.</summary>
        public async Task SignOutAsync()
        {
#if FIREBASE_AVAILABLE
            FirebaseAuth.DefaultInstance.SignOut();
#endif
            await Task.CompletedTask;
            SessionManager.Clear();
        }

        // ── Child Profiles ───────────────────────────────────────────────

        /// <summary>
        /// Loads all child profiles for the currently signed-in parent.
        /// Returns an empty list if no children have been created yet.
        /// </summary>
        public async Task<List<ChildProfile>> LoadChildProfilesAsync()
        {
            ValidateInitialized();
            ValidateParentSignedIn();

#if FIREBASE_AVAILABLE
            var db       = FirebaseFirestore.DefaultInstance;
            var colRef   = db.Collection("parents")
                             .Document(SessionManager.ActiveParent.uid)
                             .Collection(GameConstants.FirestoreChildProfilesCollection);
            var snapshot = await colRef.GetSnapshotAsync();
            var profiles = new List<ChildProfile>();

            foreach (var doc in snapshot.Documents)
            {
                var profile = doc.ConvertTo<ChildProfile>();
                if (profile.IsValid())
                    profiles.Add(profile);
            }
            return profiles;
#else
            await Task.CompletedTask;
            return new List<ChildProfile>();
#endif
        }

        /// <summary>
        /// Creates a new child profile and saves it to Firestore.
        /// </summary>
        public async Task<ChildProfile> CreateChildProfileAsync(
            string username,
            int    avatarIndex,
            string plainTextPin = "")
        {
            ValidateInitialized();
            ValidateParentSignedIn();

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required", nameof(username));
            if (username.Length > 20)
                throw new ArgumentException("username must be 20 characters or fewer", nameof(username));

            var profile = new ChildProfile(username, avatarIndex);

            if (!string.IsNullOrEmpty(plainTextPin))
            {
                if (plainTextPin.Length != 4 || !IsDigitsOnly(plainTextPin))
                    throw new ArgumentException("PIN must be exactly 4 digits", nameof(plainTextPin));
                profile.pinHash = HashPin(plainTextPin);
            }

#if FIREBASE_AVAILABLE
            var db     = FirebaseFirestore.DefaultInstance;
            var colRef = db.Collection("parents")
                           .Document(SessionManager.ActiveParent.uid)
                           .Collection(GameConstants.FirestoreChildProfilesCollection);
            await colRef.Document(profile.id).SetAsync(profile.ToDictionary());
#else
            await Task.CompletedTask;
#endif
            return profile;
        }

        /// <summary>
        /// Activates a child profile for the current play session.
        /// Validates PIN if one is set.
        /// </summary>
        public async Task ActivateChildAsync(ChildProfile profile, string plainTextPin = "")
        {
            ValidateInitialized();
            ValidateParentSignedIn();

            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.IsValid()) throw new ArgumentException("Profile data is invalid", nameof(profile));

            if (!string.IsNullOrEmpty(profile.pinHash))
            {
                if (string.IsNullOrEmpty(plainTextPin))
                    throw new InvalidOperationException("This profile requires a PIN.");

                if (HashPin(plainTextPin) != profile.pinHash)
                    throw new UnauthorizedAccessException("Incorrect PIN.");
            }

            profile.lastPlayedAt = DateTime.UtcNow.ToString("o");

#if FIREBASE_AVAILABLE
            var db     = FirebaseFirestore.DefaultInstance;
            var docRef = db.Collection("parents")
                           .Document(SessionManager.ActiveParent.uid)
                           .Collection(GameConstants.FirestoreChildProfilesCollection)
                           .Document(profile.id);
            await docRef.UpdateAsync("lastPlayedAt", profile.lastPlayedAt);
#else
            await Task.CompletedTask;
#endif
            SessionManager.SetActiveChild(profile);
        }

        // ── World Save ───────────────────────────────────────────────────

        /// <summary>
        /// Saves the child's world metadata to Firestore.
        /// Chunk data is saved separately (Phase 7).
        /// </summary>
        public async Task SaveWorldAsync(WorldSaveData saveData)
        {
            ValidateInitialized();
            ValidateParentSignedIn();
            if (!SessionManager.IsChildActive)
                throw new InvalidOperationException("No active child profile.");
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));
            if (!saveData.IsValid()) throw new ArgumentException("Save data is invalid", nameof(saveData));

            saveData.savedAt = DateTime.UtcNow.ToString("o");

#if FIREBASE_AVAILABLE
            var db     = FirebaseFirestore.DefaultInstance;
            var docRef = db.Collection("parents")
                           .Document(SessionManager.ActiveParent.uid)
                           .Collection(GameConstants.FirestoreChildProfilesCollection)
                           .Document(SessionManager.ActiveChild.id)
                           .Collection(GameConstants.FirestoreWorldSavesCollection)
                           .Document(saveData.id);
            await docRef.SetAsync(saveData.ToDictionary(), SetOptions.MergeAll);
#else
            await Task.CompletedTask;
#endif
        }

        // ── Private Helpers ──────────────────────────────────────────────

        private async Task<ParentAccount> LoadParentAccountAsync(string uid)
        {
#if FIREBASE_AVAILABLE
            var db       = FirebaseFirestore.DefaultInstance;
            var docRef   = db.Collection("parents").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                throw new InvalidOperationException($"No parent document found for uid: {uid}");

            return snapshot.ConvertTo<ParentAccount>();
#else
            await Task.CompletedTask;
            return new ParentAccount(uid, "Parent");
#endif
        }

        private void TryRestoreSession()
        {
            var savedUid = PlayerPrefs.GetString(GameConstants.PrefsParentUidKey, string.Empty);
            if (!string.IsNullOrEmpty(savedUid))
            {
                // Fire-and-forget restore on background thread
                _ = RestoreSessionAsync(savedUid);
            }
        }

        private async Task RestoreSessionAsync(string parentUid)
        {
            try
            {
                var account = await LoadParentAccountAsync(parentUid);
                SessionManager.SetParent(account);

                // Restore active child if one was cached
                var childId = PlayerPrefs.GetString(GameConstants.PrefsActiveChildKey, string.Empty);
                if (!string.IsNullOrEmpty(childId))
                {
                    var profiles = await LoadChildProfilesAsync();
                    var child    = profiles.Find(p => p.id == childId);
                    if (child != null)
                        SessionManager.SetActiveChild(child);
                }
            }
            catch (Exception ex)
            {
                // Session restore failure is non-fatal — just proceed to login screen
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
                    "FirebaseAuthManager has not been initialized. Await InitializeAsync() first.");
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
            if (!email.Contains("@") || !email.Contains("."))
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

        /// <summary>
        /// Hashes a 4-digit PIN with SHA-256.
        /// Never store plain-text PINs.
        /// </summary>
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

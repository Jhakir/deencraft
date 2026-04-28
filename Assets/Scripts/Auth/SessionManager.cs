using System;
using DeenCraft.Auth.Models;

namespace DeenCraft.Auth
{
    /// <summary>
    /// Holds the current in-memory session state.
    /// No Firebase calls — pure in-memory state managed by FirebaseAuthManager.
    ///
    /// Access pattern:
    ///   SessionManager.ActiveParent  → currently logged-in parent (null if none)
    ///   SessionManager.ActiveChild   → child who is currently playing (null if none)
    ///   SessionManager.IsParentLoggedIn → quick boolean check
    /// </summary>
    public static class SessionManager
    {
        // ── State ────────────────────────────────────────────────────────
        private static ParentAccount _activeParent;
        private static ChildProfile  _activeChild;
        private static DateTime      _sessionStart;
        private static bool          _isInitialized;

        // ── Public Read API ──────────────────────────────────────────────
        public static ParentAccount ActiveParent    => _activeParent;
        public static ChildProfile  ActiveChild     => _activeChild;
        public static bool IsParentLoggedIn         => _activeParent != null;
        public static bool IsChildActive            => _activeChild  != null;

        /// <summary>Seconds since the parent logged in.</summary>
        public static double SessionAgeSeconds =>
            _isInitialized ? (DateTime.UtcNow - _sessionStart).TotalSeconds : 0;

        // ── Events ───────────────────────────────────────────────────────
        /// <summary>Fired when a parent signs in successfully.</summary>
        public static event Action<ParentAccount> OnParentLoggedIn;

        /// <summary>Fired when the parent signs out (or session is cleared).</summary>
        public static event Action OnParentLoggedOut;

        /// <summary>Fired when a child profile is selected and activated.</summary>
        public static event Action<ChildProfile> OnChildActivated;

        /// <summary>Fired when the active child profile is deactivated.</summary>
        public static event Action OnChildDeactivated;

        // ── Write API ────────────────────────────────────────────────────
        /// <summary>
        /// Sets the active parent. Called by FirebaseAuthManager after a
        /// successful sign-in or token refresh.
        /// </summary>
        public static void SetParent(ParentAccount parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!parent.IsValid()) throw new ArgumentException("Parent account data is invalid", nameof(parent));

            _activeParent  = parent;
            _sessionStart  = DateTime.UtcNow;
            _isInitialized = true;

            OnParentLoggedIn?.Invoke(_activeParent);
        }

        /// <summary>
        /// Activates a child profile for gameplay.
        /// Persists the child UID to PlayerPrefs for session restore on next launch.
        /// </summary>
        public static void SetActiveChild(ChildProfile child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (!child.IsValid()) throw new ArgumentException("Child profile data is invalid", nameof(child));

#if UNITY_5_3_OR_NEWER
            UnityEngine.PlayerPrefs.SetString(GameConstants.PrefsActiveChildKey, child.id);
            UnityEngine.PlayerPrefs.Save();
#endif
            _activeChild = child;
            OnChildActivated?.Invoke(_activeChild);
        }

        /// <summary>
        /// Deactivates the current child (e.g., child clicks "Back to Profile Select").
        /// </summary>
        public static void ClearActiveChild()
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.PlayerPrefs.DeleteKey(GameConstants.PrefsActiveChildKey);
            UnityEngine.PlayerPrefs.Save();
#endif
            _activeChild = null;
            OnChildDeactivated?.Invoke();
        }

        /// <summary>
        /// Clears the entire session (parent signed out).
        /// Fires <see cref="OnParentLoggedOut"/> if a parent was active,
        /// then nulls all event subscriptions to prevent cross-test bleed.
        /// </summary>
        public static void Clear()
        {
            bool hadParent = _activeParent != null;

#if UNITY_5_3_OR_NEWER
            if (_activeChild != null)
            {
                UnityEngine.PlayerPrefs.DeleteKey(GameConstants.PrefsActiveChildKey);
                UnityEngine.PlayerPrefs.Save();
            }
#endif
            _activeChild   = null;
            _activeParent  = null;
            _isInitialized = false;

#if UNITY_5_3_OR_NEWER
            UnityEngine.PlayerPrefs.DeleteKey(GameConstants.PrefsParentUidKey);
            UnityEngine.PlayerPrefs.Save();
#endif

            if (hadParent)
                OnParentLoggedOut?.Invoke();

            // Null subscriptions to prevent cross-test bleed.
            OnParentLoggedIn   = null;
            OnParentLoggedOut  = null;
            OnChildActivated   = null;
            OnChildDeactivated = null;
        }
    }
}

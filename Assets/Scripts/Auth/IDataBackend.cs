using System.Collections.Generic;
using System.Threading.Tasks;
using DeenCraft.Auth.Models;

namespace DeenCraft.Auth
{
    /// <summary>
    /// Abstracts all persistence operations so the game can run against either
    /// a local file system (development) or Firebase (production) without
    /// changing any game code.
    ///
    /// To switch backends, change the single line in FirebaseAuthManager.Awake():
    ///   _backend = new LocalFileBackend();    // local dev — no internet needed
    ///   _backend = new FirebaseBackend();     // production
    /// </summary>
    public interface IDataBackend
    {
        // ── Lifecycle ─────────────────────────────────────────────────────

        /// <summary>Called once on startup. FirebaseBackend checks SDK dependencies here.</summary>
        Task InitializeAsync();

        // ── Auth ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new parent account. Returns the created account.
        /// Throws on duplicate email or invalid data.
        /// </summary>
        Task<ParentAccount> CreateParentAsync(string email, string password, string displayName);

        /// <summary>
        /// Signs in an existing parent. Returns their account.
        /// Throws <see cref="System.UnauthorizedAccessException"/> on wrong credentials.
        /// </summary>
        Task<ParentAccount> SignInParentAsync(string email, string password);

        /// <summary>Signs the current parent out.</summary>
        Task SignOutAsync();

        /// <summary>
        /// Returns a cached/stored parent account by UID without re-authentication.
        /// Used for session restore on app restart. Returns null if not found.
        /// </summary>
        Task<ParentAccount> GetCachedParentAsync(string parentUid);

        // ── Child Profiles ────────────────────────────────────────────────

        Task<List<ChildProfile>>  GetChildProfilesAsync(string parentUid);
        Task<ChildProfile>        CreateChildProfileAsync(string parentUid, ChildProfile profile);
        Task                      UpdateChildProfileAsync(string parentUid, ChildProfile profile);
        Task                      DeleteChildProfileAsync(string parentUid, string childId);

        // ── World Saves ───────────────────────────────────────────────────

        Task<WorldSaveData>       GetWorldSaveAsync(string parentUid, string childId, string saveId);
        Task                      SaveWorldAsync(string parentUid, string childId, WorldSaveData save);
        Task<List<WorldSaveData>> ListWorldSavesAsync(string parentUid, string childId);
        Task                      DeleteWorldSaveAsync(string parentUid, string childId, string saveId);
    }
}

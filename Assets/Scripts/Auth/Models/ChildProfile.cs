using System;

namespace DeenCraft.Auth.Models
{
    /// <summary>
    /// A child's profile stored as a Firestore document at
    /// /parents/{parentUid}/childProfiles/{childId}.
    ///
    /// Security note: child profiles are scoped under the parent document.
    /// Firestore rules must enforce that only the authenticated parent can
    /// read/write their own children's profiles.
    /// </summary>
    [Serializable]
    public sealed class ChildProfile
    {
        /// <summary>Auto-generated unique ID (Firestore document ID).</summary>
        public string id;

        /// <summary>Child's chosen in-game username. Shown to other players in future multiplayer.</summary>
        public string username;

        /// <summary>Integer index of the chosen avatar icon (for the profile selection screen).</summary>
        public int avatarIndex;

        /// <summary>Optional 4-digit PIN for profile access. Empty string means no PIN required.</summary>
        public string pinHash;  // Stored as SHA-256 hex — never store plain-text PIN

        /// <summary>The child's saved character appearance.</summary>
        public CharacterData character;

        /// <summary>ID of the child's active world save document in Firestore.</summary>
        public string activeWorldSaveId;

        /// <summary>ISO 8601 UTC timestamp of profile creation.</summary>
        public string createdAt;

        /// <summary>ISO 8601 UTC timestamp of last time this child played.</summary>
        public string lastPlayedAt;

        public ChildProfile() { }

        public ChildProfile(string username, int avatarIndex)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username must not be empty", nameof(username));
            if (username.Length > 20)
                throw new ArgumentException("username must be 20 characters or fewer", nameof(username));
            if (avatarIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(avatarIndex), "must be >= 0");

            id                = Guid.NewGuid().ToString("N");
            this.username     = username.Trim();
            this.avatarIndex  = avatarIndex;
            pinHash           = string.Empty;
            character         = new CharacterData();
            createdAt         = DateTime.UtcNow.ToString("o");
            lastPlayedAt      = createdAt;
        }

        /// <summary>Returns true when minimum required fields are populated.</summary>
        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(id) &&
            !string.IsNullOrWhiteSpace(username) &&
            username.Length <= 20 &&
            avatarIndex >= 0 &&
            character != null &&
            character.IsValid();
    }
}

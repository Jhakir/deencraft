using System;

namespace DeenCraft.Auth.Models
{
    /// <summary>
    /// Represents a parent's Firebase account metadata stored in Firestore.
    /// This is the top-level document at /parents/{parentUid}.
    /// Sensitive data (email, password) lives in Firebase Auth — not here.
    /// </summary>
    [Serializable]
    public sealed class ParentAccount
    {
        /// <summary>Firebase Auth UID — also the Firestore document ID.</summary>
        public string uid;

        /// <summary>Display name the parent chose during sign-up.</summary>
        public string displayName;

        /// <summary>ISO 8601 UTC string of when the account was created.</summary>
        public string createdAt;

        /// <summary>ISO 8601 UTC string of the last successful login.</summary>
        public string lastLoginAt;

        public ParentAccount() { }

        public ParentAccount(string uid, string displayName)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("uid must not be empty", nameof(uid));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("displayName must not be empty", nameof(displayName));

            this.uid = uid;
            this.displayName = displayName;
            createdAt  = DateTime.UtcNow.ToString("o");
            lastLoginAt = createdAt;
        }

        /// <summary>Returns true if all required fields are populated.</summary>
        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(uid) &&
            !string.IsNullOrWhiteSpace(displayName);
    }
}

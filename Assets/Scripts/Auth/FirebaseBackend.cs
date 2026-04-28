using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DeenCraft.Auth.Models;

// Firebase SDK references are conditional — project compiles without the package.
#if FIREBASE_AVAILABLE
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
#endif

namespace DeenCraft.Auth
{
    /// <summary>
    /// IDataBackend implementation backed by Firebase Auth + Firestore.
    /// Used in production. Set _useLocalBackend = false on FirebaseAuthManager
    /// and ensure StreamingAssets/firebase-config.json is populated.
    /// </summary>
    public sealed class FirebaseBackend : IDataBackend
    {
        // -- Lifecycle -------------------------------------------------------

        public async Task InitializeAsync()
        {
#if FIREBASE_AVAILABLE
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException($"Firebase unavailable: {status}");
            Debug.Log("[FirebaseBackend] Firebase ready.");
#else
            await Task.CompletedTask;
            Debug.LogWarning("[FirebaseBackend] FIREBASE_AVAILABLE not defined. " +
                             "Import the Firebase SDK or enable _useLocalBackend.");
#endif
        }

        // -- Auth ------------------------------------------------------------

        public async Task<ParentAccount> CreateParentAsync(
            string email, string password, string displayName)
        {
#if FIREBASE_AVAILABLE
            var auth   = FirebaseAuth.DefaultInstance;
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var uid    = result.User.UserId;
            var account = new ParentAccount(uid, displayName);

            var db = FirebaseFirestore.DefaultInstance;
            await db.Collection("parents").Document(uid).SetAsync(account.ToDictionary());
            return account;
#else
            await Task.CompletedTask;
            throw new NotSupportedException("Firebase SDK not available. Enable _useLocalBackend.");
#endif
        }

        public async Task<ParentAccount> SignInParentAsync(string email, string password)
        {
#if FIREBASE_AVAILABLE
            var auth   = FirebaseAuth.DefaultInstance;
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            var uid    = result.User.UserId;
            var account = await GetCachedParentAsync(uid)
                ?? throw new InvalidOperationException($"No Firestore document for uid: {uid}");

            account.lastLoginAt = DateTime.UtcNow.ToString("o");
            var db = FirebaseFirestore.DefaultInstance;
            await db.Collection("parents").Document(uid)
                    .UpdateAsync("lastLoginAt", account.lastLoginAt);
            return account;
#else
            await Task.CompletedTask;
            throw new NotSupportedException("Firebase SDK not available. Enable _useLocalBackend.");
#endif
        }

        public async Task SignOutAsync()
        {
#if FIREBASE_AVAILABLE
            FirebaseAuth.DefaultInstance.SignOut();
#endif
            await Task.CompletedTask;
        }

        public async Task<ParentAccount> GetCachedParentAsync(string parentUid)
        {
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = await db.Collection("parents").Document(parentUid).GetSnapshotAsync();
            if (!doc.Exists) return null;
            return doc.ConvertTo<ParentAccount>();
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        // -- Child Profiles --------------------------------------------------

        public async Task<List<ChildProfile>> GetChildProfilesAsync(string parentUid)
        {
#if FIREBASE_AVAILABLE
            var db   = FirebaseFirestore.DefaultInstance;
            var col  = db.Collection("parents").Document(parentUid)
                         .Collection(GameConstants.FirestoreChildProfilesCollection);
            var snap = await col.GetSnapshotAsync();
            var list = new List<ChildProfile>();
            foreach (var doc in snap.Documents)
            {
                var p = doc.ConvertTo<ChildProfile>();
                if (p.IsValid()) list.Add(p);
            }
            return list;
#else
            await Task.CompletedTask;
            return new List<ChildProfile>();
#endif
        }

        public async Task<ChildProfile> CreateChildProfileAsync(
            string parentUid, ChildProfile profile)
        {
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var col = db.Collection("parents").Document(parentUid)
                        .Collection(GameConstants.FirestoreChildProfilesCollection);
            await col.Document(profile.id).SetAsync(profile.ToDictionary());
#else
            await Task.CompletedTask;
#endif
            return profile;
        }

        public async Task UpdateChildProfileAsync(string parentUid, ChildProfile profile)
        {
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("parents").Document(parentUid)
                        .Collection(GameConstants.FirestoreChildProfilesCollection)
                        .Document(profile.id);
            await doc.SetAsync(profile.ToDictionary(), SetOptions.MergeAll);
#else
            await Task.CompletedTask;
#endif
        }

        public async Task DeleteChildProfileAsync(string parentUid, string childId)
        {
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("parents").Document(parentUid)
                        .Collection(GameConstants.FirestoreChildProfilesCollection)
                        .Document(childId);
            await doc.DeleteAsync();
#else
            await Task.CompletedTask;
#endif
        }

        // -- World Saves -----------------------------------------------------

        public async Task<WorldSaveData> GetWorldSaveAsync(
            string parentUid, string childId, string saveId)
        {
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = await db.Collection("parents").Document(parentUid)
                               .Collection(GameConstants.FirestoreChildProfilesCollection)
                               .Document(childId)
                               .Collection(GameConstants.FirestoreWorldSavesCollection)
                               .Document(saveId)
                               .GetSnapshotAsync();
            if (!doc.Exists) return null;
            return doc.ConvertTo<WorldSaveData>();
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        public async Task SaveWorldAsync(string parentUid, string childId, WorldSaveData save)
        {
            save.savedAt = DateTime.UtcNow.ToString("o");
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("parents").Document(parentUid)
                        .Collection(GameConstants.FirestoreChildProfilesCollection)
                        .Document(childId)
                        .Collection(GameConstants.FirestoreWorldSavesCollection)
                        .Document(save.id);
            await doc.SetAsync(save.ToDictionary(), SetOptions.MergeAll);
#else
            await Task.CompletedTask;
#endif
        }

        public async Task<List<WorldSaveData>> ListWorldSavesAsync(
            string parentUid, string childId)
        {
#if FIREBASE_AVAILABLE
            var db   = FirebaseFirestore.DefaultInstance;
            var col  = db.Collection("parents").Document(parentUid)
                         .Collection(GameConstants.FirestoreChildProfilesCollection)
                         .Document(childId)
                         .Collection(GameConstants.FirestoreWorldSavesCollection);
            var snap = await col.GetSnapshotAsync();
            var list = new List<WorldSaveData>();
            foreach (var doc in snap.Documents)
            {
                var s = doc.ConvertTo<WorldSaveData>();
                if (s != null && s.IsValid()) list.Add(s);
            }
            return list;
#else
            await Task.CompletedTask;
            return new List<WorldSaveData>();
#endif
        }

        public async Task DeleteWorldSaveAsync(
            string parentUid, string childId, string saveId)
        {
#if FIREBASE_AVAILABLE
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("parents").Document(parentUid)
                        .Collection(GameConstants.FirestoreChildProfilesCollection)
                        .Document(childId)
                        .Collection(GameConstants.FirestoreWorldSavesCollection)
                        .Document(saveId);
            await doc.DeleteAsync();
#else
            await Task.CompletedTask;
#endif
        }
    }
}

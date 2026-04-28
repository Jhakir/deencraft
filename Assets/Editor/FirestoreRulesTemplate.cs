#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeenCraft.Editor
{
    /// <summary>
    /// Generates the Firestore security rules file (firestore.rules) in the
    /// project root. Run this once when setting up Firebase, then deploy
    /// the rules with: firebase deploy --only firestore:rules
    ///
    /// Rules summary:
    ///   - /parents/{parentUid}             → read/write only by the authenticated parent
    ///   - /parents/{parentUid}/childProfiles/{childId} → same
    ///   - /parents/{parentUid}/worldSaves/{saveId}     → same
    ///   - All other paths                 → denied
    /// </summary>
    public static class FirestoreRulesTemplate
    {
        private const string RulesFileName = "firestore.rules";
        private const string IndexFileName = "firestore.indexes.json";

        [MenuItem("Deencraft/Firebase/Generate Firestore Rules")]
        public static void GenerateRules()
        {
            WriteRulesFile();
            WriteIndexFile();
            AssetDatabase.Refresh();
            Debug.Log("[DeenCraft] Firestore rules generated. Deploy with: firebase deploy --only firestore:rules");
        }

        private static void WriteRulesFile()
        {
            const string rules = @"rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    // Deny all by default
    match /{document=**} {
      allow read, write: if false;
    }

    // Parent account — only the authenticated parent can read/write their own doc
    match /parents/{parentUid} {
      allow read, write: if request.auth != null
                         && request.auth.uid == parentUid;

      // Child profiles — scoped under the parent
      match /childProfiles/{childId} {
        allow read, write: if request.auth != null
                           && request.auth.uid == parentUid;
      }

      // World saves — scoped under child profile
      match /childProfiles/{childId}/worldSaves/{saveId} {
        allow read, write: if request.auth != null
                           && request.auth.uid == parentUid;
      }
    }
  }
}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), RulesFileName);
            File.WriteAllText(path, rules);
            Debug.Log($"[DeenCraft] Written: {path}");
        }

        private static void WriteIndexFile()
        {
            const string indexes = @"{
  ""indexes"": [],
  ""fieldOverrides"": []
}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), IndexFileName);
            File.WriteAllText(path, indexes);
            Debug.Log($"[DeenCraft] Written: {path}");
        }
    }
}
#endif

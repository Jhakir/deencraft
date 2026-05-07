using System;
using System.Collections.Generic;

namespace DeenCraft.Auth.Models
{
    /// <summary>
    /// Minimal world save metadata stored in Firestore.
    /// Actual chunk data is too large for Firestore — it will be stored
    /// as compressed JSON blobs in Firebase Storage (Phase 7).
    /// This document is the index / manifest.
    /// </summary>
    [Serializable]
    public sealed class WorldSaveData
    {
        /// <summary>Firestore document ID.</summary>
        public string id;

        /// <summary>The child profile this save belongs to.</summary>
        public string childProfileId;

        /// <summary>World seed used for procedural generation.</summary>
        public int seed;

        /// <summary>Player position encoded as "x,y,z".</summary>
        public string playerPosition;

        /// <summary>Player rotation encoded as "x,y,z,w" (Quaternion).</summary>
        public string playerRotation;

        /// <summary>ISO 8601 UTC timestamp of last save.</summary>
        public string savedAt;

        /// <summary>World display name set by the child.</summary>
        public string worldName;

        /// <summary>Number of in-game days elapsed.</summary>
        public int dayCount;

        /// <summary>
        /// Map of inventory slot index → item type ID and count ("typeId:count").
        /// Kept here as a summary so the HUD can restore quickly.
        /// </summary>
        public Dictionary<int, string> hotbarSummary;

        public WorldSaveData() { }

        public WorldSaveData(string childProfileId, int seed, string worldName)
        {
            if (string.IsNullOrWhiteSpace(childProfileId))
                throw new ArgumentException("childProfileId must not be empty", nameof(childProfileId));
            if (string.IsNullOrWhiteSpace(worldName))
                throw new ArgumentException("worldName must not be empty", nameof(worldName));

            id                  = Guid.NewGuid().ToString("N");
            this.childProfileId = childProfileId;
            this.seed           = seed;
            this.worldName      = worldName;
            playerPosition      = "0,64,0";
            playerRotation      = "0,0,0,1";
            savedAt             = DateTime.UtcNow.ToString("o");
            dayCount            = 0;
            hotbarSummary       = new Dictionary<int, string>();
        }

        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(id) &&
            !string.IsNullOrWhiteSpace(childProfileId) &&
            !string.IsNullOrWhiteSpace(worldName);

        // ── Position helpers ──────────────────────────────────────────────

        /// <summary>Encodes a world-space position as a culture-invariant "x,y,z" string.</summary>
        public static string EncodePosition(float x, float y, float z)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture,
                             "{0},{1},{2}", x, y, z);

        /// <summary>
        /// Decodes a "x,y,z" position string into float components.
        /// Returns (0, 64, 0) — sea level — on parse failure.
        /// </summary>
        public static void DecodePosition(string encoded, out float x, out float y, out float z)
        {
            x = 0f; y = 64f; z = 0f;
            if (string.IsNullOrWhiteSpace(encoded)) return;
            var parts = encoded.Split(',');
            if (parts.Length != 3) return;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var style = System.Globalization.NumberStyles.Float;
            float.TryParse(parts[0], style, inv, out x);
            float.TryParse(parts[1], style, inv, out y);
            float.TryParse(parts[2], style, inv, out z);
        }
    }
}

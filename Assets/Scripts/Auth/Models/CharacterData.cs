using System;

namespace DeenCraft.Auth.Models
{
    /// <summary>
    /// Persisted character customization for a child's avatar.
    /// Stored as a nested object inside ChildProfile.
    /// </summary>
    [Serializable]
    public sealed class CharacterData
    {
        // ── Appearance ──────────────────────────────────────────────────
        /// <summary>Skin tone index (0–5, maps to texture variant).</summary>
        public int skinToneIndex;

        /// <summary>
        /// Head covering type.
        /// 0 = none, 1 = hijab, 2 = kufi, 3 = hat.
        /// </summary>
        public int headCoveringType;

        /// <summary>RGBA hex string for head covering color, e.g. "#4A90D9FF".</summary>
        public string headCoveringColor;

        /// <summary>
        /// Outfit style index.
        /// 0 = casual, 1 = thobe, 2 = dress, 3 = jilbab.
        /// </summary>
        public int outfitStyle;

        /// <summary>RGBA hex string for outfit primary color.</summary>
        public string outfitPrimaryColor;

        /// <summary>RGBA hex string for outfit secondary color.</summary>
        public string outfitSecondaryColor;

        // ── Defaults ────────────────────────────────────────────────────
        public CharacterData()
        {
            skinToneIndex        = 2;
            headCoveringType     = 0;
            headCoveringColor    = "#FFFFFFFF";
            outfitStyle          = 0;
            outfitPrimaryColor   = "#3A7DBFFF";
            outfitSecondaryColor = "#FFFFFFFF";
        }

        public bool IsValid() =>
            skinToneIndex    >= 0 && skinToneIndex    <= 5 &&
            headCoveringType >= 0 && headCoveringType <= 3 &&
            outfitStyle      >= 0 && outfitStyle      <= 3 &&
            !string.IsNullOrWhiteSpace(headCoveringColor) &&
            !string.IsNullOrWhiteSpace(outfitPrimaryColor);
    }
}

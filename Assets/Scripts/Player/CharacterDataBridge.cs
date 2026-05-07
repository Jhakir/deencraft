// Assets/Scripts/Player/CharacterDataBridge.cs
// Converts between CharacterData (Auth.Models — int indices) and
// CharacterAppearance (Player — enums + UnityEngine.Color).
using UnityEngine;
using DeenCraft.Auth.Models;

namespace DeenCraft.Player
{
    /// <summary>
    /// Converts <see cref="CharacterData"/> (Auth model, serialized as ints/hex strings)
    /// to and from <see cref="CharacterAppearance"/> (Player system, enums + Color).
    ///
    /// Call <see cref="ToAppearance"/> after loading a child profile to restore
    /// the player's appearance. Call <see cref="ToData"/> before saving.
    /// </summary>
    public static class CharacterDataBridge
    {
        // ── CharacterData → CharacterAppearance ──────────────────────────────

        public static CharacterAppearance ToAppearance(CharacterData data)
        {
            if (data == null) return new CharacterAppearance();

            return new CharacterAppearance
            {
                SkinTone      = IndexToSkinTone(data.skinToneIndex),
                HeadwearType  = IndexToHeadwear(data.headCoveringType),
                ClothingStyle = IndexToClothingStyle(data.outfitStyle),
                ClothingColor = ParseColor(data.outfitPrimaryColor, Color.white),
            };
        }

        // ── CharacterAppearance → CharacterData ──────────────────────────────

        public static CharacterData ToData(CharacterAppearance appearance)
        {
            if (appearance == null) return new CharacterData();

            return new CharacterData
            {
                skinToneIndex        = SkinToneToIndex(appearance.SkinTone),
                headCoveringType     = HeadwearToIndex(appearance.HeadwearType),
                headCoveringColor    = FormatColor(appearance.ClothingColor),
                outfitStyle          = ClothingStyleToIndex(appearance.ClothingStyle),
                outfitPrimaryColor   = FormatColor(appearance.ClothingColor),
                outfitSecondaryColor = "#FFFFFFFF",
            };
        }

        // ── SkinTone ──────────────────────────────────────────────────────────

        private static SkinTone IndexToSkinTone(int index)
        {
            switch (index)
            {
                case 0: return SkinTone.Light;
                case 1: return SkinTone.MediumLight;
                case 2: return SkinTone.Medium;
                case 3: return SkinTone.MediumDark;
                case 4: return SkinTone.Dark;
                default: return SkinTone.Medium;
            }
        }

        private static int SkinToneToIndex(SkinTone tone)
        {
            switch (tone)
            {
                case SkinTone.Light:       return 0;
                case SkinTone.MediumLight: return 1;
                case SkinTone.Medium:      return 2;
                case SkinTone.MediumDark:  return 3;
                case SkinTone.Dark:        return 4;
                default:                   return 2;
            }
        }

        // ── HeadwearType ──────────────────────────────────────────────────────

        private static HeadwearType IndexToHeadwear(int index)
        {
            switch (index)
            {
                case 1:  return HeadwearType.Hijab;
                case 2:  return HeadwearType.Kufi;
                default: return HeadwearType.None;
            }
        }

        private static int HeadwearToIndex(HeadwearType type)
        {
            switch (type)
            {
                case HeadwearType.Hijab: return 1;
                case HeadwearType.Kufi:  return 2;
                default:                 return 0;
            }
        }

        // ── ClothingStyle ─────────────────────────────────────────────────────

        private static ClothingStyle IndexToClothingStyle(int index)
        {
            switch (index)
            {
                case 1:  return ClothingStyle.Traditional;
                case 2:  return ClothingStyle.Winter;
                default: return ClothingStyle.Casual;
            }
        }

        private static int ClothingStyleToIndex(ClothingStyle style)
        {
            switch (style)
            {
                case ClothingStyle.Traditional: return 1;
                case ClothingStyle.Winter:      return 2;
                default:                        return 0;
            }
        }

        // ── Color helpers ─────────────────────────────────────────────────────

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;
        }

        private static string FormatColor(Color color)
            => "#" + ColorUtility.ToHtmlStringRGBA(color);
    }
}

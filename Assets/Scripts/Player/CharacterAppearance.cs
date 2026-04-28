// Assets/Scripts/Player/CharacterAppearance.cs
using UnityEngine;

namespace DeenCraft.Player
{
    public enum SkinTone
    {
        Light,
        MediumLight,
        Medium,
        MediumDark,
        Dark
    }

    public enum HeadwearType
    {
        None,
        Hijab,
        Kufi
    }

    public enum ClothingStyle
    {
        Casual,
        Traditional,
        Winter
    }

    [System.Serializable]
    public class CharacterAppearance
    {
        public SkinTone      SkinTone      = SkinTone.Medium;
        public HeadwearType  HeadwearType  = HeadwearType.None;
        public ClothingStyle ClothingStyle = ClothingStyle.Casual;
        public Color         ClothingColor = Color.white;

        /// <summary>Returns the Unity Color matching this skin tone.</summary>
        public Color GetSkinColor()
        {
            switch (SkinTone)
            {
                case SkinTone.Light:       return new Color(1.00f, 0.88f, 0.77f);
                case SkinTone.MediumLight: return new Color(0.93f, 0.76f, 0.62f);
                case SkinTone.Medium:      return new Color(0.82f, 0.62f, 0.45f);
                case SkinTone.MediumDark:  return new Color(0.60f, 0.40f, 0.25f);
                case SkinTone.Dark:        return new Color(0.35f, 0.22f, 0.13f);
                default:                   return Color.white;
            }
        }
    }
}

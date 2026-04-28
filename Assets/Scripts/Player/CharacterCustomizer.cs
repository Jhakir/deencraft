// Assets/Scripts/Player/CharacterCustomizer.cs
using UnityEngine;

namespace DeenCraft.Player
{
    /// <summary>
    /// Applies CharacterAppearance to the player's renderers.
    /// Saves/loads via PlayerPrefs using JsonUtility.
    /// Attach to the Player root GameObject.
    /// </summary>
    public class CharacterCustomizer : MonoBehaviour
    {
        private const string PrefsKey = "PlayerAppearance";

        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _headwearRenderer;

        public CharacterAppearance Appearance { get; private set; } = new CharacterAppearance();

        private void Awake()
        {
            LoadFromPrefs();
        }

        /// <summary>Apply current Appearance to renderers.</summary>
        public void Apply()
        {
            if (_bodyRenderer != null)
                _bodyRenderer.material.color = Appearance.GetSkinColor();

            if (_headwearRenderer != null)
            {
                _headwearRenderer.enabled = Appearance.HeadwearType != HeadwearType.None;
                _headwearRenderer.material.color = Appearance.ClothingColor;
            }
        }

        public void SetAppearance(CharacterAppearance appearance)
        {
            Appearance = appearance;
            Apply();
        }

        public void SaveToPrefs()
        {
            string json = JsonUtility.ToJson(Appearance);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        public void LoadFromPrefs()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                string json = PlayerPrefs.GetString(PrefsKey);
                Appearance = JsonUtility.FromJson<CharacterAppearance>(json) ?? new CharacterAppearance();
            }
            Apply();
        }

        public bool HasSavedAppearance() => PlayerPrefs.HasKey(PrefsKey);
    }
}

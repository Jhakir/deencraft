// Assets/Scripts/UI/CharacterCustomizerPanel.cs
// UI panel for choosing skin tone, headwear, clothing style, and clothing colour.
// Attach to the CharacterCustomizer Canvas panel.
// Wire [SerializeField] slots in the Inspector.
using UnityEngine;
using UnityEngine.UI;
using DeenCraft.Auth;
using DeenCraft.Player;

namespace DeenCraft.UI
{
    /// <summary>
    /// Lets the player customise their character's appearance.
    /// Choices are previewed live via <see cref="CharacterCustomizer.SetAppearance"/>
    /// and saved to PlayerPrefs + the active child profile on confirm.
    /// </summary>
    public sealed class CharacterCustomizerPanel : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private CharacterCustomizer _characterCustomizer;

        // Skin-tone buttons (5): wire in Inspector order Light→Dark
        [SerializeField] private Button[] _skinToneButtons;

        // Headwear toggle buttons (3): None, Hijab, Kufi
        [SerializeField] private Button[] _headwearButtons;

        // Clothing style buttons (3): Casual, Traditional, Winter
        [SerializeField] private Button[] _clothingStyleButtons;

        // Colour swatches: each button's normal-color is used as the swatch colour
        [SerializeField] private Button[] _colourSwatchButtons;

        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        // ── State ────────────────────────────────────────────────────────────
        private CharacterAppearance _workingCopy;
        private CharacterAppearance _originalCopy;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            // Take a snapshot to restore on cancel
            _originalCopy = CloneAppearance(_characterCustomizer.Appearance);
            _workingCopy  = CloneAppearance(_originalCopy);
            Preview();
            WireButtons();
        }

        // ── Button wiring ─────────────────────────────────────────────────────

        private void WireButtons()
        {
            for (int i = 0; i < _skinToneButtons.Length; i++)
            {
                int captured = i;
                _skinToneButtons[i].onClick.RemoveAllListeners();
                _skinToneButtons[i].onClick.AddListener(() => SetSkinTone(captured));
            }

            for (int i = 0; i < _headwearButtons.Length; i++)
            {
                int captured = i;
                _headwearButtons[i].onClick.RemoveAllListeners();
                _headwearButtons[i].onClick.AddListener(() => SetHeadwear(captured));
            }

            for (int i = 0; i < _clothingStyleButtons.Length; i++)
            {
                int captured = i;
                _clothingStyleButtons[i].onClick.RemoveAllListeners();
                _clothingStyleButtons[i].onClick.AddListener(() => SetClothingStyle(captured));
            }

            for (int i = 0; i < _colourSwatchButtons.Length; i++)
            {
                int captured = i;
                _colourSwatchButtons[i].onClick.RemoveAllListeners();
                _colourSwatchButtons[i].onClick.AddListener(() => SetClothingColour(captured));
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(ApplyAndSave);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(CancelAndRevert);
            }
        }

        // ── Setter callbacks ─────────────────────────────────────────────────

        private void SetSkinTone(int index)
        {
            if (index < 0 || index >= System.Enum.GetValues(typeof(SkinTone)).Length) return;
            _workingCopy.SkinTone = (SkinTone)index;
            Preview();
        }

        private void SetHeadwear(int index)
        {
            if (index < 0 || index >= System.Enum.GetValues(typeof(HeadwearType)).Length) return;
            _workingCopy.HeadwearType = (HeadwearType)index;
            Preview();
        }

        private void SetClothingStyle(int index)
        {
            if (index < 0 || index >= System.Enum.GetValues(typeof(ClothingStyle)).Length) return;
            _workingCopy.ClothingStyle = (ClothingStyle)index;
            Preview();
        }

        private void SetClothingColour(int index)
        {
            if (index < 0 || index >= _colourSwatchButtons.Length) return;
            _workingCopy.ClothingColor = _colourSwatchButtons[index].colors.normalColor;
            Preview();
        }

        // ── Confirm / Cancel ──────────────────────────────────────────────────

        private void ApplyAndSave()
        {
            _characterCustomizer.SetAppearance(_workingCopy);
            _characterCustomizer.SaveToPrefs();

            // Persist into the active child profile if one is active
            if (SessionManager.IsChildActive)
            {
                var data = CharacterDataBridge.ToData(_workingCopy);
                SessionManager.ActiveChild.character = data;
            }

            gameObject.SetActive(false);
        }

        private void CancelAndRevert()
        {
            _characterCustomizer.SetAppearance(_originalCopy);
            gameObject.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Preview() => _characterCustomizer.SetAppearance(_workingCopy);

        private static CharacterAppearance CloneAppearance(CharacterAppearance source)
            => new CharacterAppearance
            {
                SkinTone     = source.SkinTone,
                HeadwearType = source.HeadwearType,
                ClothingStyle = source.ClothingStyle,
                ClothingColor = source.ClothingColor
            };
    }
}

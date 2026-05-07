// Assets/Scripts/UI/ChildProfilePickerPanel.cs
// Shows all child profiles for the active parent.
// Child taps profile → PIN entry if required → world select panel.
// Attach to the ChildProfilePicker Canvas panel.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeenCraft.Auth;
using DeenCraft.Auth.Models;

namespace DeenCraft.UI
{
    /// <summary>
    /// Lists child profiles under the signed-in parent account.
    /// On selection, prompts for PIN if the profile requires one,
    /// then calls <see cref="FirebaseAuthManager.ActivateChildAsync"/> and
    /// navigates to <see cref="_worldSelectPanel"/>.
    /// </summary>
    public sealed class ChildProfilePickerPanel : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private Transform  _profileContainer;      // ScrollView content
        [SerializeField] private GameObject _profileButtonPrefab;   // prefab with Button + Text
        [SerializeField] private GameObject _pinEntryPanel;         // sub-panel
        [SerializeField] private InputField _pinInputField;
        [SerializeField] private Text       _errorLabel;
        [SerializeField] private GameObject _worldSelectPanel;

        // ── State ────────────────────────────────────────────────────────────
        private List<ChildProfile> _profiles;
        private ChildProfile       _selectedProfile;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private async void OnEnable()
        {
            _errorLabel.text = "";
            _pinEntryPanel.SetActive(false);

            try
            {
                _profiles = await FirebaseAuthManager.Instance.LoadChildProfilesAsync();
            }
            catch (Exception ex)
            {
                _errorLabel.text = $"Could not load profiles: {ex.Message}";
                return;
            }

            BuildProfileButtons();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void BuildProfileButtons()
        {
            foreach (Transform child in _profileContainer)
                Destroy(child.gameObject);

            foreach (var profile in _profiles)
            {
                var go  = Instantiate(_profileButtonPrefab, _profileContainer);
                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = profile.username;

                var captured = profile;
                btn.onClick.AddListener(() => OnProfileSelected(captured));
            }
        }

        private void OnProfileSelected(ChildProfile profile)
        {
            _selectedProfile = profile;
            _errorLabel.text = "";

            if (!string.IsNullOrEmpty(profile.pinHash))
            {
                _pinInputField.text = "";
                _pinEntryPanel.SetActive(true);
            }
            else
            {
                ActivateProfileAsync("");
            }
        }

        // ── Button callbacks ─────────────────────────────────────────────────

        /// <summary>Called by the Confirm button inside _pinEntryPanel.</summary>
        public async void OnPinConfirmed()
        {
            if (_selectedProfile == null) return;
            _errorLabel.text = "";

            try
            {
                await FirebaseAuthManager.Instance.ActivateChildAsync(
                    _selectedProfile, _pinInputField.text);
                _pinEntryPanel.SetActive(false);
                ShowWorldSelect();
            }
            catch (UnauthorizedAccessException)
            {
                _errorLabel.text = "Incorrect PIN — try again.";
            }
            catch (Exception ex)
            {
                _errorLabel.text = $"Error: {ex.Message}";
            }
        }

        private async void ActivateProfileAsync(string pin)
        {
            try
            {
                await FirebaseAuthManager.Instance.ActivateChildAsync(_selectedProfile, pin);
                ShowWorldSelect();
            }
            catch (Exception ex)
            {
                _errorLabel.text = $"Error: {ex.Message}";
            }
        }

        private void ShowWorldSelect()
        {
            gameObject.SetActive(false);
            if (_worldSelectPanel != null) _worldSelectPanel.SetActive(true);
        }
    }
}

// Assets/Scripts/UI/WorldSelectPanel.cs
// Lists saved worlds for the active child profile plus a New World button.
// Attach to the WorldSelect Canvas panel.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeenCraft.Auth.Models;
using DeenCraft.Auth;
using DeenCraft.World;

namespace DeenCraft.UI
{
    /// <summary>
    /// Shows all world saves for the active child profile.
    /// Selecting a save loads it via <see cref="WorldSaveManager"/>.
    /// "New World" creates a fresh save.
    /// </summary>
    public sealed class WorldSelectPanel : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private Transform  _saveContainer;        // ScrollView content
        [SerializeField] private GameObject _saveButtonPrefab;     // prefab with Button + Text
        [SerializeField] private InputField _newWorldNameInput;
        [SerializeField] private Button     _createNewWorldButton;
        [SerializeField] private Text       _errorLabel;
        [SerializeField] private WorldSaveManager _worldSaveManager;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private async void OnEnable()
        {
            _errorLabel.text = "";
            if (_createNewWorldButton != null)
                _createNewWorldButton.interactable = true;

            List<WorldSaveData> saves;
            try
            {
                saves = await FirebaseAuthManager.Instance.ListWorldSavesAsync();
            }
            catch (Exception ex)
            {
                _errorLabel.text = $"Could not load saves: {ex.Message}";
                return;
            }

            BuildSaveButtons(saves);
        }

        // ── Button callbacks ─────────────────────────────────────────────────

        /// <summary>Called by the Create New World button.</summary>
        public async void OnCreateNewWorld()
        {
            string worldName = _newWorldNameInput != null
                ? _newWorldNameInput.text.Trim()
                : "";

            if (string.IsNullOrEmpty(worldName))
            {
                _errorLabel.text = "Please enter a world name.";
                return;
            }

            _errorLabel.text = "";
            if (_createNewWorldButton != null)
                _createNewWorldButton.interactable = false;

            try
            {
                await _worldSaveManager.NewWorldAsync(worldName);
                gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                _errorLabel.text = $"Error: {ex.Message}";
                if (_createNewWorldButton != null)
                    _createNewWorldButton.interactable = true;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void BuildSaveButtons(List<WorldSaveData> saves)
        {
            foreach (Transform child in _saveContainer)
                Destroy(child.gameObject);

            foreach (var save in saves)
            {
                var go  = Instantiate(_saveButtonPrefab, _saveContainer);
                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<Text>();
                if (lbl != null)
                    lbl.text = $"{save.worldName}\n<size=10>{save.lastSaved}</size>";

                var capturedId = save.id;
                btn.onClick.AddListener(() => OnSaveSelected(capturedId));
            }
        }

        private async void OnSaveSelected(string saveId)
        {
            _errorLabel.text = "";
            try
            {
                await _worldSaveManager.LoadWorldAsync(saveId);
                gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                _errorLabel.text = $"Error: {ex.Message}";
            }
        }
    }
}

// Assets/Scripts/UI/PauseMenuController.cs
// Toggles the pause overlay on Escape and exposes Resume / Save / SaveAndQuit / Settings.
// Attach to the PauseMenu Canvas panel (start with SetActive(false)).
using System;
using UnityEngine;
using DeenCraft.World;

namespace DeenCraft.UI
{
    /// <summary>
    /// Pause menu controller.  Sets <c>Time.timeScale</c> to 0 when open,
    /// 1 when closed.  Delegates world save operations to <see cref="WorldSaveManager"/>.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private WorldSaveManager _worldSaveManager;
        [SerializeField] private GameObject       _pausePanel;
        [SerializeField] private GameObject       _settingsPanel;     // the in-game settings panel
        [SerializeField] private UnityEngine.UI.Text _statusLabel;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Toggle()
        {
            bool willBeOpen = !_pausePanel.activeSelf;
            _pausePanel.SetActive(willBeOpen);
            Time.timeScale = willBeOpen ? 0f : 1f;
            if (_statusLabel != null) _statusLabel.text = "";
        }

        // ── Button callbacks ─────────────────────────────────────────────────

        public void OnResumePressed()
        {
            _pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public async void OnSavePressed()
        {
            if (_statusLabel != null) _statusLabel.text = "Saving…";
            try
            {
                await _worldSaveManager.SaveAsync();
                if (_statusLabel != null) _statusLabel.text = "Saved!";
            }
            catch (Exception ex)
            {
                if (_statusLabel != null) _statusLabel.text = $"Save failed: {ex.Message}";
            }
        }

        public async void OnSaveAndQuitPressed()
        {
            if (_statusLabel != null) _statusLabel.text = "Saving…";
            try
            {
                await _worldSaveManager.SaveAndQuitAsync();
                // Return to main menu — assumes the main menu Canvas is a sibling
                // and the developer loads the main-menu scene here in production.
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            catch (Exception ex)
            {
                if (_statusLabel != null) _statusLabel.text = $"Error: {ex.Message}";
            }
        }

        public void OnSettingsPressed()
        {
            _pausePanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }
    }
}

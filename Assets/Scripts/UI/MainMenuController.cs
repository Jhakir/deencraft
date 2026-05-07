// Assets/Scripts/UI/MainMenuController.cs
// Controls the top-level main menu: logo, Play, Settings, Credits.
// Attach to the MainMenu Canvas root GameObject.
// Wire all panel references in the Inspector.
using UnityEngine;

namespace DeenCraft.UI
{
    /// <summary>
    /// Manages the main menu panel and navigates to child panels on button press.
    /// Panels are activated/deactivated via SetActive — no scene loading needed
    /// for the menu flow (all UI lives in the same scene as the game).
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _childProfilePanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _creditsPanel;

        private void Awake()
        {
            ShowMainMenu();
        }

        // ── Button callbacks (wire in Inspector via onClick) ─────────────────

        public void OnPlayPressed()
        {
            _mainMenuPanel.SetActive(false);
            _childProfilePanel.SetActive(true);
        }

        public void OnSettingsPressed()
        {
            _mainMenuPanel.SetActive(false);
            _settingsPanel.SetActive(true);
        }

        public void OnCreditsPressed()
        {
            _mainMenuPanel.SetActive(false);
            _creditsPanel.SetActive(true);
        }

        // ── Navigation helpers ────────────────────────────────────────────────

        /// <summary>Returns to the main menu panel, hiding all others.</summary>
        public void ShowMainMenu()
        {
            _mainMenuPanel.SetActive(true);
            if (_childProfilePanel != null) _childProfilePanel.SetActive(false);
            if (_settingsPanel     != null) _settingsPanel.SetActive(false);
            if (_creditsPanel      != null) _creditsPanel.SetActive(false);
        }
    }
}

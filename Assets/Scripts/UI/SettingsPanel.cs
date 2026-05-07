// Assets/Scripts/UI/SettingsPanel.cs
// In-game Settings overlay: music volume, SFX volume, Adhan toggle, graphics quality.
// Attach to the Settings Canvas panel.
// Requires an AudioMixer named "MasterMixer" with exposed parameters
//   "MusicVolume" and "SFXVolume" (each accepts dB values).
using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using DeenCraft.World;

namespace DeenCraft.UI
{
    /// <summary>
    /// Bridges <see cref="SettingsModel"/> to Unity audio/graphics APIs.
    ///
    /// Sliders drive volume via the AudioMixer using dB conversion:
    ///   dB = Mathf.Log10(linearValue) * 20f  (never set linear values on a Mixer)
    ///
    /// Adhan toggle delegates to <see cref="AdhanAudioManager"/>.
    /// Graphics Dropdown sets <see cref="QualitySettings.SetQualityLevel"/>.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private AudioMixer  _audioMixer;
        [SerializeField] private Slider      _musicSlider;
        [SerializeField] private Slider      _sfxSlider;
        [SerializeField] private Toggle      _adhanToggle;
        [SerializeField] private Dropdown    _graphicsDropdown;
        [SerializeField] private GameObject  _callerPanel;          // panel that opened settings

        private const string MixerMusicParam = "MusicVolume";
        private const string MixerSfxParam   = "SFXVolume";
        private const float  MinDb           = -80f; // silence threshold

        private SettingsModel _model;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            _model = new SettingsModel();
            _model.Load();

            SyncSlidersToModel();
            SyncAdhanToggle();
            SyncGraphicsDropdown();

            WireListeners();
        }

        private void OnDisable()
        {
            _model?.Save();
        }

        // ── Listener wiring ───────────────────────────────────────────────────

        private void WireListeners()
        {
            if (_musicSlider    != null) { _musicSlider.onValueChanged.RemoveAllListeners();    _musicSlider.onValueChanged.AddListener(OnMusicChanged); }
            if (_sfxSlider      != null) { _sfxSlider.onValueChanged.RemoveAllListeners();      _sfxSlider.onValueChanged.AddListener(OnSfxChanged);   }
            if (_adhanToggle    != null) { _adhanToggle.onValueChanged.RemoveAllListeners();    _adhanToggle.onValueChanged.AddListener(OnAdhanToggled);}
            if (_graphicsDropdown != null) { _graphicsDropdown.onValueChanged.RemoveAllListeners(); _graphicsDropdown.onValueChanged.AddListener(OnGraphicsChanged); }
        }

        // ── Sync helpers ──────────────────────────────────────────────────────

        private void SyncSlidersToModel()
        {
            if (_musicSlider != null) _musicSlider.value = _model.MusicVolume;
            if (_sfxSlider   != null) _sfxSlider.value   = _model.SfxVolume;
        }

        private void SyncAdhanToggle()
        {
            if (_adhanToggle == null || AdhanAudioManager.Instance == null) return;
            _adhanToggle.isOn = AdhanAudioManager.Instance.IsEnabled;
        }

        private void SyncGraphicsDropdown()
        {
            if (_graphicsDropdown == null) return;
            _graphicsDropdown.value = Mathf.Clamp(_model.GraphicsQuality, 0, _graphicsDropdown.options.Count - 1);
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnMusicChanged(float value)
        {
            _model.MusicVolume = value;
            SetMixerVolume(MixerMusicParam, value);
        }

        private void OnSfxChanged(float value)
        {
            _model.SfxVolume = value;
            SetMixerVolume(MixerSfxParam, value);
        }

        private void OnAdhanToggled(bool isOn)
        {
            if (AdhanAudioManager.Instance != null)
                AdhanAudioManager.Instance.SetEnabled(isOn);
        }

        private void OnGraphicsChanged(int index)
        {
            _model.GraphicsQuality = index;
            QualitySettings.SetQualityLevel(index, true);
        }

        /// <summary>Back button — also saves settings and re-opens caller panel.</summary>
        public void OnBackPressed()
        {
            _model.Save();
            gameObject.SetActive(false);
            if (_callerPanel != null) _callerPanel.SetActive(true);
        }

        // ── AudioMixer helper ─────────────────────────────────────────────────

        private void SetMixerVolume(string param, float linear)
        {
            if (_audioMixer == null) return;
            float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : MinDb;
            _audioMixer.SetFloat(param, db);
        }
    }
}

// Assets/Scripts/UI/SettingsModel.cs
// Pure-C# settings storage. No MonoBehaviour — fully testable in EditMode.
// Reads/writes PlayerPrefs with "dc_" namespace prefix.
using UnityEngine;

namespace DeenCraft.UI
{
    /// <summary>
    /// Holds user-facing settings (volume, graphics quality).
    /// Adhan toggle is NOT stored here — it lives in AdhanAudioManager/PlayerPrefs
    /// via AdhanAudioManager.SetEnabled() which owns that key.
    ///
    /// Call <see cref="Load"/> on startup to restore persisted values.
    /// Call <see cref="Save"/> after any change (or on panel close).
    /// </summary>
    public sealed class SettingsModel
    {
        // ── PlayerPrefs keys ─────────────────────────────────────────────────
        private const string KeyMusicVol     = "dc_music_vol";
        private const string KeySfxVol       = "dc_sfx_vol";
        private const string KeyGraphicsQual = "dc_graphics_quality";

        // ── Defaults ─────────────────────────────────────────────────────────
        private const float DefaultMusicVol     = 0.8f;
        private const float DefaultSfxVol       = 1.0f;
        private const int   DefaultGraphicsQual = 1;   // 0=Low, 1=Medium, 2=High

        // ── Properties ───────────────────────────────────────────────────────
        private float _musicVolume = DefaultMusicVol;
        private float _sfxVolume   = DefaultSfxVol;

        public float MusicVolume
        {
            get => _musicVolume;
            set => _musicVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public int GraphicsQuality { get; set; } = DefaultGraphicsQual;

        // ── Lifecycle ────────────────────────────────────────────────────────

        public SettingsModel()
        {
            // Defaults applied above — call Load() to restore persisted values.
        }

        /// <summary>Restores settings from PlayerPrefs. Call on panel open.</summary>
        public void Load()
        {
            MusicVolume     = PlayerPrefs.GetFloat(KeyMusicVol,     DefaultMusicVol);
            SfxVolume       = PlayerPrefs.GetFloat(KeySfxVol,       DefaultSfxVol);
            GraphicsQuality = PlayerPrefs.GetInt(  KeyGraphicsQual, DefaultGraphicsQual);
        }

        /// <summary>Persists current settings to PlayerPrefs. Call on panel close or change.</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(KeyMusicVol,     _musicVolume);
            PlayerPrefs.SetFloat(KeySfxVol,       _sfxVolume);
            PlayerPrefs.SetInt(  KeyGraphicsQual, GraphicsQuality);
            PlayerPrefs.Save();
        }
    }
}

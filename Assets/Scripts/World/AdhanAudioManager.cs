// Assets/Scripts/World/AdhanAudioManager.cs
// Plays an Adhan audio clip at each of the five daily prayer times.
// Attach to a persistent GameObject in the same scene as DayNightCycle.
//
// HOW TO ADD YOUR OWN VOICE RECORDING:
//   1. Record your Adhan in Voice Memos / GarageBand on Mac.
//   2. Export as .mp3 or .wav.
//   3. Drag the file into Assets/Audio/ in the Unity Project window.
//   4. In the Inspector for this component, expand "Adhan Clips" and increase
//      the size by 1, then drag your clip into the new slot.
//   5. In the parent settings screen, the clip-index selector will show the new option.
using UnityEngine;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// Listens to <see cref="DayNightCycle.OnPrayerTime"/> and plays the
    /// parent-selected Adhan clip (toggle + clip choice persisted via PlayerPrefs).
    ///
    /// Multiple clips are supported: assign them in the Inspector under
    /// <c>_adhanClips</c>.  The parent selects which clip to use via
    /// <see cref="SetClipIndex"/> (wired to the settings UI in Phase 8).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AdhanAudioManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Tooltip("Assign one or more Adhan recordings here. Parents can pick between them.\n" +
                 "Slot 0 = default clip. Add extra slots for custom voice recordings.")]
        [SerializeField] private AudioClip[] _adhanClips = new AudioClip[0];

        // ── State ─────────────────────────────────────────────────────────────
        private AudioSource  _audioSource;
        private bool         _enabled;
        private int          _clipIndex;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _audioSource           = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop        = false;
            _audioSource.volume      = GameConstants.AdhanVolume;

            LoadPrefs();
        }

        private void OnEnable()
        {
            if (DayNightCycle.Instance != null)
                DayNightCycle.Instance.OnPrayerTime += HandlePrayerTime;
        }

        private void OnDisable()
        {
            if (DayNightCycle.Instance != null)
                DayNightCycle.Instance.OnPrayerTime -= HandlePrayerTime;
        }

        // ── Public API (called from settings UI in Phase 8) ───────────────────

        /// <summary>
        /// Enable or disable the Adhan. Setting is saved immediately to PlayerPrefs.
        /// Call this from the parent settings toggle.
        /// </summary>
        public void SetEnabled(bool isEnabled)
        {
            _enabled = isEnabled;
            PlayerPrefs.SetInt(GameConstants.PrefsAdhanEnabledKey, isEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!isEnabled && _audioSource.isPlaying)
                _audioSource.Stop();
        }

        /// <summary>
        /// Select which clip to use (0 = default, 1+ = custom recordings).
        /// Setting is saved to PlayerPrefs. Call this from the parent settings UI.
        /// </summary>
        public void SetClipIndex(int index)
        {
            if (_adhanClips == null || _adhanClips.Length == 0) return;
            _clipIndex = Mathf.Clamp(index, 0, _adhanClips.Length - 1);
            PlayerPrefs.SetInt(GameConstants.PrefsAdhanClipIndexKey, _clipIndex);
            PlayerPrefs.Save();
        }

        /// <returns>True if the Adhan is currently enabled.</returns>
        public bool IsEnabled => _enabled;

        /// <returns>The index of the currently selected clip.</returns>
        public int ClipIndex => _clipIndex;

        /// <returns>Number of available clips (for building the UI dropdown).</returns>
        public int ClipCount => _adhanClips != null ? _adhanClips.Length : 0;

        // ── Private helpers ───────────────────────────────────────────────────
        private void HandlePrayerTime(PrayerName prayer)
        {
            if (!_enabled) return;
            if (_adhanClips == null || _adhanClips.Length == 0) return;

            AudioClip clip = _adhanClips[_clipIndex];
            if (clip == null) return;

            _audioSource.clip = clip;
            _audioSource.Play();

            Debug.Log($"[AdhanAudioManager] Playing Adhan for {prayer} (clip index {_clipIndex}: {clip.name})");
        }

        private void LoadPrefs()
        {
            _enabled   = PlayerPrefs.GetInt(GameConstants.PrefsAdhanEnabledKey, 1) == 1;
            _clipIndex = PlayerPrefs.GetInt(GameConstants.PrefsAdhanClipIndexKey, 0);

            if (_adhanClips != null && _adhanClips.Length > 0)
                _clipIndex = Mathf.Clamp(_clipIndex, 0, _adhanClips.Length - 1);
        }
    }
}

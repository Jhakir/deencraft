// Assets/Scripts/World/DayNightCycle.cs
// Tracks in-game time and fires Unity events at the five Islamic prayer times.
// Attach to a persistent GameObject in the scene (same one as ChunkManager works).
// Singleton — one instance per scene.
using System;
using UnityEngine;
using UnityEngine.Events;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// Advances in-game time and invokes <see cref="OnPrayerTime"/> once per
    /// in-game day at each of the five prayer-time thresholds.
    ///
    /// One full in-game day = <see cref="GameConstants.DayLengthSeconds"/> real seconds.
    /// NormalizedTime runs from 0 (midnight) to 1 (next midnight).
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static DayNightCycle Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        /// <summary>Start time as a fraction of a day (0–1). 0.25 ≈ 6 AM.</summary>
        [SerializeField] [Range(0f, 1f)] private float _startingTime = 0.25f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired once per in-game day at each prayer time.
        /// The <see cref="PrayerName"/> string identifies which prayer triggered the event.
        /// </summary>
        public event Action<PrayerName> OnPrayerTime;

        // ── State ─────────────────────────────────────────────────────────────
        /// <summary>Current time as a fraction of a full day (0 = midnight, 1 = next midnight).</summary>
        public float NormalizedTime { get; private set; }

        // Track whether each prayer has fired this cycle so it only fires once.
        private bool _fajrFired;
        private bool _dhuhrFired;
        private bool _asrFired;
        private bool _maghribFired;
        private bool _ishaFired;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            NormalizedTime = _startingTime;
        }

        private void Update()
        {
            NormalizedTime += Time.deltaTime / GameConstants.DayLengthSeconds;

            if (NormalizedTime >= 1f)
            {
                // New day — reset all prayer flags
                NormalizedTime -= 1f;
                _fajrFired = _dhuhrFired = _asrFired = _maghribFired = _ishaFired = false;
            }

            CheckPrayer(GameConstants.PrayerTimeFajr,    PrayerName.Fajr,    ref _fajrFired);
            CheckPrayer(GameConstants.PrayerTimeDhuhr,   PrayerName.Dhuhr,   ref _dhuhrFired);
            CheckPrayer(GameConstants.PrayerTimeAsr,     PrayerName.Asr,     ref _asrFired);
            CheckPrayer(GameConstants.PrayerTimeMaghrib, PrayerName.Maghrib, ref _maghribFired);
            CheckPrayer(GameConstants.PrayerTimeIsha,    PrayerName.Isha,    ref _ishaFired);
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>
        /// Jump to a specific time fraction. Useful for testing and settings preview.
        /// </summary>
        public void SetTime(float normalizedTime)
        {
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            // Reset prayer flags based on new position so prayers fire correctly going forward.
            _fajrFired    = NormalizedTime > GameConstants.PrayerTimeFajr;
            _dhuhrFired   = NormalizedTime > GameConstants.PrayerTimeDhuhr;
            _asrFired     = NormalizedTime > GameConstants.PrayerTimeAsr;
            _maghribFired = NormalizedTime > GameConstants.PrayerTimeMaghrib;
            _ishaFired    = NormalizedTime > GameConstants.PrayerTimeIsha;
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private void CheckPrayer(float threshold, PrayerName prayer, ref bool fired)
        {
            if (!fired && NormalizedTime >= threshold)
            {
                fired = true;
                OnPrayerTime?.Invoke(prayer);
            }
        }
    }

    /// <summary>The five daily Islamic prayer times.</summary>
    public enum PrayerName
    {
        Fajr    = 0,
        Dhuhr   = 1,
        Asr     = 2,
        Maghrib = 3,
        Isha    = 4,
    }
}

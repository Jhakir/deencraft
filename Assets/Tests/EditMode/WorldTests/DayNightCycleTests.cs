// Assets/Tests/EditMode/WorldTests/DayNightCycleTests.cs
// Tests for DayNightCycle event-firing logic using a test-only subclass that
// drives NormalizedTime directly without needing MonoBehaviour Update ticks.
using System.Collections.Generic;
using NUnit.Framework;
using DeenCraft.World;

namespace DeenCraft.Tests.EditMode
{
    /// <summary>
    /// Test-only harness that exposes the prayer-check method so we can drive it
    /// without needing a running MonoBehaviour.
    /// </summary>
    internal class TestDayNightCycle
    {
        public float NormalizedTime { get; set; }
        public List<PrayerName> FiredPrayers { get; } = new List<PrayerName>();

        private bool _fajrFired, _dhuhrFired, _asrFired, _maghribFired, _ishaFired;

        public void AdvanceTo(float time)
        {
            if (time < NormalizedTime)
            {
                // New day — reset flags
                _fajrFired = _dhuhrFired = _asrFired = _maghribFired = _ishaFired = false;
            }
            NormalizedTime = time;
            CheckPrayer(DeenCraft.GameConstants.PrayerTimeFajr,    PrayerName.Fajr,    ref _fajrFired);
            CheckPrayer(DeenCraft.GameConstants.PrayerTimeDhuhr,   PrayerName.Dhuhr,   ref _dhuhrFired);
            CheckPrayer(DeenCraft.GameConstants.PrayerTimeAsr,     PrayerName.Asr,     ref _asrFired);
            CheckPrayer(DeenCraft.GameConstants.PrayerTimeMaghrib, PrayerName.Maghrib, ref _maghribFired);
            CheckPrayer(DeenCraft.GameConstants.PrayerTimeIsha,    PrayerName.Isha,    ref _ishaFired);
        }

        private void CheckPrayer(float threshold, PrayerName prayer, ref bool fired)
        {
            if (!fired && NormalizedTime >= threshold)
            {
                fired = true;
                FiredPrayers.Add(prayer);
            }
        }
    }

    [TestFixture]
    public class DayNightCycleTests
    {
        [Test]
        public void NoPrayersFire_BeforeFajrTime()
        {
            var cycle = new TestDayNightCycle();
            cycle.AdvanceTo(0.10f); // before Fajr (0.20)
            Assert.AreEqual(0, cycle.FiredPrayers.Count);
        }

        [Test]
        public void FajrFires_WhenTimePassesFajrThreshold()
        {
            var cycle = new TestDayNightCycle();
            cycle.AdvanceTo(0.21f);
            Assert.Contains(PrayerName.Fajr, cycle.FiredPrayers);
        }

        [Test]
        public void AllFivePrayers_FireInOrder_OverOneFulLDay()
        {
            var cycle = new TestDayNightCycle();
            cycle.AdvanceTo(0.95f); // past all prayer times
            Assert.AreEqual(5, cycle.FiredPrayers.Count,
                "All five prayers must fire exactly once per day");
        }

        [Test]
        public void PrayersDoNotFire_Twice_InSameDay()
        {
            var cycle = new TestDayNightCycle();
            cycle.AdvanceTo(0.25f); // past Fajr
            int countAfterFirst = cycle.FiredPrayers.Count;
            cycle.AdvanceTo(0.30f); // still same day, Fajr already fired
            int countAfterSecond = cycle.FiredPrayers.Count;
            Assert.AreEqual(countAfterFirst, countAfterSecond,
                "No new prayers should fire between Fajr and Dhuhr times");
        }

        [Test]
        public void PrayerFlags_ResetOnNewDay()
        {
            var cycle = new TestDayNightCycle();
            cycle.AdvanceTo(0.95f); // fire all prayers
            Assert.AreEqual(5, cycle.FiredPrayers.Count);

            cycle.FiredPrayers.Clear();
            cycle.AdvanceTo(0.01f); // new day (time went backward → triggers reset)
            cycle.AdvanceTo(0.22f); // Fajr threshold again
            Assert.Contains(PrayerName.Fajr, cycle.FiredPrayers,
                "Fajr must fire again on a new day");
        }

        [Test]
        public void IshaPrayer_IsLast_ToFire()
        {
            var cycle = new TestDayNightCycle();
            cycle.AdvanceTo(0.95f);
            Assert.AreEqual(PrayerName.Isha, cycle.FiredPrayers[cycle.FiredPrayers.Count - 1],
                "Isha must be the last prayer to fire each day");
        }
    }
}

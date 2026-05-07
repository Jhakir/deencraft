// Assets/Scripts/UI/HudViewModel.cs
// Pure-C# helpers that convert raw VitalityData values into HUD-ready display values.
// No MonoBehaviour — fully testable in EditMode.
using UnityEngine;
using DeenCraft;

namespace DeenCraft.UI
{
    /// <summary>
    /// Converts raw game data into HUD display values.
    /// All methods are pure functions — no state.
    /// </summary>
    public static class HudViewModel
    {
        // ── Health ────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts health (0–MaxHealth) to a whole heart count (0–10).
        /// Two health points per heart; partial hearts are floored.
        /// </summary>
        public static int HealthToHearts(float health)
        {
            float clamped = Mathf.Clamp(health, 0f, GameConstants.MaxHealth);
            return Mathf.FloorToInt(clamped / 2f);
        }

        // ── Hunger ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns hunger as a 0–1 fraction suitable for setting Image.fillAmount.
        /// </summary>
        public static float HungerFraction(float hunger)
            => Mathf.Clamp01(hunger / GameConstants.MaxHunger);

        // ── Day / Time ────────────────────────────────────────────────────────

        /// <summary>
        /// Converts normalizedTime (0 = midnight → 1 = next midnight) to a
        /// short kid-friendly time-of-day label.
        ///
        ///   0.000 – 0.125  Night
        ///   0.125 – 0.375  Morning
        ///   0.375 – 0.625  Midday
        ///   0.625 – 0.875  Evening
        ///   0.875 – 1.000  Night
        /// </summary>
        public static string TimeLabel(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            if (t < 0.125f || t >= 0.875f) return "Night";
            if (t < 0.375f)                return "Morning";
            if (t < 0.625f)                return "Midday";
            return "Evening";
        }
    }
}

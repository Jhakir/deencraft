// Assets/Scripts/UI/HudController.cs
// Polls VitalitySystem and Inventory each frame and updates all HUD elements.
// Attach to the HUD Canvas root GameObject.
// Wire all [SerializeField] slots in the Inspector.
using UnityEngine;
using UnityEngine.UI;
using DeenCraft.Player;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.UI
{
    /// <summary>
    /// Updates the in-game HUD every frame:
    /// health hearts, hunger bar fill, hotbar slot icons, day/time label.
    ///
    /// Attach to the HUD root CanvasGroup. Wire Inspector references.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        // ── Data sources ─────────────────────────────────────────────────────
        [SerializeField] private VitalitySystem  _vitalitySystem;
        [SerializeField] private InventoryHolder _inventoryHolder;

        // ── Heart display ─────────────────────────────────────────────────────
        /// <summary>
        /// Array of 10 heart Image components, left-to-right.
        /// Each is set to _heartFull or _heartEmpty every frame.
        /// </summary>
        [SerializeField] private Image[] _heartIcons;
        [SerializeField] private Sprite  _heartFull;
        [SerializeField] private Sprite  _heartEmpty;

        // ── Hunger bar ────────────────────────────────────────────────────────
        /// <summary>Image with fill method set to Horizontal. fillAmount = hunger fraction.</summary>
        [SerializeField] private Image _hungerFill;

        // ── Hotbar ────────────────────────────────────────────────────────────
        [SerializeField] private HotbarSlotUI[] _hotbarSlots;    // 9 slots
        [SerializeField] private RectTransform  _hotbarSelector; // slides to active slot

        // ── Time label ────────────────────────────────────────────────────────
        [SerializeField] private Text _timeLabel;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (_vitalitySystem != null)
            {
                UpdateHearts(_vitalitySystem.Health);
                UpdateHunger(_vitalitySystem.Hunger);
            }

            if (_inventoryHolder != null)
                UpdateHotbar(_inventoryHolder.Inventory);

            UpdateTimeLabel();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void UpdateHearts(float health)
        {
            int filledHearts = HudViewModel.HealthToHearts(health);
            for (int i = 0; i < _heartIcons.Length; i++)
            {
                if (_heartIcons[i] == null) continue;
                _heartIcons[i].sprite = i < filledHearts ? _heartFull : _heartEmpty;
            }
        }

        private void UpdateHunger(float hunger)
        {
            if (_hungerFill == null) return;
            _hungerFill.fillAmount = HudViewModel.HungerFraction(hunger);
        }

        private void UpdateHotbar(Inventory inventory)
        {
            for (int i = 0; i < _hotbarSlots.Length; i++)
            {
                if (_hotbarSlots[i] == null) continue;
                _hotbarSlots[i].Refresh(inventory.GetHotbarSlot(i));
            }

            // Slide selector highlight to the active slot
            if (_hotbarSelector != null && _hotbarSlots.Length > 0)
            {
                float totalWidth = _hotbarSelector.parent.GetComponent<RectTransform>().rect.width;
                float slotWidth  = totalWidth / GameConstants.HotbarSlots;
                _hotbarSelector.anchoredPosition = new Vector2(
                    inventory.SelectedSlot * slotWidth, _hotbarSelector.anchoredPosition.y);
            }
        }

        private void UpdateTimeLabel()
        {
            if (_timeLabel == null || DayNightCycle.Instance == null) return;
            _timeLabel.text = HudViewModel.TimeLabel(DayNightCycle.Instance.NormalizedTime);
        }
    }
}

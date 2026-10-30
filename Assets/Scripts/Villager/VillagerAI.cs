// Assets/Scripts/Villager/VillagerAI.cs
using UnityEngine;
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Villager
{
    /// <summary>
    /// Minimal villager behaviour:
    ///   - Stands at spawn point.
    ///   - Slowly rotates to face the player when within detection range.
    ///   - Implements <see cref="IInteractable"/> so pressing E opens the trade UI.
    ///     (Phase 5: logs interaction; full trade UI wired in Phase 8.)
    ///
    /// Assign a <see cref="VillagerProfile"/> at start; if none is supplied a
    /// default one is generated automatically.
    /// </summary>
    public class VillagerAI : MonoBehaviour, IInteractable
    {
        public static event System.Action<VillagerAI, GameObject> TradeRequested;
        // ── Constants ─────────────────────────────────────────────────────────
        private const float DetectionRange = 10f;
        private const float TurnSpeed      =  2f;  // Slerp factor per second

        // ── Runtime ───────────────────────────────────────────────────────────
        private Transform       _player;
        private VillagerProfile _profile;

        public VillagerProfile Profile => _profile;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            var playerCtrl = FindObjectOfType<PlayerController>();
            if (playerCtrl != null)
                _player = playerCtrl.transform;

            if (_profile == null)
                _profile = VillagerProfile.CreateDefault();
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= DetectionRange)
                FacePlayer();
        }

        // ── IInteractable ─────────────────────────────────────────────────────
        public void Interact(GameObject interactor)
        {
            if (interactor == null || interactor.GetComponent<InventoryHolder>() == null)
            {
                Debug.LogWarning("[VillagerAI] Trading requires a player inventory.", this);
                return;
            }
            if (_profile == null) _profile = VillagerProfile.CreateDefault();
            if (TradeRequested == null)
            {
                Debug.LogError("[VillagerAI] No trade menu is listening for interactions.", this);
                return;
            }
            TradeRequested.Invoke(this, interactor);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Assign a profile from a spawner or designer script.</summary>
        public void SetProfile(VillagerProfile profile) => _profile = profile;

        /// <summary>
        /// Attempt a trade directly (called from trade UI panels in Phase 8).
        /// Returns true on success.
        /// </summary>
        public bool ExecuteTrade(int tradeIndex, Inventory playerInventory)
        {
            if (_profile == null || playerInventory == null)    return false;
            if (tradeIndex < 0 || tradeIndex >= _profile.Trades.Length) return false;

            return TradeSession.Execute(_profile.Trades[tradeIndex], playerInventory);
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private void FacePlayer()
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, TurnSpeed * Time.deltaTime);
        }
    }
}

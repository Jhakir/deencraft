// Assets/Scripts/Animals/BoatController.cs
using UnityEngine;
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Animals
{
    /// <summary>
    /// A boat entity that floats on water and can be steered by a player.
    /// Place a BoatController prefab near or on the water surface.
    /// Implements <see cref="IInteractable"/> so players can board by pressing E.
    ///
    /// The boat stays at <c>_waterSurfaceY</c> via a soft lerp (buoyancy effect).
    /// While piloted, player locomotion is disabled and WASD steers the boat.
    /// Press E again to disembark.
    /// </summary>
    public class BoatController : MonoBehaviour, IInteractable
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        /// <summary>Y coordinate of the water surface; should match world-gen river Y.</summary>
        [SerializeField] private float _waterSurfaceY = 63.5f;

        // ── Constants ─────────────────────────────────────────────────────────
        private const float FloatLerp      = 5f;    // buoyancy lerp speed
        private const float SteerSpeed     = 5f;    // forward/backward units/s
        private const float TurnDegPerSec  = 90f;   // yaw degrees/s
        private const float PilotOffsetY   = 0.6f;  // pilot sits this high above boat origin

        // ── Runtime ───────────────────────────────────────────────────────────
        private PlayerController _pilot;
        private float            _yaw;

        public bool HasPilot => _pilot != null;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            ApplyBuoyancy();
            if (HasPilot)
                HandlePilotInput();
        }

        // ── IInteractable ─────────────────────────────────────────────────────
        public void Interact(GameObject interactor)
        {
            var player = interactor.GetComponent<PlayerController>();
            if (player != null && !HasPilot)
                Board(player);
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Board the boat, disabling the player's own locomotion.</summary>
        public void Board(PlayerController player)
        {
            if (HasPilot || player == null) return;

            _pilot = player;
            _pilot.enabled = false;
            _pilot.transform.SetParent(transform);
            _pilot.transform.localPosition = new Vector3(0f, PilotOffsetY, 0f);
            _pilot.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Disembark the current pilot, restoring their locomotion.</summary>
        public void Disembark()
        {
            if (!HasPilot) return;

            _pilot.transform.SetParent(null);
            _pilot.transform.position = transform.position + transform.right * 2f;
            _pilot.enabled = true;
            _pilot = null;
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private void ApplyBuoyancy()
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, _waterSurfaceY, FloatLerp * Time.deltaTime);
            transform.position = pos;
        }

        private void HandlePilotInput()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            _yaw += h * TurnDegPerSec * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            Vector3 forward = transform.forward * (v * SteerSpeed * Time.deltaTime);
            transform.position += new Vector3(forward.x, 0f, forward.z);

            if (Input.GetKeyDown(KeyCode.E))
                Disembark();
        }
    }
}

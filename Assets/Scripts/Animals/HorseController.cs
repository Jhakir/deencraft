// Assets/Scripts/Animals/HorseController.cs
using UnityEngine;
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Animals
{
    /// <summary>
    /// Adds rideable behaviour to a horse GameObject.
    /// Requires an <see cref="AnimalController"/> on the same object.
    /// Implements <see cref="IInteractable"/> so players can mount by pressing E.
    ///
    /// While mounted:
    ///   - Player's PlayerController is disabled (no independent locomotion).
    ///   - Player transform is parented to the optional _mountPoint (or transform).
    ///   - WASD controls the horse; E dismounts.
    /// </summary>
    [RequireComponent(typeof(AnimalController))]
    [RequireComponent(typeof(CharacterController))]
    public class HorseController : MonoBehaviour, IInteractable
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        /// <summary>Optional transform the rider sits at; defaults to horse root.</summary>
        [SerializeField] private Transform _mountPoint;

        // ── Constants ─────────────────────────────────────────────────────────
        private const float RiderOffsetY  = 1.4f;  // rider sits above horse origin
        private const float Gravity       = -20f;
        private const float GroundedBump  = -2f;
        private const float TurnSpeed     = 8f;    // Quaternion.Slerp speed

        // ── Runtime ───────────────────────────────────────────────────────────
        private CharacterController _cc;
        private AnimalController    _animal;
        private PlayerController    _rider;
        private float               _verticalVelocity;

        public bool IsRidden => _rider != null;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _cc     = GetComponent<CharacterController>();
            _animal = GetComponent<AnimalController>();
        }

        private void Update()
        {
            if (!IsRidden) return;
            HandleRiderInput();
        }

        // ── IInteractable ─────────────────────────────────────────────────────
        public void Interact(GameObject interactor)
        {
            var player = interactor.GetComponent<PlayerController>();
            if (player != null && !IsRidden)
                Mount(player);
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Mount the horse, disabling the player's own locomotion.</summary>
        public void Mount(PlayerController player)
        {
            if (IsRidden || player == null) return;

            _rider = player;
            _rider.enabled = false; // hand control to HorseController

            Transform seatPoint = _mountPoint != null ? _mountPoint : transform;
            _rider.transform.SetParent(seatPoint);
            _rider.transform.localPosition = new Vector3(0f, RiderOffsetY, 0f);
            _rider.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Dismount the current rider, restoring their locomotion.</summary>
        public void Dismount()
        {
            if (!IsRidden) return;

            _rider.transform.SetParent(null);
            // Place rider to the side of the horse to avoid overlap
            _rider.transform.position = transform.position + transform.right * 1.5f;
            _rider.enabled = true;
            _rider = null;
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private void HandleRiderInput()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 dir = (transform.forward * v + transform.right * h);
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    TurnSpeed * Time.deltaTime);
            }

            // Gravity
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = GroundedBump;
            _verticalVelocity += Gravity * Time.deltaTime;

            float moveSpeed = dir.sqrMagnitude > 0.01f ? GameConstants.HorseRideSpeed : 0f;
            Vector3 velocity = dir.normalized * moveSpeed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);

            // Dismount
            if (Input.GetKeyDown(KeyCode.E))
                Dismount();
        }
    }
}

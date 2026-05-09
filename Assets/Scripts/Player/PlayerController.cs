// Assets/Scripts/Player/PlayerController.cs
using UnityEngine;
using DeenCraft;
using DeenCraft.World;

namespace DeenCraft.Player
{
    /// <summary>
    /// Third-person CharacterController locomotion.
    /// - WASD: move in yaw-relative direction
    /// - Mouse X: rotate player body (yaw)
    /// - Mouse Y: tilt third-person camera (pitch)
    /// - Space: jump (when grounded or swimming up)
    /// - Left Shift: sprint
    /// - Left Ctrl while swimming: swim down
    /// - Water block at chest height triggers swim mode
    /// 
    /// Camera child object named "PlayerCamera" is positioned at (0, 1.6, -4).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private Transform _cameraTarget; // "PlayerCamera" child

        // ── Constants ────────────────────────────────────────────────────────
        private const float Gravity        = -20f;
        private const float SwimGravity    = -2f;
        private const float GroundedBump   = -2f;
        private const float MinPitch       = -80f;
        private const float MaxPitch       =  60f;

        // ── State ─────────────────────────────────────────────────────────────
        private CharacterController _cc;
        private ChunkManager        _chunkManager;

        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;

        // Spawn protection: freeze player until the chunk underneath has loaded
        private bool  _spawnReady;
        private float _spawnCheckTimer;

        public bool IsMoving    { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsJumping   { get; private set; }
        public bool IsSwimming  { get; private set; }
        public PlayerAnimationState CurrentAnimationState { get; private set; } = PlayerAnimationState.Idle;

        // ── Unity ────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _chunkManager = FindObjectOfType<ChunkManager>();

            // Create camera if not assigned
            if (_cameraTarget == null)
            {
                var camGo = new GameObject("PlayerCamera");
                camGo.transform.SetParent(transform);
                camGo.transform.localPosition = new Vector3(0f, 1.6f, -4f);
                var camComp = camGo.AddComponent<Camera>();
                camComp.tag = "MainCamera";
                _cameraTarget = camGo.transform;
            }
        }

        private void Update()
        {
            if (!_spawnReady)
            {
                WaitForSpawnGround();
                return;
            }
            HandleMouseLook();
            HandleMovement();
            UpdateAnimationState();
        }

        // Poll until a solid surface exists below the player, then enable physics
        private void WaitForSpawnGround()
        {
            _spawnCheckTimer += Time.deltaTime;

            // Raycast downward — if we hit something the chunk collider is ready
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 300f))
            {
                // Snap player just above the hit surface
                var pos = transform.position;
                pos.y = hit.point.y + _cc.height * 0.5f + 0.05f;
                _cc.enabled = false;
                transform.position = pos;
                _cc.enabled = true;
                _spawnReady = true;
                return;
            }

            // Safety fallback: give up after 10 s and drop the player anyway
            if (_spawnCheckTimer > 10f)
                _spawnReady = true;
        }

        // ── Mouse Look ───────────────────────────────────────────────────────
        private void HandleMouseLook()
        {
            _yaw   += Input.GetAxis("Mouse X") * 2f;
            _pitch -= Input.GetAxis("Mouse Y") * 2f;
            _pitch  = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            if (_cameraTarget != null)
            {
                // Position camera on a pitch-rotated arm behind the player
                float pitchRad = _pitch * Mathf.Deg2Rad;
                float armLength = 4f;
                float camY   = 1.6f + armLength * Mathf.Sin(pitchRad);
                float camZ   = -armLength * Mathf.Cos(pitchRad);
                _cameraTarget.localPosition = new Vector3(0f, camY, camZ);
                _cameraTarget.LookAt(transform.position + Vector3.up * 1.4f);
            }
        }

        // ── Movement ─────────────────────────────────────────────────────────
        private void HandleMovement()
        {
            bool grounded  = _cc.isGrounded;
            IsSwimming = IsInWater();

            float moveSpeed;
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && !IsSwimming;
            moveSpeed   = IsSprinting
                ? GameConstants.PlayerSprintSpeed
                : IsSwimming
                    ? GameConstants.PlayerSwimSpeed
                    : GameConstants.PlayerMoveSpeed;

            // Horizontal movement
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = transform.forward * v + transform.right * h;
            IsMoving = move.sqrMagnitude > 0.01f;

            // Vertical
            if (IsSwimming)
            {
                _verticalVelocity = 0f;
                if (Input.GetKey(KeyCode.Space))          _verticalVelocity =  GameConstants.PlayerSwimSpeed;
                if (Input.GetKey(KeyCode.LeftControl))    _verticalVelocity = -GameConstants.PlayerSwimSpeed;
            }
            else
            {
                if (grounded)
                {
                    IsJumping = false;
                    if (_verticalVelocity < 0f) _verticalVelocity = GroundedBump;
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        _verticalVelocity = GameConstants.PlayerJumpForce;
                        IsJumping = true;
                    }
                }
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 velocity = move * moveSpeed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
        }

        private bool IsInWater()
        {
            if (_chunkManager == null) return false;
            Vector3 checkPos = transform.position + Vector3.up * 0.5f;
            BlockType block = _chunkManager.GetBlock(
                Mathf.FloorToInt(checkPos.x),
                Mathf.FloorToInt(checkPos.y),
                Mathf.FloorToInt(checkPos.z));
            return block == BlockType.Water;
        }

        // ── Animation State ──────────────────────────────────────────────────
        private void UpdateAnimationState()
        {
            if      (IsSwimming)  CurrentAnimationState = PlayerAnimationState.Swim;
            else if (IsJumping)   CurrentAnimationState = PlayerAnimationState.Jump;
            else if (IsSprinting) CurrentAnimationState = PlayerAnimationState.Run;
            else if (IsMoving)    CurrentAnimationState = PlayerAnimationState.Walk;
            else                  CurrentAnimationState = PlayerAnimationState.Idle;
        }
    }
}

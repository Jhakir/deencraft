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
        private const float MinPitch       = -40f;
        private const float MaxPitch       =  60f;
        private const float CamSmoothSpeed = 12f;
        private const float CamArmLength   =  5f;

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

            // Create a visible body (capsule) if none exists
            if (transform.Find("PlayerBody") == null)
            {
                // Body (torso+legs)
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "PlayerBody";
                body.transform.SetParent(transform);
                body.transform.localPosition = new Vector3(0f, 0f, 0f);
                body.transform.localScale    = new Vector3(0.8f, 1f, 0.8f);
                Destroy(body.GetComponent<CapsuleCollider>()); // CC handles collision
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.4f, 0.6f, 1f); // light blue
                body.GetComponent<Renderer>().material = mat;

                // Head
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "PlayerHead";
                head.transform.SetParent(transform);
                head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                head.transform.localScale    = new Vector3(0.7f, 0.7f, 0.7f);
                Destroy(head.GetComponent<SphereCollider>());
                var headMat = new Material(Shader.Find("Standard"));
                headMat.color = new Color(1f, 0.85f, 0.7f); // skin tone
                head.GetComponent<Renderer>().material = headMat;
            }

            // Create camera if not assigned — detached from player to avoid jitter
            if (_cameraTarget == null)
            {
                var camGo = new GameObject("PlayerCamera");
                // NOT parented to player — we move it manually in LateUpdate
                camGo.transform.position = transform.position + new Vector3(0f, 3f, -CamArmLength);
                var camComp = camGo.AddComponent<Camera>();
                camComp.tag = "MainCamera";
                camComp.farClipPlane = 500f;
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

        private void LateUpdate()
        {
            if (_cameraTarget == null) return;
            // Smooth camera follow — detached from player so no jitter
            float pitchRad  = _pitch * Mathf.Deg2Rad;
            float camY      = 1.6f + CamArmLength * Mathf.Sin(pitchRad);
            float camZ      = -CamArmLength * Mathf.Cos(pitchRad);
            Vector3 desiredPos = transform.position
                + Quaternion.Euler(0f, _yaw, 0f) * new Vector3(0f, camY, camZ);
            _cameraTarget.position = Vector3.Lerp(
                _cameraTarget.position, desiredPos, CamSmoothSpeed * Time.deltaTime);
            _cameraTarget.LookAt(transform.position + Vector3.up * 1.4f);
        }

        // Poll until a solid surface exists below the player, then enable physics
        private void WaitForSpawnGround()
        {
            _spawnCheckTimer += Time.deltaTime;
            if (_spawnCheckTimer < 0.5f) return;

            if (_chunkManager == null)
            {
                _chunkManager = FindObjectOfType<ChunkManager>();
                if (_chunkManager == null) { _spawnReady = true; return; }
            }

            // Keep teleporting player to the exact surface Y every frame.
            // Once the chunk's MeshCollider is ready, isGrounded becomes true → release.
            int   surfaceY = WorldGenerator.GetSurfaceY(transform.position.x, transform.position.z, _chunkManager.WorldSeed);
            // +height/2 centres the CC capsule, +skinWidth lifts it off the surface
            float targetY  = surfaceY + _cc.height * 0.5f + _cc.skinWidth + 0.05f;

            _cc.enabled = false;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            _cc.enabled = true;
            _verticalVelocity = 0f;

            if (_cc.isGrounded)
                _spawnReady = true;

            // Safety: give up after 15 s regardless
            if (_spawnCheckTimer > 15f)
                _spawnReady = true;
        }

        // ── Mouse Look ───────────────────────────────────────────────────────
        private void HandleMouseLook()
        {
            _yaw   += Input.GetAxis("Mouse X") * 2f;
            _pitch -= Input.GetAxis("Mouse Y") * 2f;
            _pitch  = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Camera rotation driven by _yaw/_pitch; actual movement done in LateUpdate
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

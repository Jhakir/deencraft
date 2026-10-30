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
    /// - Double-tap Space: toggle creative flight
    /// - Space / Left Ctrl while flying: rise / descend
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
        private const float CameraCollisionRadius = 0.15f;
        private const float CameraWallClearance = 0.05f;
        private const float CameraEyeInset = 0.25f;
        private const float CameraShoulderOffset = 0.2f;
        private const float SpawnHoldDelay = 0.5f;
        private const float SpawnWarningDelay = 15f;
        private const float SurfaceClearance = 0.1f;
        // Terrain never generates below y=0, so anything under this is the void.
        private const float VoidRescueY    = -5f;

        // ── State ─────────────────────────────────────────────────────────────
        private CharacterController _cc;
        private ChunkManager        _chunkManager;

        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;
        private readonly FlightInput _flightInput = new FlightInput();

        // Spawn protection: freeze player until the chunk underneath has loaded
        private bool  _spawnReady;
        private float _spawnCheckTimer;
        private bool _spawnWarningLogged;
        private bool _inputBlocked;
        private int _inputReleasedFrame = -1;

        public bool IsMoving    { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsJumping   { get; private set; }
        public bool IsSwimming  { get; private set; }
        public bool IsFlying => _flightInput.IsFlying;
        public bool InputBlocked
        {
            get => _inputBlocked;
            set
            {
                if (_inputBlocked && !value) _inputReleasedFrame = Time.frameCount;
                _inputBlocked = value;
            }
        }
        public bool CanProcessGameplayInput => !InputBlocked && Time.frameCount > _inputReleasedFrame;
        public PlayerAnimationState CurrentAnimationState { get; private set; } = PlayerAnimationState.Idle;

        // ── Unity ────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _cc.radius    = GameConstants.PlayerWidth * 0.5f;
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
                // CC handles collision. Collider may be absent if engine code
                // stripping dropped it (WebGL), so guard against null.
                var bodyCollider = body.GetComponent<CapsuleCollider>();
                if (bodyCollider != null) Destroy(bodyCollider);
                // Recolor the default primitive material instead of calling
                // Shader.Find("Standard") — that shader can be stripped from
                // WebGL builds (nothing else references it), which makes
                // Shader.Find return null and crashes Awake() via
                // `new Material(null)`.
                body.GetComponent<Renderer>().material.color = new Color(0.4f, 0.6f, 1f); // light blue

                // Head
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "PlayerHead";
                head.transform.SetParent(transform);
                head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                head.transform.localScale    = new Vector3(0.7f, 0.7f, 0.7f);
                var headCollider = head.GetComponent<SphereCollider>();
                if (headCollider != null) Destroy(headCollider);
                head.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.7f); // skin tone
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
            if (!CanProcessGameplayInput)
            {
                IsMoving = false;
                IsSprinting = false;
                return;
            }
            HandleCursorLock();

            if (!_spawnReady)
            {
                WaitForSpawnGround();
                return;
            }
            if (!HasTerrainAtPosition(transform.position))
            {
                _spawnReady = false;
                _spawnCheckTimer = SpawnHoldDelay;
                _verticalVelocity = 0f;
                WaitForSpawnGround();
                return;
            }
            HandleMouseLook();
            HandleMovement();
            GuardAgainstVoidFall();
            UpdateAnimationState();
        }

        private void GuardAgainstVoidFall()
        {
            bool insideTerrain = false;
            if (_chunkManager != null)
            {
                Vector3 centre = transform.position + _cc.center;
                BlockType block = _chunkManager.GetBlock(
                    Mathf.FloorToInt(centre.x), Mathf.FloorToInt(centre.y), Mathf.FloorToInt(centre.z));
                insideTerrain = block != BlockType.Air && block != BlockType.Water;
            }
            if (transform.position.y >= VoidRescueY && !insideTerrain) return;

            _spawnReady       = false;
            _spawnCheckTimer  = SpawnHoldDelay; // skip the initial delay; re-ground next frame
            _verticalVelocity = 0f;
        }

        private bool HasTerrainAtPosition(Vector3 position)
        {
            if (_chunkManager == null) return false;
            float margin = _cc.radius + _cc.skinWidth;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = position + new Vector3(x * margin, 0f, z * margin);
                if (!_chunkManager.IsChunkReady(ChunkManager.WorldToChunkCoord(Vector3Int.FloorToInt(corner))))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Browsers only grant pointer lock in response to a user gesture, so the
        /// cursor is captured on click and released with Escape. Without this,
        /// mouse look does nothing in a WebGL build.
        /// </summary>
        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        private void LateUpdate()
        {
            if (_cameraTarget == null) return;
            // Direct positioning — no Lerp, eliminates camera-chasing jitter
            float pitchRad  = _pitch * Mathf.Deg2Rad;
            float camY      = CameraShoulderOffset + CamArmLength * Mathf.Sin(pitchRad);
            float camZ      = -CamArmLength * Mathf.Cos(pitchRad);
            Vector3 lookTarget = transform.position + _cc.center
                + Vector3.up * (_cc.height * 0.5f - CameraEyeInset);
            Vector3 desiredPosition = lookTarget
                + Quaternion.Euler(0f, _yaw, 0f) * new Vector3(0f, camY, camZ);
            Vector3 offset = desiredPosition - lookTarget;
            float distance = offset.magnitude;
            Vector3 direction = offset.normalized;
            foreach (var hit in Physics.SphereCastAll(lookTarget, CameraCollisionRadius, direction,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - CameraWallClearance));
            }
            _cameraTarget.position = lookTarget + direction * distance;
            _cameraTarget.rotation = Quaternion.LookRotation(-direction, Vector3.up);
        }

        private void WaitForSpawnGround()
        {
            _spawnCheckTimer += Time.deltaTime;
            if (_spawnCheckTimer < SpawnHoldDelay) return;

            if (_chunkManager == null)
            {
                _chunkManager = FindObjectOfType<ChunkManager>();
                if (_chunkManager == null)
                {
                    if (!_spawnWarningLogged)
                    {
                        Debug.LogError("[PlayerController] Cannot spawn: no ChunkManager is available.", this);
                        _spawnWarningLogged = true;
                    }
                    return;
                }
            }

            if (!HasTerrainAtPosition(transform.position)
                || !_chunkManager.TryGetSurfaceHeight(transform.position.x, transform.position.z, out float surfaceY))
            {
                if (_spawnCheckTimer > SpawnWarningDelay && !_spawnWarningLogged)
                {
                    Debug.LogWarning("[PlayerController] Waiting for terrain colliders before spawning.", this);
                    _spawnWarningLogged = true;
                }
                return;
            }

            // Clear the entire controller footprint, including columns on chunk boundaries.
            float margin = _cc.radius + _cc.skinWidth;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                if (_chunkManager.TryGetSurfaceHeight(transform.position.x + x * margin,
                    transform.position.z + z * margin, out float cornerY))
                    surfaceY = Mathf.Max(surfaceY, cornerY);
            }
            float targetY = surfaceY + _cc.height * 0.5f - _cc.center.y + SurfaceClearance;

            _cc.enabled = false;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            _cc.enabled = true;
            _verticalVelocity = 0f;

            _spawnReady = true;
            _spawnWarningLogged = false;
        }

        // ── Mouse Look ───────────────────────────────────────────────────────
        private void HandleMouseLook()
        {
            // Only steer while the pointer is captured, otherwise moving the mouse
            // across the page would spin the camera.
            if (Cursor.lockState != CursorLockMode.Locked) return;

            _yaw   += Input.GetAxis("Mouse X") * 2f;
            _pitch -= Input.GetAxis("Mouse Y") * 2f;
            _pitch  = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Camera rotation driven by _yaw/_pitch; actual movement done in LateUpdate
        }

        // ── Movement ─────────────────────────────────────────────────────────
        private void HandleMovement()
        {
            bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
            bool toggledFlight = jumpPressed && _flightInput.PressJump(Time.time);
            if (toggledFlight)
            {
                _verticalVelocity = 0f;
                IsJumping = false;
                Debug.Log(IsFlying ? "[PlayerController] Flight enabled." : "[PlayerController] Flight disabled.", this);
            }
            bool grounded  = _cc.isGrounded;
            IsSwimming = !IsFlying && IsInWater();

            float moveSpeed;
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && !IsSwimming;
            moveSpeed   = IsFlying
                ? (IsSprinting ? GameConstants.PlayerFlyBoostSpeed : GameConstants.PlayerFlySpeed)
                : IsSprinting
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
            if (IsFlying)
            {
                int verticalInput = (Input.GetKey(KeyCode.Space) ? 1 : 0)
                    - (Input.GetKey(KeyCode.LeftControl) ? 1 : 0);
                _verticalVelocity = verticalInput * moveSpeed;
            }
            else if (IsSwimming)
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
                    if (jumpPressed && !toggledFlight)
                    {
                        _verticalVelocity = GameConstants.PlayerJumpForce;
                        IsJumping = true;
                    }
                }
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 velocity = move * moveSpeed + Vector3.up * _verticalVelocity;
            Vector3 destination = transform.position + velocity * Time.deltaTime;
            if (!HasTerrainAtPosition(destination))
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }
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

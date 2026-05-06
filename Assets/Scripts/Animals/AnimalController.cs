// Assets/Scripts/Animals/AnimalController.cs
using UnityEngine;
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Animals
{
    /// <summary>
    /// Drives a passive animal using <see cref="AnimalBrainData"/> and a CharacterController.
    /// Implements <see cref="IInteractable"/> so players can interact (sheep shearing).
    ///
    /// Attach alongside a CharacterController.  Set <c>_animalType</c> and <c>_foodItemId</c>
    /// in the Inspector.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AnimalController : MonoBehaviour, IInteractable
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private AnimalType _animalType   = AnimalType.Cow;
        [SerializeField] private ItemId     _foodItemId   = ItemId.Wheat;

        // ── Movement constants ────────────────────────────────────────────────
        private const float MoveSpeed    =  3f;
        private const float FleeSpeed    =  5f;
        private const float Gravity      = -20f;
        private const float GroundedBump =  -2f;
        private const float RotateSpeed  =  5f;
        private const float FollowGap    =  1.5f; // min distance kept from player when following

        // ── Sheep shearing ────────────────────────────────────────────────────
        private const float ShearCooldown = 60f; // seconds until wool regrows
        private bool  _canBeSheared       = true;
        private float _shearCooldownTimer;

        // ── Runtime state ─────────────────────────────────────────────────────
        private CharacterController _cc;
        private AnimalBrainData     _brain;
        private Transform           _player;
        private InventoryHolder     _playerInventory;
        private float               _verticalVelocity;

        // ── Public accessors ──────────────────────────────────────────────────
        public AnimalBrainData Brain      => _brain;
        public AnimalType      AnimalType => _animalType;
        public bool            CanBeSheared => _canBeSheared;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _brain = new AnimalBrainData();
        }

        private void Start()
        {
            var playerCtrl = FindObjectOfType<PlayerController>();
            if (playerCtrl != null)
            {
                _player          = playerCtrl.transform;
                _playerInventory = playerCtrl.GetComponent<InventoryHolder>();
            }
        }

        private void Update()
        {
            TickShearCooldown();

            if (_player == null) return;

            bool playerHasFood = _playerInventory != null &&
                                 _playerInventory.Inventory.CountItem(_foodItemId) > 0;

            _brain.Tick(Time.deltaTime, transform.position, _player.position, playerHasFood);
            Move();
        }

        // ── IInteractable ─────────────────────────────────────────────────────
        /// <summary>
        /// Called when the player presses E while looking at this animal.
        /// Sheep give wool; other animals just flee.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (_animalType == AnimalType.Sheep && _canBeSheared)
            {
                var holder = interactor.GetComponent<InventoryHolder>();
                if (holder != null)
                {
                    holder.Inventory.AddItem(new ItemStack(ItemId.Wool, 1));
                    _canBeSheared       = false;
                    _shearCooldownTimer = ShearCooldown;
                }
            }
            // All animals flee when touched
            _brain.TriggerFlee(interactor.transform.position);
        }

        /// <summary>Manually trigger flee (called by combat or environment events).</summary>
        public void TriggerFlee(Vector3 fromPosition) => _brain.TriggerFlee(fromPosition);

        // ── Private helpers ───────────────────────────────────────────────────
        private void TickShearCooldown()
        {
            if (_canBeSheared) return;
            _shearCooldownTimer -= Time.deltaTime;
            if (_shearCooldownTimer <= 0f)
                _canBeSheared = true;
        }

        private void Move()
        {
            Vector3 direction = Vector3.zero;
            float   speed     = MoveSpeed;

            switch (_brain.CurrentState)
            {
                case AnimalState.Wander:
                    Vector3 toTarget = _brain.WanderTarget - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.01f)
                        direction = toTarget.normalized;
                    break;

                case AnimalState.Flee:
                    Vector3 away = transform.position - _brain.FleeFromPos;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.01f)
                        direction = away.normalized;
                    speed = FleeSpeed;
                    break;

                case AnimalState.Follow:
                    Vector3 toPlayer = _brain.FollowTarget - transform.position;
                    toPlayer.y = 0f;
                    if (toPlayer.magnitude > FollowGap)
                        direction = toPlayer.normalized;
                    break;
            }

            // Rotate toward movement direction
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    RotateSpeed * Time.deltaTime);

            // Apply gravity
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = GroundedBump;
            _verticalVelocity += Gravity * Time.deltaTime;

            _cc.Move((direction * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }
    }
}

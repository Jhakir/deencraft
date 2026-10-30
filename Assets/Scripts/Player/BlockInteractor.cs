// Assets/Scripts/Player/BlockInteractor.cs
// Replaces the Phase-2 stub BlockInteractionManager.
// Attach to the Player GameObject alongside PlayerController and InventoryHolder.
using UnityEngine;
using DeenCraft;
using DeenCraft.World;

namespace DeenCraft.Player
{
    /// <summary>
    /// Handles all player-world interaction:
    ///   Left-click  → mine targeted block (adds item to inventory)
    ///   Right-click → place active hotbar block
    ///   E           → interact with entities (horse, boat, villager, etc.)
    ///
    /// Uses <see cref="BlockItemMap"/> for correct BlockType ↔ ItemId conversion.
    /// Uses <see cref="IInteractable"/> for entity interaction (no circular dependency).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(InventoryHolder))]
    public class BlockInteractor : MonoBehaviour
    {
        public static event System.Action<GameObject, string> BlockEditDenied;
        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private Camera _playerCamera;

        // ── Runtime ───────────────────────────────────────────────────────────
        private PlayerController _controller;
        private InventoryHolder  _inventoryHolder;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _controller      = GetComponent<PlayerController>();
            _inventoryHolder = GetComponent<InventoryHolder>();
        }

        private void Start()
        {
            if (_playerCamera == null)
                _playerCamera = Camera.main;
        }

        private void Update()
        {
            if (_controller != null && !_controller.CanProcessGameplayInput) return;
            if (_playerCamera == null || ChunkManager.Instance == null) return;

            // The first click is consumed by the browser to capture the pointer,
            // so ignore interaction until the cursor is actually locked.
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Input.GetMouseButtonDown(0)) TryMine();
            if (Input.GetMouseButtonDown(1)) TryPlace();
            if (Input.GetKeyDown(KeyCode.E)) TryInteract();
        }

        // ── Mining ────────────────────────────────────────────────────────────
        private void TryMine()
        {
            if (!TryGetTargetBlock(out _, out Vector3Int blockPos)) return;

            BlockType blockType = ChunkManager.Instance.GetBlock(blockPos.x, blockPos.y, blockPos.z);
            if (blockType == BlockType.Air || blockType == BlockType.Water) return;

            if (!ApplyEdit(blockPos, BlockType.Air)) return;

            // Give the block's item drop to the player
            ItemId itemId = BlockItemMap.BlockTypeToItemId(blockType);
            if (itemId != ItemId.None && _inventoryHolder != null)
                _inventoryHolder.Inventory.AddItem(new ItemStack(itemId, 1));
        }

        // ── Placing ───────────────────────────────────────────────────────────
        private void TryPlace()
        {
            if (_inventoryHolder == null) return;

            ItemStack activeItem = _inventoryHolder.Inventory.ActiveItem;
            if (activeItem.IsEmpty) return;

            BlockType blockType = BlockItemMap.ItemIdToBlockType(activeItem.ItemId);
            if (blockType == BlockType.Air) return; // item is not a placeable block

            if (!TryGetTargetBlock(out RaycastHit hit, out Vector3Int blockPos)) return;

            // Place on the face that was hit
            Vector3Int placePos = blockPos + Vector3Int.RoundToInt(hit.normal);

            // Safety: never place a block that would clip into the player
            if (IsInsidePlayer(placePos)) return;

            if (!ApplyEdit(placePos, blockType)) return;
            _inventoryHolder.Inventory.RemoveItem(activeItem.ItemId, 1);
        }

        private bool ApplyEdit(Vector3Int position, BlockType block)
        {
            BlockEditResult result = ChunkManager.Instance.TrySetBlock(position, block);
            if (result == BlockEditResult.Success) return true;
            string message = result == BlockEditResult.Protected
                ? "This masjid is a protected landmark. You can explore, but not change its blocks."
                : "This area is not ready for building. Please move closer and try again.";
            if (BlockEditDenied != null) BlockEditDenied.Invoke(gameObject, message);
            else Debug.LogWarning("[BlockInteractor] " + message, this);
            return false;
        }

        // ── Entity interaction ────────────────────────────────────────────────
        private void TryInteract()
        {
            if (_playerCamera == null) return;

            if (!TryGetCameraHit(out RaycastHit hit)) return;

            // IInteractable covers horses, boats, villagers, sheep, etc.
            // No direct reference to Animals/Villager assemblies needed — only the Core interface.
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.Interact(gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool TryGetTargetBlock(out RaycastHit hit, out Vector3Int blockPos)
        {
            blockPos = Vector3Int.zero;
            if (!TryGetCameraHit(out hit)) return false;
            if (hit.collider.GetComponent<ChunkView>() == null) return false;

            // Step slightly inside the hit surface to identify the correct block voxel
            Vector3 inside = hit.point - hit.normal * 0.5f;
            blockPos = Vector3Int.FloorToInt(inside);
            return true;
        }

        private Ray GetCentreRay()
        {
            return new Ray(
                _playerCamera.transform.position,
                _playerCamera.transform.forward);
        }

        private bool TryGetCameraHit(out RaycastHit hit)
        {
            hit = default;
            if (_playerCamera == null) return false;
            float distance = GameConstants.PlayerReach +
                Vector3.Distance(_playerCamera.transform.position, transform.position);
            RaycastHit[] hits = Physics.RaycastAll(GetCentreRay(), distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var candidate in hits)
            {
                if (candidate.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Distance(transform.position, candidate.point) > GameConstants.PlayerReach) return false;
                hit = candidate;
                return true;
            }
            return false;
        }

        private bool IsInsidePlayer(Vector3Int pos)
        {
            Bounds playerBounds = new Bounds(
                transform.position + Vector3.up * (GameConstants.PlayerHeight * 0.5f),
                new Vector3(GameConstants.PlayerWidth, GameConstants.PlayerHeight, GameConstants.PlayerWidth));

            Bounds blockBounds = new Bounds(
                new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f),
                Vector3.one);

            return playerBounds.Intersects(blockBounds);
        }
    }
}

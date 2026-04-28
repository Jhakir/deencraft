using UnityEngine;

namespace DeenCraft.World
{
    /// <summary>
    /// Handles player block placement and destruction via raycasting.
    /// Attach to the Player GameObject.
    ///
    /// Left-click  → destroy block (set Air)
    /// Right-click → place selected block on adjacent face
    ///
    /// Selected block is a Phase 4 stub — always returns Stone until
    /// the inventory system is implemented.
    /// </summary>
    public sealed class BlockInteractionManager : MonoBehaviour
    {
        [SerializeField] private Camera _playerCamera;

        private void Update()
        {
            if (ChunkManager.Instance == null) return;

            if (Input.GetMouseButtonDown(0)) TryDestroyBlock();
            if (Input.GetMouseButtonDown(1)) TryPlaceBlock();
        }

        private void TryDestroyBlock()
        {
            if (!TryGetTargetBlock(out _, out Vector3Int blockPos)) return;
            ChunkManager.Instance.SetBlock(blockPos, BlockType.Air);
        }

        private void TryPlaceBlock()
        {
            if (!TryGetTargetBlock(out RaycastHit hit, out Vector3Int blockPos)) return;

            // Place on the face that was hit (step one unit along the surface normal)
            Vector3Int placePos = blockPos + Vector3Int.RoundToInt(hit.normal);
            ChunkManager.Instance.SetBlock(placePos, GetSelectedBlock());
        }

        private bool TryGetTargetBlock(out RaycastHit hit, out Vector3Int blockPos)
        {
            blockPos = Vector3Int.zero;
            Camera cam = _playerCamera != null ? _playerCamera : Camera.main;

            if (cam == null)
            {
                hit = default;
                return false;
            }

            if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                out hit, GameConstants.PlayerReach))
            {
                // Step slightly inside the hit surface to identify the correct block
                Vector3 insideBlock = hit.point - hit.normal * 0.5f;
                blockPos = Vector3Int.FloorToInt(insideBlock);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Phase 4 stub — returns Stone until PlayerInventory is implemented.
        /// Replace with <c>PlayerInventory.Instance.SelectedBlock</c> in Phase 4.
        /// </summary>
        private static BlockType GetSelectedBlock() => BlockType.Stone;
    }
}

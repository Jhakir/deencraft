using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// BFS-based water flow simulation.
    /// Only player-placed water triggers spreading; world-gen water is static.
    ///
    /// Attach to the same GameObject as ChunkManager (or a child of it).
    /// Call TriggerSpread(worldPos) after placing a Water block.
    /// </summary>
    public sealed class WaterSimulator : MonoBehaviour
    {
        [SerializeField] private float _waterTickInterval = 0.1f;

        private readonly Queue<(Vector3Int pos, int distance)> _spreadQueue =
            new Queue<(Vector3Int, int)>();
        private readonly HashSet<Vector3Int> _visited = new HashSet<Vector3Int>();

        private static readonly Vector3Int[] s_horizontalDirs =
        {
            Vector3Int.right,
            Vector3Int.left,
            new Vector3Int(0, 0,  1),
            new Vector3Int(0, 0, -1),
        };

        private void Start()
        {
            StartCoroutine(SimulationLoop());
        }

        /// <summary>
        /// Call this after the player places a Water block to trigger BFS spread from that position.
        /// </summary>
        public void TriggerSpread(Vector3Int sourceWorldPos)
        {
            if (_visited.Contains(sourceWorldPos)) return;
            _visited.Add(sourceWorldPos);
            _spreadQueue.Enqueue((sourceWorldPos, 0));
        }

        private IEnumerator SimulationLoop()
        {
            var tickWait = new WaitForSeconds(_waterTickInterval);
            while (true)
            {
                if (_spreadQueue.Count > 0)
                {
                    var (pos, dist) = _spreadQueue.Dequeue();
                    if (dist < GameConstants.MaxWaterSpread)
                        SpreadFrom(pos, dist);
                }
                yield return tickWait;
            }
        }

        private void SpreadFrom(Vector3Int pos, int dist)
        {
            if (ChunkManager.Instance == null) return;

            // Prefer flowing down
            Vector3Int below = pos + Vector3Int.down;
            if (TryFill(below, dist + 1))
                return; // Water falls straight down — don't spread horizontally yet

            // Spread horizontally
            foreach (var dir in s_horizontalDirs)
                TryFill(pos + dir, dist + 1);
        }

        private bool TryFill(Vector3Int worldPos, int dist)
        {
            if (ChunkManager.Instance == null) return false;

            var chunkCoord = ChunkManager.WorldToChunkCoord(worldPos);
            var data       = ChunkManager.Instance.GetChunkData(chunkCoord);
            if (data == null) return false;

            int lx = worldPos.x - chunkCoord.x * GameConstants.ChunkWidth;
            int lz = worldPos.z - chunkCoord.y * GameConstants.ChunkDepth;
            if (!data.IsInBounds(lx, worldPos.y, lz)) return false;

            if (data.GetBlock(lx, worldPos.y, lz) != BlockType.Air) return false;

            ChunkManager.Instance.SetBlock(worldPos, BlockType.Water);

            if (!_visited.Contains(worldPos))
            {
                _visited.Add(worldPos);
                _spreadQueue.Enqueue((worldPos, dist));
            }
            return true;
        }
    }
}

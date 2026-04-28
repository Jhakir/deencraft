using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// Manages the chunk pool, loads/unloads chunks as the player moves,
    /// and orchestrates WorldGenerator + ChunkMesher per frame via coroutines.
    ///
    /// Singleton — attach to one GameObject in the scene.
    /// Never call Instantiate/Destroy on chunk GameObjects.
    /// </summary>
    public sealed class ChunkManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static ChunkManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private int       _viewDistance   = 8;
        [SerializeField] private int       _poolSize       = 200;
        [SerializeField] private int       _worldSeed      = 42;
        [SerializeField] private Material  _worldMaterial;
        [SerializeField] private Transform _playerTransform;

        // ── Internal State ───────────────────────────────────────────────────
        private readonly Dictionary<Vector2Int, ChunkData> _chunkDataCache =
            new Dictionary<Vector2Int, ChunkData>();
        private readonly Dictionary<Vector2Int, ChunkView> _activeViews =
            new Dictionary<Vector2Int, ChunkView>();
        private readonly Queue<ChunkView>    _pool         = new Queue<ChunkView>();
        private readonly HashSet<Vector2Int> _dirtyChunks  = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int>   _loadQueue    = new Queue<Vector2Int>();

        private Vector2Int _lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitPool();
        }

        private void Start()
        {
            StartCoroutine(ChunkUpdateLoop());
            StartCoroutine(LoadQueueProcessor());
            StartCoroutine(MeshRebuildLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Pool ─────────────────────────────────────────────────────────────
        private void InitPool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject("ChunkView");
                go.transform.SetParent(transform, worldPositionStays: false);

                var view = go.AddComponent<ChunkView>();
                var mr   = go.GetComponent<MeshRenderer>();
                if (_worldMaterial != null) mr.sharedMaterial = _worldMaterial;

                go.SetActive(false);
                _pool.Enqueue(view);
            }
        }

        // ── Chunk Update Loop ────────────────────────────────────────────────
        private IEnumerator ChunkUpdateLoop()
        {
            while (true)
            {
                Vector2Int playerChunk = GetPlayerChunkCoord();
                if (playerChunk != _lastPlayerChunk)
                {
                    _lastPlayerChunk = playerChunk;
                    RefreshChunkSet(playerChunk);
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void RefreshChunkSet(Vector2Int center)
        {
            var needed = new HashSet<Vector2Int>();
            for (int dx = -_viewDistance; dx <= _viewDistance; dx++)
            for (int dz = -_viewDistance; dz <= _viewDistance; dz++)
            {
                var coord = new Vector2Int(center.x + dx, center.y + dz);
                needed.Add(coord);
                if (!_activeViews.ContainsKey(coord) && !IsInLoadQueue(coord))
                    _loadQueue.Enqueue(coord);
            }

            var toUnload = new List<Vector2Int>();
            foreach (var coord in _activeViews.Keys)
                if (!needed.Contains(coord))
                    toUnload.Add(coord);

            foreach (var coord in toUnload)
                UnloadChunk(coord);
        }

        private bool IsInLoadQueue(Vector2Int coord)
        {
            // Queue doesn't expose Contains; check active views as proxy
            return _activeViews.ContainsKey(coord);
        }

        // ── Load Queue Processor (one chunk per frame) ───────────────────────
        private IEnumerator LoadQueueProcessor()
        {
            while (true)
            {
                if (_loadQueue.Count > 0)
                {
                    var coord = _loadQueue.Dequeue();
                    if (!_activeViews.ContainsKey(coord))
                    {
                        yield return StartCoroutine(LoadChunk(coord));
                    }
                }
                yield return null; // one chunk per frame
            }
        }

        private IEnumerator LoadChunk(Vector2Int coord)
        {
            // Generate terrain data if not cached
            if (!_chunkDataCache.ContainsKey(coord))
            {
                var data = new ChunkData();
                WorldGenerator.Generate(data, coord.x, coord.y, _worldSeed);
                _chunkDataCache[coord] = data;
            }

            // Grab a view from the pool
            if (_pool.Count == 0) yield break;

            var view = _pool.Dequeue();
            view.gameObject.SetActive(true);
            view.transform.position = new Vector3(
                coord.x * GameConstants.ChunkWidth,
                0f,
                coord.y * GameConstants.ChunkDepth);

            // Build mesh
            var meshData = ChunkMesher.BuildMesh(_chunkDataCache[coord], GetNeighborData(coord));
            view.ApplyMesh(meshData);
            _activeViews[coord] = view;

            yield return null;
        }

        private void UnloadChunk(Vector2Int coord)
        {
            if (!_activeViews.TryGetValue(coord, out var view)) return;
            view.Clear();
            _pool.Enqueue(view);
            _activeViews.Remove(coord);
        }

        // ── Mesh Rebuild Loop (dirty chunks) ─────────────────────────────────
        private IEnumerator MeshRebuildLoop()
        {
            while (true)
            {
                if (_dirtyChunks.Count > 0)
                {
                    var batch = new List<Vector2Int>(_dirtyChunks);
                    _dirtyChunks.Clear();

                    foreach (var coord in batch)
                    {
                        if (_activeViews.TryGetValue(coord, out var view) &&
                            _chunkDataCache.TryGetValue(coord, out var data))
                        {
                            view.ApplyMesh(ChunkMesher.BuildMesh(data, GetNeighborData(coord)));
                        }
                        yield return null; // one rebuild per frame
                    }
                }
                yield return null;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────
        /// <summary>Returns the ChunkData for the given chunk coordinate, or null if not loaded.</summary>
        public ChunkData GetChunkData(Vector2Int chunkCoord)
        {
            _chunkDataCache.TryGetValue(chunkCoord, out var data);
            return data;
        }

        /// <summary>
        /// Sets a block at a world position and marks the affected chunk (and boundary neighbors) dirty.
        /// </summary>
        public void SetBlock(Vector3Int worldPos, BlockType block)
        {
            var chunkCoord = WorldToChunkCoord(worldPos);
            if (!_chunkDataCache.TryGetValue(chunkCoord, out var data)) return;

            int lx = worldPos.x - chunkCoord.x * GameConstants.ChunkWidth;
            int lz = worldPos.z - chunkCoord.y * GameConstants.ChunkDepth;

            if (!data.IsInBounds(lx, worldPos.y, lz)) return;

            data.SetBlock(lx, worldPos.y, lz, block);
            DirtyChunk(chunkCoord);

            // Dirty neighbors when on a boundary
            if (lx == 0)                             DirtyChunk(new Vector2Int(chunkCoord.x - 1, chunkCoord.y));
            if (lx == GameConstants.ChunkWidth  - 1) DirtyChunk(new Vector2Int(chunkCoord.x + 1, chunkCoord.y));
            if (lz == 0)                             DirtyChunk(new Vector2Int(chunkCoord.x, chunkCoord.y - 1));
            if (lz == GameConstants.ChunkDepth  - 1) DirtyChunk(new Vector2Int(chunkCoord.x, chunkCoord.y + 1));
        }

        /// <summary>Marks a chunk as needing a mesh rebuild next frame.</summary>
        public void DirtyChunk(Vector2Int chunkCoord) => _dirtyChunks.Add(chunkCoord);

        // ── Coordinate Utilities ─────────────────────────────────────────────
        /// <summary>Converts a world-space position to chunk coordinates.</summary>
        public static Vector2Int WorldToChunkCoord(Vector3Int worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt((float)worldPos.x / GameConstants.ChunkWidth),
                Mathf.FloorToInt((float)worldPos.z / GameConstants.ChunkDepth));
        }

        private Vector2Int GetPlayerChunkCoord()
        {
            if (_playerTransform == null) return Vector2Int.zero;
            return WorldToChunkCoord(Vector3Int.FloorToInt(_playerTransform.position));
        }

        private ChunkData[] GetNeighborData(Vector2Int coord)
        {
            return new ChunkData[]
            {
                GetChunkData(new Vector2Int(coord.x + 1, coord.y)),  // +X
                GetChunkData(new Vector2Int(coord.x - 1, coord.y)),  // -X
                GetChunkData(new Vector2Int(coord.x, coord.y + 1)),  // +Z
                GetChunkData(new Vector2Int(coord.x, coord.y - 1)),  // -Z
            };
        }
    }
}

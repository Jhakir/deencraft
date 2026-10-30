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
        [SerializeField] private int       _viewDistance   = 6;
        [SerializeField] private int       _poolSize       = 200;
        [SerializeField] private int       _worldSeed      = 42;
        [SerializeField] private Material  _worldMaterial;
        [SerializeField] private Transform _playerTransform;

        public int WorldSeed => _worldSeed;
        public MasjidDistrictLayout DistrictLayout { get; private set; }
        public int WorldRevision { get; private set; }
        public event System.Action<Vector2Int> ChunkUnloading;
        public event System.Action WorldReset;

        /// <summary>
        /// Every chunk in the view square needs its own view. If the pool is smaller
        /// than this, LoadChunk runs dry and leaves chunks with no MeshCollider —
        /// the player then falls through the world on walking onto one.
        /// </summary>
        private int RequiredViewCount => (2 * _viewDistance + 1) * (2 * _viewDistance + 1);

        // ── Internal State ───────────────────────────────────────────────────
        private readonly Dictionary<Vector2Int, ChunkData> _chunkDataCache =
            new Dictionary<Vector2Int, ChunkData>();
        private readonly Dictionary<Vector2Int, ChunkView> _activeViews =
            new Dictionary<Vector2Int, ChunkView>();
        private readonly Queue<ChunkView>    _pool         = new Queue<ChunkView>();
        private readonly HashSet<Vector2Int> _dirtyChunks  = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int>   _loadQueue    = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> _queuedChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _editedChunks = new HashSet<Vector2Int>();

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
            PrepareDistrict();
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
            int poolSize = Mathf.Max(_poolSize, RequiredViewCount);
            for (int i = 0; i < poolSize; i++)
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
            var pending = new List<Vector2Int>();
            _loadQueue.Clear();
            _queuedChunks.Clear();
            for (int dx = -_viewDistance; dx <= _viewDistance; dx++)
            for (int dz = -_viewDistance; dz <= _viewDistance; dz++)
            {
                var coord = new Vector2Int(center.x + dx, center.y + dz);
                needed.Add(coord);
                if (!_activeViews.ContainsKey(coord) && _queuedChunks.Add(coord))
                    pending.Add(coord);
            }

            pending.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));
            foreach (var coord in pending)
                _loadQueue.Enqueue(coord);

            var toUnload = new List<Vector2Int>();
            foreach (var coord in _activeViews.Keys)
                if (!needed.Contains(coord))
                    toUnload.Add(coord);

            foreach (var coord in toUnload)
                UnloadChunk(coord);
        }

        // ── Load Queue Processor (one chunk per frame) ───────────────────────
        private IEnumerator LoadQueueProcessor()
        {
            while (true)
            {
                if (_loadQueue.Count > 0)
                {
                    var coord = _loadQueue.Dequeue();
                    _queuedChunks.Remove(coord);
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
                WorldGenerator.Generate(data, coord.x, coord.y, _worldSeed, DistrictLayout);
                _chunkDataCache[coord] = data;
            }

            // Grab a view from the pool
            if (_pool.Count == 0)
            {
                // Re-queue rather than drop it: a dropped chunk stays collider-less
                // and the player falls through it.
                if (_queuedChunks.Add(coord)) _loadQueue.Enqueue(coord);
                yield break;
            }

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
            ChunkUnloading?.Invoke(coord);
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

        public bool IsChunkReady(Vector2Int chunkCoord)
        {
            return _activeViews.TryGetValue(chunkCoord, out var view)
                && view.gameObject.activeInHierarchy
                && view.GetComponent<MeshCollider>().sharedMesh != null;
        }

        /// <summary>Finds the top of the actual loaded column, including structures and player edits.</summary>
        public bool TryGetSurfaceHeight(float worldX, float worldZ, out float surfaceHeight)
        {
            surfaceHeight = 0f;
            var position = new Vector3Int(Mathf.FloorToInt(worldX), 0, Mathf.FloorToInt(worldZ));
            Vector2Int coord = WorldToChunkCoord(position);
            if (!IsChunkReady(coord) || !_chunkDataCache.TryGetValue(coord, out var data))
                return false;

            int x = position.x - coord.x * GameConstants.ChunkWidth;
            int z = position.z - coord.y * GameConstants.ChunkDepth;
            for (int y = GameConstants.ChunkHeight - 1; y >= 0; y--)
            {
                if (data.GetBlock(x, y, z) == BlockType.Air) continue;
                surfaceHeight = y + 1f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the BlockType at the given world-space voxel coordinates.
        /// Returns BlockType.Air when the chunk is not loaded or coords are out of bounds.
        /// </summary>
        public BlockType GetBlock(int worldX, int worldY, int worldZ)
        {
            var chunkCoord = WorldToChunkCoord(new Vector3Int(worldX, worldY, worldZ));
            if (!_chunkDataCache.TryGetValue(chunkCoord, out var data)) return BlockType.Air;

            int lx = worldX - chunkCoord.x * GameConstants.ChunkWidth;
            int lz = worldZ - chunkCoord.y * GameConstants.ChunkDepth;

            if (!data.IsInBounds(lx, worldY, lz)) return BlockType.Air;
            return data.GetBlock(lx, worldY, lz);
        }

        /// <summary>
        /// Sets a block at a world position and marks the affected chunk (and boundary neighbors) dirty.
        /// </summary>
        public void SetBlock(Vector3Int worldPos, BlockType block)
        {
            BlockEditResult result = TrySetBlock(worldPos, block);
            if (result != BlockEditResult.Success)
                Debug.LogWarning($"[ChunkManager] Block edit at {worldPos} rejected: {result}.", this);
        }

        public BlockEditResult TrySetBlock(Vector3Int worldPos, BlockType block)
        {
            if (worldPos.y < 0 || worldPos.y >= GameConstants.ChunkHeight)
                return BlockEditResult.OutOfBounds;
            if (MasjidDistrictProtection.Contains(DistrictLayout, worldPos))
                return BlockEditResult.Protected;
            var chunkCoord = WorldToChunkCoord(worldPos);
            if (!_chunkDataCache.TryGetValue(chunkCoord, out var data)) return BlockEditResult.ChunkUnavailable;

            int lx = worldPos.x - chunkCoord.x * GameConstants.ChunkWidth;
            int lz = worldPos.z - chunkCoord.y * GameConstants.ChunkDepth;

            if (!data.IsInBounds(lx, worldPos.y, lz)) return BlockEditResult.OutOfBounds;

            data.SetBlock(lx, worldPos.y, lz, block);
            _editedChunks.Add(chunkCoord);
            DirtyChunk(chunkCoord);

            // Dirty neighbors when on a boundary
            if (lx == 0)                             DirtyChunk(new Vector2Int(chunkCoord.x - 1, chunkCoord.y));
            if (lx == GameConstants.ChunkWidth  - 1) DirtyChunk(new Vector2Int(chunkCoord.x + 1, chunkCoord.y));
            if (lz == 0)                             DirtyChunk(new Vector2Int(chunkCoord.x, chunkCoord.y - 1));
            if (lz == GameConstants.ChunkDepth  - 1) DirtyChunk(new Vector2Int(chunkCoord.x, chunkCoord.y + 1));
            return BlockEditResult.Success;
        }

        /// <summary>Marks a chunk as needing a mesh rebuild next frame.</summary>
        public void DirtyChunk(Vector2Int chunkCoord) => _dirtyChunks.Add(chunkCoord);

        /// <summary>
        /// Changes the world seed and clears all cached chunk data so the new seed
        /// takes effect on next generation. Call before the player enters the world
        /// (e.g., from WorldSaveManager.ApplyWorldState). All active views are unloaded.
        /// </summary>
        public void SetSeed(int seed)
            => ReplaceWorld(seed, null);

        public void ConfigureWorld(int seed, int districtVersion, Vector2Int districtOrigin, int districtFloorY)
        {
            MasjidDistrictLayout restored = districtVersion == 0 ? null :
                MasjidDistrictPlanner.Restore(seed, districtVersion, districtOrigin, districtFloorY);
            ReplaceWorld(seed, restored);
        }

        private void ReplaceWorld(int seed, MasjidDistrictLayout restored)
        {
            WorldReset?.Invoke();
            _worldSeed = seed;
            _chunkDataCache.Clear();
            _editedChunks.Clear();
            WorldRevision++;
            if (restored == null) PrepareDistrict();
            else DistrictLayout = restored;
            // Return all active views to the pool
            foreach (var kvp in _activeViews)
            {
                kvp.Value.Clear();
                _pool.Enqueue(kvp.Value);
            }
            _activeViews.Clear();
            _loadQueue.Clear();
            _queuedChunks.Clear();
            _dirtyChunks.Clear();
            _lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue); // force refresh
        }

        private void PrepareDistrict()
        {
            try
            {
                DistrictLayout = MasjidDistrictPlanner.Create(_worldSeed, bounds =>
                {
                    foreach (var pair in _chunkDataCache)
                    {
                        if (!_editedChunks.Contains(pair.Key) && !pair.Value.HouseFloorY.HasValue) continue;
                        int x = pair.Key.x * GameConstants.ChunkWidth;
                        int z = pair.Key.y * GameConstants.ChunkDepth;
                        if (x < bounds.xMax && x + GameConstants.ChunkWidth > bounds.xMin &&
                            z < bounds.zMax && z + GameConstants.ChunkDepth > bounds.zMin) return true;
                    }
                    return false;
                });
            }
            catch (System.InvalidOperationException ex)
            {
                DistrictLayout = null;
                Debug.LogError("[ChunkManager] Masjid district placement failed: " + ex.Message, this);
            }
        }

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

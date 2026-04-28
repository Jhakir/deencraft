# Phase 2 Status — Voxel Engine Core

**Status:** ✅ Complete  
**Date completed:** 2026-04-28  
**Commits:** 7 (c6b0883 → b8a8067)

---

## What Was Done

### Architecture

Implemented a layered voxel engine with clean separation of concerns:

```
Data Layer      → BlockType, ChunkData
Generation      → BiomeType, BiomeDefinition, BiomeSystem, WorldGenerator
Rendering       → MeshData, ChunkMesher
Orchestration   → ChunkView, ChunkManager
Interaction     → BlockInteractionManager, WaterSimulator
```

All 12 scripts committed to `Assets/Scripts/World/`.

### Task 1 — BlockType + GameConstants

- Added 8 world-generation constants to `GameConstants.cs`: `SeaLevel`, `BiomeNoiseScale`, `TerrainNoiseScale`, `TerrainOctaves`, `TerrainPersistence`, `MinTerrainHeight`, `MaxTerrainHeight`, `MaxWaterSpread`
- Created `BlockType` enum (13 values, Air–Wheat) matching existing `GameConstants.Block*` byte constants exactly
- 8 EditMode tests: ID correctness, sequential ordering, count, constant cross-checks

### Task 2 — ChunkData

- Pure C# voxel container: flat `byte[]` array (16×256×16 = 65,536 bytes per chunk)
- Flat index: `x + ChunkWidth * (y + ChunkHeight * z)`
- `IsInBounds`, `GetBlock`, `SetBlock` with `ArgumentOutOfRangeException` on OOB
- No Unity dependencies — fully testable in EditMode
- 13 EditMode tests: bounds, round-trips, corruption prevention, size check

### Task 3 — BiomeSystem

- `BiomeType` enum: Desert, Grassland, SnowyIsland, OliveGrove, Riverside
- `BiomeDefinition`: surface block, filler block, min/max height per biome
- `BiomeSystem`: two-channel Perlin noise (temperature × humidity) → BiomeType; seed-shifted
- 6 EditMode tests: determinism, all 5 biomes reachable, valid block types, height ranges

### Task 4 — WorldGenerator

- Pure static class: `Generate(ChunkData, chunkX, chunkZ, seed)`
- 3-octave Perlin noise height field using all terrain GameConstants
- Y=0 always Stone; filler block 3 layers below surface; water fills below SeaLevel
- Biome-correct surface/filler blocks per column
- 5 EditMode tests: determinism, Y=0 stone, max height air, no air below sea level, coord variance

### Task 5 — ChunkMesher + MeshData

- `MeshData`: immutable container (Vector3[] Vertices, int[] Triangles, Vector2[] UVs)
- `ChunkMesher.BuildMesh`: 6-pass greedy meshing (3 axes × 2 face directions)
- Face culling via neighbor chunk array `[+X, -X, +Z, -Z]`; null = treat as exposed
- UV atlas tiling: `blockId / 13` per block row
- 6 EditMode tests: all-air → 0 verts, single block → 24 verts/36 tris, adjacent merge, water faces, full-chunk no-throw, Empty static

### Task 6 — ChunkManager + ChunkView

- `ChunkView`: lightweight MonoBehaviour with `ApplyMesh(MeshData)` and `Clear()`; pooled only
- `ChunkManager`: singleton with pre-allocated pool (200 GameObjects), 3 coroutine loops:
  - `ChunkUpdateLoop`: detects player movement, queues load/unload
  - `LoadQueueProcessor`: one chunk per frame to spread load cost
  - `MeshRebuildLoop`: rebuilds dirty chunks one per frame
- `SetBlock(Vector3Int, BlockType)`: modifies data + dirty-marks chunk + boundary neighbors
- `WorldToChunkCoord(Vector3Int)`: public static utility

### Task 7 — BlockInteractionManager

- MonoBehaviour on Player: raycast from camera, left-click destroy, right-click place
- Max range: `GameConstants.PlayerReach` (5f)
- Phase 4 stub: selected block returns Stone until `PlayerInventory` (Phase 4) is built

### Task 8 — WaterSimulator

- MonoBehaviour alongside ChunkManager
- BFS spread: flows down first, then horizontally; max `MaxWaterSpread` blocks from source
- One BFS step per tick (`_waterTickInterval = 0.1f`); runs as coroutine
- Only player-placed water triggers simulation; world-gen water is static

---

## Test Coverage

| Test File | Tests | Coverage |
|---|---|---|
| `BlockTypeTests.cs` | 8 | Enum values, sequential IDs, constant cross-check |
| `ChunkDataTests.cs` | 13 | Bounds, round-trips, corruption, size |
| `BiomeSystemTests.cs` | 6 | Determinism, all 5 biomes, valid definitions |
| `WorldGeneratorTests.cs` | 5 | Determinism, Y=0, sea level fill, height caps |
| `ChunkMesherTests.cs` | 6 | Empty, single block, greedy merge, water, full chunk |
| **Total Phase 2 new tests** | **38** | |
| **Cumulative (Phase 1 + 2)** | **107** | |

---

## Errors Encountered

### None in Phase 2

All tasks completed cleanly. The BlockType enum IDs were adjusted from the initial spec to match the pre-existing `GameConstants.Block*` values (Grass=1, Stone=3, etc. — not the canonical Minecraft order originally planned). This kept the block ID mapping consistent with Phase 1 constants.

---

## What to Keep in Mind for Phase 3

### Phase 2 API contracts Phase 3 must respect

1. **BlockType enum is locked.** Grass=1, Stone=3. New block types must be added to BOTH `BlockType.cs` AND `GameConstants.Block*` constants, with sequential IDs starting at 13.

2. **WorldGenerator is the only source of world-gen truth.** Phase 3 terrain features (trees, structures, rivers) should be generated by extending `WorldGenerator.Generate` or via a separate `FeatureGenerator` pass that runs after `WorldGenerator.Generate`.

3. **ChunkManager.SetBlock** is the only correct way to modify blocks at runtime. Never access `ChunkData` directly outside of WorldGenerator and ChunkMesher.

4. **`DirtyChunk` must be called after any SetBlock.** ChunkManager.SetBlock handles this automatically; if a FeatureGenerator writes directly to ChunkData, it must call `DirtyChunk` for every modified chunk.

5. **Pool ceiling is 200 GameObjects.** `_viewDistance = 8` → (2×8+1)² = 289 chunks in a ring. If viewDistance is increased in Phase 3, raise `_poolSize` to match: `_poolSize ≥ (2*viewDistance+1)²`.

6. **UV atlas row = blockId / 13.** Adding block types beyond ID 12 requires updating `TotalBlockTypes = 13` in `ChunkMesher.cs` and re-exporting the texture atlas.

7. **WaterSimulator.TriggerSpread** must be called by any system that places a Water block (e.g., river generation feature, player water placement).

8. **BiomeSystem thresholds.** The current biome classification (Perlin noise temperature × humidity) covers all 5 biomes with seed 42. New biome noise scales or threshold changes must be validated against the `AllFiveBiomes_AreReachable` test.

### Phase 3 planned content

- Terrain features: mountains, rivers, ocean/lake areas
- Trees: olive trees, apple trees, palm trees, regular trees
- Plants: flowers, grass tufts, wheat
- Structures: auto-generated villages (mud-brick houses)
- Water bodies with boat traversal
- Snow and desert biome detail blocks (ice, cacti, oasis)

# Phase 2: Voxel Engine Core — Design Spec

**Date:** 2026-04-28  
**Status:** Approved  
**Phase:** 2 of 9

---

## Goal

Build a chunk-based voxel world engine inside Unity 2022 LTS capable of:
- Rendering an infinite procedurally-generated 3D world with 5 biomes
- Greedy-meshed chunk geometry at ≥ 30 FPS in WebGL
- Block placement and destruction by the player
- Water flow simulation
- Full EditMode test coverage for all pure-logic layers

---

## Architecture — Layered Subsystems

All scripts live in `Assets/Scripts/World/` under the `DeenCraft.World` assembly.

```
Data Layer (no Unity dependencies — fully EditMode-testable)
  BlockType.cs          — enum BlockType + BlockDefinition struct
  ChunkData.cs          — 3D voxel array, get/set, in-bounds checks

Generation Layer (pure C# — fully EditMode-testable)
  BiomeType.cs          — enum BiomeType
  BiomeDefinition.cs    — data class: surface block, filler block, height range
  BiomeSystem.cs        — maps (x,z) world coords → BiomeType via Perlin noise
  WorldGenerator.cs     — fills ChunkData from biome + height noise

Rendering Layer (pure C# mesh math — EditMode-testable)
  ChunkMesher.cs        — greedy meshing algorithm, produces MeshData

Orchestration Layer (Unity MonoBehaviour)
  ChunkManager.cs       — chunk pool, load/unload around player, coroutine scheduler

Interaction Layer (Unity MonoBehaviours)
  BlockInteractionManager.cs — raycast block place/destroy, dirty-mark chunks
  WaterSimulator.cs          — BFS water spread coroutine
```

---

## Data Layer

### BlockType enum (extends GameConstants block IDs 0–12)

```
Air = 0, Stone = 1, Dirt = 2, Grass = 3, Sand = 4, Snow = 5,
Water = 6, Wood = 7, Leaves = 8, MosaicTile = 9,
Bedrock = 10, Gravel = 11, OliveWood = 12
```

All block IDs must match `GameConstants.BlockId*` constants exactly.

### ChunkData

- Internal storage: `byte[] _blocks` (flat array, index = `x + width*(y + height*z)`)
- Size: `GameConstants.ChunkWidth × GameConstants.ChunkHeight × GameConstants.ChunkDepth` (16×256×16 = 65,536 bytes per chunk)
- Methods: `GetBlock(int x, int y, int z)`, `SetBlock(int x, int y, int z, byte blockId)`, `IsInBounds(int x, int y, int z)`
- Immutable dimensions — no constructor overloads with custom sizes

---

## Generation Layer

### BiomeSystem

- Two independent Perlin noise values at (x,z): **temperature** and **humidity**
- Biome map:

| Biome | Temperature | Humidity |
|---|---|---|
| Desert | high | low |
| Grassland | mid | mid |
| SnowyIsland | low | any |
| OliveGrove | mid-high | mid-high |
| Riverside | any | high |

- Exposed method: `BiomeType GetBiome(float worldX, float worldZ, int seed)`
- Each biome has: surface block, filler block (2–4 layers), stone below, ground height range

### WorldGenerator

- Takes `ChunkData` (empty), chunk coordinate (chunkX, chunkZ), and seed
- Uses `UnityEngine.Mathf.PerlinNoise` for height (octave sum: 3 octaves, persistence 0.5)
- Uses `BiomeSystem.GetBiome` to select surface/filler blocks per column
- Fills from y=0 (bedrock) → y=height (surface)
- Water fills air below `GameConstants.SeaLevel` (new constant: 64)
- Does NOT touch `GameObject` or `Transform` — pure data

---

## Rendering Layer

### ChunkMesher (Greedy Meshing)

Algorithm per face direction (6 passes):
1. Sweep a 2D plane across the chunk in the current axis direction
2. For each slice, build a 2D mask of visible face IDs (0 = no face needed)
3. Greedy-expand each non-zero mask cell into the largest rectangle of the same block type
4. Emit one quad per rectangle, mark those cells consumed
5. Repeat for next slice

Output: `MeshData` struct — `Vector3[] vertices`, `int[] triangles`, `Vector2[] uvs`  
UV tiling: one texture atlas, each block type has a fixed row in the atlas (UV = `blockId / totalBlockTypes`)

`ChunkMesher` is a static class with a single entry point:
```csharp
public static MeshData BuildMesh(ChunkData chunk, ChunkData[] neighbors)
```
`neighbors` is a 4-element array [+X, -X, +Z, -Z] — used to cull faces at chunk boundaries.

---

## Orchestration Layer

### ChunkManager (MonoBehaviour singleton)

Responsibilities:
- Maintain a pool of `ChunkView` GameObjects (pre-allocated, never Instantiate/Destroy)
- On player move: compute which chunks are within `_viewDistance` radius (in chunk coords)
- Load new chunks into scope: call `WorldGenerator`, then `ChunkMesher`, apply mesh to `ChunkView`
- Unload out-of-scope chunks: return `ChunkView` to pool, mark data stale
- Chunk data cache: `Dictionary<Vector2Int, ChunkData>` (keep loaded + adjacent)
- Coroutine scheduler: one new chunk per frame (spread load over time)

`ChunkView`: lightweight `MonoBehaviour` on pooled GameObjects — holds `MeshFilter` + `MeshRenderer`, exposes `ApplyMesh(MeshData data)` and `Clear()`

Inspector fields:
- `[SerializeField] private int _viewDistance = 8` (chunks)
- `[SerializeField] private int _poolSize = 200`
- `[SerializeField] private int _worldSeed = 42`
- `[SerializeField] private Material _worldMaterial`

---

## Interaction Layer

### BlockInteractionManager (MonoBehaviour)

- On left-click: raycast from camera, call `ChunkManager.SetBlock(worldPos, BlockType.Air)` → destroy block
- On right-click: raycast, place selected hotbar block adjacent to hit face
- Max raycast range: `GameConstants.BlockInteractionRange` (new constant: 5 units)
- After any set: dirty-mark the affected chunk (and neighbors if on boundary) → re-mesh next frame
- Does NOT handle inventory — reads `PlayerInventory.SelectedBlock` (Phase 4 stub: always returns Stone for now)

### WaterSimulator (MonoBehaviour)

- Runs as a coroutine on `ChunkManager`
- BFS spread: each water block checks 4 horizontal neighbors + 1 below; if air → set water, enqueue
- One BFS step per frame tick (spread rate controllable via `_waterTicksPerSecond`)
- Max water spread distance: `GameConstants.MaxWaterSpread` (new constant: 16 blocks)
- Water does not flow uphill
- Water placed by player triggers simulation; natural world-gen water is static (already filled by WorldGenerator)

---

## Constants to Add to GameConstants.cs

```csharp
public const int SeaLevel = 64;
public const int BlockInteractionRange = 5;
public const int MaxWaterSpread = 16;
public const float BiomeNoiseScale = 0.003f;
public const float TerrainNoiseScale = 0.004f;
public const int TerrainOctaves = 3;
public const float TerrainPersistence = 0.5f;
public const int MinTerrainHeight = 60;
public const int MaxTerrainHeight = 120;
```

---

## Test Strategy

| Layer | Test type | What to test |
|---|---|---|
| BlockType | EditMode | All IDs match GameConstants, no ID gaps |
| ChunkData | EditMode | Get/set, bounds, flat index math |
| BiomeSystem | EditMode | Same seed → same biome, all 5 biomes reachable |
| WorldGenerator | EditMode | Same seed+coord → identical ChunkData, sea level water fill |
| ChunkMesher | EditMode | Air-only chunk → 0 faces, single block → correct face count |
| ChunkManager | PlayMode smoke | Generates 1 chunk, mesh is non-null |

---

## File Map

| File | Create/Modify | Assembly |
|---|---|---|
| `Assets/Scripts/GameConstants.cs` | Modify | DeenCraft.Core |
| `Assets/Scripts/World/BlockType.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/ChunkData.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/BiomeType.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/BiomeDefinition.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/BiomeSystem.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/WorldGenerator.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/MeshData.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/ChunkMesher.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/ChunkView.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/ChunkManager.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/BlockInteractionManager.cs` | Create | DeenCraft.World |
| `Assets/Scripts/World/WaterSimulator.cs` | Create | DeenCraft.World |
| `Assets/Tests/EditMode/WorldTests/BlockTypeTests.cs` | Create | DeenCraft.Tests.EditMode |
| `Assets/Tests/EditMode/WorldTests/ChunkDataTests.cs` | Create | DeenCraft.Tests.EditMode |
| `Assets/Tests/EditMode/WorldTests/BiomeSystemTests.cs` | Create | DeenCraft.Tests.EditMode |
| `Assets/Tests/EditMode/WorldTests/WorldGeneratorTests.cs` | Create | DeenCraft.Tests.EditMode |
| `Assets/Tests/EditMode/WorldTests/ChunkMesherTests.cs` | Create | DeenCraft.Tests.EditMode |

---

## Constraints (from Phase 1 status)

- Chunk size is **locked**: 16×16×256. Never use literal numbers — always `GameConstants.Chunk*`.
- No `public` fields anywhere — `[SerializeField] private` for inspector, `private` otherwise.
- No `Instantiate`/`Destroy` for chunks — pool only.
- Coroutines for all game-side async.
- `DeenCraft.World` asmdef already exists and already references `DeenCraft.Core`.
- All files must declare `namespace DeenCraft.World` (or `DeenCraft` for GameConstants).

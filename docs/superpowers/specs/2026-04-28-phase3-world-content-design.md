# Phase 3: World Content — Design Spec
**Date:** 2026-04-28  
**Status:** Approved (Option A — full scope including boat placeholder and village interiors)

---

## Goals

Add rich, biome-aware world content to the voxel engine built in Phase 2:
- New block types for terrain, structures, and decoration
- Procedural feature generation: trees, plants, villages, oases
- Terrain enhancement: rivers, desert dunes, biome-height variation
- Static boat placeholder near water (ride logic deferred to Phase 5)

---

## Architecture Decision: Self-Contained Features

Features (trees, structures, oases) are **self-contained per chunk**. No cross-chunk writes.

- Trees are planted only at column positions ≥ 2 blocks from the chunk edge (x ∈ [2,13], z ∈ [2,13])
- Village houses have a max footprint of 7×5 blocks — easily fits in 16×16
- Oasis (5×5 water + 4 palms) is planted at centre of chunk if eligible

This avoids any inter-chunk coordination while producing visually plausible content.

---

## New Block Types

IDs 0–12 already exist (Air → Wheat). Phase 3 adds 7 new types:

| ID | Name       | Usage                                      |
|----|------------|--------------------------------------------|
| 13 | Cactus     | Desert biome, 1–3 tall column              |
| 14 | MudBrick   | Village walls, floors, interiors           |
| 15 | PalmWood   | Palm tree trunk (Desert + Riverside)       |
| 16 | OliveLeaves| Olive tree canopy (OliveGrove biome)       |
| 17 | Flower     | Decorative plant (Grassland + Riverside)   |
| 18 | Boat       | Static placeholder near water (Riverside)  |
| 19 | Thatch     | Village roof                               |

**TotalBlockTypes updated:** 13 → 20 in ChunkMesher.cs

---

## Feature Generator Architecture

New static class: `Assets/Scripts/World/FeatureGenerator.cs`

```
FeatureGenerator.Decorate(ChunkData chunk, int chunkX, int chunkZ, int seed)
  ├── PlantTrees(chunk, chunkX, chunkZ, seed, biome, rng)
  ├── PlantVegetation(chunk, chunkX, chunkZ, biome, rng)
  ├── PlaceStructures(chunk, chunkX, chunkZ, seed, biome, rng)
  └── PlaceBoat(chunk, chunkX, chunkZ, rng)          [Riverside only]
```

Called at the end of `WorldGenerator.Generate(...)`, after terrain is filled.

### Seeding

Each chunk gets a deterministic RNG:
```csharp
int hashSeed = chunkX * 1000003 ^ chunkZ * 1000033 ^ seed;
var rng = new System.Random(hashSeed);
```

---

## Trees

### Tree Types by Biome

| Biome       | Tree Types                      | Density (noise threshold) |
|-------------|---------------------------------|---------------------------|
| Grassland   | Regular (Wood + Leaves)         | 0.65                      |
| OliveGrove  | Olive (Wood + OliveLeaves)      | 0.60                      |
| Desert      | Palm (PalmWood + Leaves) at oasis only | n/a (oasis only)   |
| Riverside   | Palm (PalmWood + Leaves)        | 0.70                      |
| SnowyIsland | Regular (Wood + Snow-capped top)| 0.55                      |

### Tree Shapes

**Regular tree** (height 4–6):
- Trunk: Wood column, height h
- Canopy: Leaves, 3×3 at h-1 and h, 1×1 cap at h+1

**Olive tree** (height 3–5):
- Trunk: Wood column, height h
- Canopy: OliveLeaves, 3×3 at h and h-1

**Palm tree** (height 5–7):
- Trunk: PalmWood column, height h, no branching
- Top: Leaves cross pattern at h (1×3 + 3×1, centre overlap)

**Snow tree** (SnowyIsland):
- Same as Regular but top 2 canopy layers have Snow blocks on top of Leaves

### Placement Rule
- Column must be ≥ 2 from chunk edge: x ∈ [2, 13], z ∈ [2, 13]
- Must be planted on Grass or Sand surface
- Tree noise: `Mathf.PerlinNoise((chunkX*16+x)*0.1f, (chunkZ*16+z)*0.1f) > threshold`
- Max 3 trees per chunk to limit density

---

## Vegetation (Plants)

Placed in the same pass after trees. One scan over all eligible surface columns.

| Block   | Biome(s)           | Surface    | Noise threshold |
|---------|--------------------|------------|-----------------|
| Flower  | Grassland, Riverside| Grass      | 0.78            |
| Wheat   | Riverside          | Grass      | 0.72            |
| Flower  | OliveGrove         | Grass      | 0.74            |
| Cactus  | Desert             | Sand       | 0.70 (1–3 tall) |

Vegetation is placed directly on the surface block (y = surfaceY + 1). Air check required above surface before placing.

---

## Structures: Village Houses

### Eligibility
- Biomes: Grassland, OliveGrove, Riverside
- Village noise: `Mathf.PerlinNoise(chunkX * 0.3f + seed * 0.001f, chunkZ * 0.3f) > 0.70`
- One house per eligible chunk

### House Layout (7 wide × 5 deep × 4 tall)
```
Top-down view (7×5 footprint):
MMMMMM  M = MudBrick wall
M    M  (space) = Air interior
M    M
MMMMMM
```
- Walls: MudBrick (ID 14), 4 layers tall
- Roof: Thatch (ID 19) covering full 7×5 footprint
- Floor: MudBrick (ID 14)
- Door: Air block cutout in south wall, 2 blocks tall (centre column)
- Interior Shelf: Wood block (ID 7) along north interior wall at y=floor+1 (shelf row)
- Window: Air block cutout in east wall at y=floor+2 (1 block)

### House Placement
- Anchored at chunk-local x=4, z=4 (provides ≥4 block buffer from edge)
- Surface height sampled at anchor point; house placed at `surfaceY`

---

## Terrain Enhancement

### River Carving (Riverside biome)
- River runs in Z direction through the chunk
- Channel: x ∈ [6, 10] (5 wide), carved to y = SeaLevel - 2
- Channel floor: Sand (ID 4)
- Channel filled: Water (ID 6) from SeaLevel-2 to SeaLevel-1
- World-gen water is **static** — WaterSimulator is NOT triggered

### Desert Dune Variation
- Second Perlin octave added to height in Desert biome
- `duneBump = Mathf.PerlinNoise(worldX * 0.08f, worldZ * 0.08f) * 8f`
- Applied inside WorldGenerator per-column after biome determination

### Oasis (Desert biome)
- Oasis noise: `Mathf.PerlinNoise(chunkX * 0.5f + seed * 0.001f, chunkZ * 0.5f) > 0.85`
- Placed at chunk centre (x=5..10, z=5..10), 6×6 area
- Carve to SeaLevel, fill with Water (ID 6) — static water
- Place 4 Palm trees at corners of 6×6 area (if ≥2 from edge — they are at x=5,z=5 etc.)
- Sand border (1 block) around water

### Snow Island Enhancement
- SnowyIsland biome surface already uses Snow (ID 5)
- Regular trees get snow caps: after placing tree canopy, place Snow on top of topmost Leaves layer
- No new terrain shape changes needed (biome height is already handled by WorldGenerator)

---

## Boat Placeholder (Riverside biome)

- Static Boat block (ID 18) placed at the river bank (water edge + 1 block east)
- Only if village noise > 0.70 (near village, one per village chunk)
- No interaction logic — Phase 5 converts this to a rideable entity

---

## Files Changed

### New Files
| File | Responsibility |
|------|----------------|
| `Assets/Scripts/World/FeatureGenerator.cs` | Static feature placement: trees, vegetation, structures, oasis, boats |
| `Assets/Tests/EditMode/WorldTests/FeatureGeneratorTests.cs` | Unit tests for all FeatureGenerator features |

### Modified Files
| File | Change |
|------|--------|
| `Assets/Scripts/World/BlockType.cs` | Add Cactus=13 … Thatch=19 |
| `Assets/Scripts/GameConstants.cs` | Add BlockCactus=13 … BlockThatch=19; add world-gen constants |
| `Assets/Scripts/World/ChunkMesher.cs` | Update `TotalBlockTypes = 20` |
| `Assets/Scripts/World/WorldGenerator.cs` | Add biome-height dune variation; call FeatureGenerator.Decorate at end of Generate |

---

## Test Plan

### BlockType Tests (add to existing BlockTypeTests.cs)
- New enum values map to correct byte constants
- TotalBlockTypes = 20

### FeatureGeneratorTests.cs (new file)
- `Decorate_Grassland_ContainsAtLeastOneTree` — chunk seeded for guaranteed tree
- `Decorate_Desert_ContainsCactus` — cactus in desert column
- `Decorate_Riverside_HasRiverWater` — river channel has water at SeaLevel
- `Decorate_Desert_OasisHasWater` — oasis chunk has water at centre
- `Decorate_EligibleVillageChunk_HasMudBrickWall` — village house placed
- `PlantTrees_NeverPlantsWithin2BlocksOfEdge` — column ≥ 2 from edge
- `PlantTrees_TrunkIsWoodOrPalmWood` — tree trunk type matches biome
- `Decorate_Riverside_HasBoatBlock` — boat placed at river edge in eligible chunk
- `Decorate_SnowyIsland_HasSnowOnTreeTop` — snow cap on trees

---

## What's Deferred

| Item | Deferred To |
|------|-------------|
| Boat ride mechanic (board/steer) | Phase 5 (Entities) |
| Village interior interaction (open doors, sit on chair) | Phase 4/5 |
| Mosque auto-generation | Phase 6 (Islamic Cultural Content) |
| Fruit harvesting from olive/apple trees | Phase 6 |
| Multiplayer world sync | Phase v2 |

# Phase 3 Status: World Content

**Completed:** 2026-04-28  
**Commit:** eed3006  
**Branch:** main

---

## What Was Built

### New Block Types (IDs 13–19)
| ID | Name        | Usage                                      |
|----|-------------|--------------------------------------------|
| 13 | Cactus      | Desert biome, 1–3 tall column              |
| 14 | MudBrick    | Village walls, floors, roof interiors      |
| 15 | PalmWood    | Palm tree trunks (desert oasis, riverside) |
| 16 | OliveLeaves | Olive tree canopy (OliveGrove biome)       |
| 17 | Flower      | Decorative (Grassland, Riverside, OliveGrove) |
| 18 | Boat        | Static placeholder near rivers (Phase 5 activates) |
| 19 | Thatch      | Village roof                               |

`TotalBlockTypes` in `ChunkMesher.cs` updated to 20.

### FeatureGenerator (new static class)
`Assets/Scripts/World/FeatureGenerator.cs`

- `Decorate(ChunkData, chunkX, chunkZ, seed)` — called at end of WorldGenerator.Generate
- **Trees** — self-contained (planted only at columns x∈[2,13], z∈[2,13]):
  - Regular tree: Wood trunk + Leaves canopy, Grassland biome
  - Olive tree: Wood trunk + OliveLeaves, OliveGrove biome
  - Palm tree: PalmWood trunk + cross-top Leaves, Riverside biome
  - Snow tree: Wood trunk + Leaves + Snow cap, SnowyIsland biome
- **Vegetation** — full chunk scan:
  - Flowers on Grass surface: Grassland, Riverside, OliveGrove
  - Wheat on Grass surface: Riverside
  - Cactus 1–3 tall on Sand: Desert
- **Village houses** (7×5×4, MudBrick walls, Thatch roof, Wood shelf): Grassland, OliveGrove, Riverside; one per eligible chunk (VillageNoise > 0.70)
- **Oasis** (5×5 static water + 4 palms + sand border): Desert; one per eligible chunk (OasisNoise > 0.85)
- **Boat placeholder**: Static Boat block at river east bank, Riverside village chunks

### WorldGenerator Enhancements
`Assets/Scripts/World/WorldGenerator.cs`

- Desert dune height variation: second Perlin octave adds up to 8 blocks of height variation
- River carving: Riverside chunks get a 5-wide (x=6..10) water channel carved to SeaLevel-2, Sand floor, static Water fill
- `FeatureGenerator.Decorate(data, chunkX, chunkZ, seed)` called at end of Generate

### GameConstants Additions
- `BlockCactus=13` through `BlockThatch=19`
- `TreeNoiseScale=0.1f`, `VillageNoiseScale=0.3f`, `VillageNoiseThreshold=0.70f`
- `OasisNoiseThreshold=0.85f`, `DuneNoiseScale=0.08f`, `DuneHeight=8f`
- `RiverXMin=6`, `RiverXMax=10`, `MaxTreesPerChunk=3`
- `HouseWidth=7`, `HouseDepth=5`, `HouseHeight=4`, `HouseAnchorX=4`, `HouseAnchorZ=4`

---

## Test Coverage

| Test File | Tests | Notes |
|---|---|---|
| `FeatureGeneratorTests.cs` | 9 | New in Phase 3 |

**9 new tests:**
1. `Decorate_AllChunks_GenerateWithoutException` — smoke test, 25 chunks
2. `Decorate_Grassland_ContainsAtLeastOneTree` — trees in grassland
3. `Decorate_Desert_CactusBlocksAreOnSandOrCactus` — cactus placement validity
4. `Decorate_Riverside_HasRiverWater` — river channel has water
5. `Decorate_EligibleVillageChunk_HasMudBrickWall` — village house placed
6. `Decorate_TreeTrunks_NeverWithin2BlocksOfEdge` — edge constraint (x∈[2,13])
7. `Decorate_VegetationFlowers_AlwaysOnSolidBlock` — no floating flowers
8. `Decorate_SnowyIsland_NoDesertBlocksGenerated` — biome separation
9. `Decorate_MaxTreesPerChunk_NotExceeded` — density cap

**Cumulative test count:**
- Phase 1: 69 tests
- Phase 2: 38 tests
- Phase 3: 9 tests
- **Total: 116 tests**

---

## Decisions Made

### Self-Contained Features (Option A)
Trees, structures, and oases are fully self-contained per chunk. No cross-chunk writes. Trees only planted at columns 2–13 in both axes.

### Boat as Static Block
`BlockType.Boat` (ID 18) is a placeholder block placed near the river in Riverside village chunks. Actual rideable boat entity is deferred to Phase 5 (Entities).

### Village = 1 House Per Eligible Chunk
Rather than multi-chunk village layouts, each eligible chunk gets exactly one 7×5×4 MudBrick house. Adjacent eligible chunks naturally cluster into a village feel.

### World-Gen Water is Static
River channels and oasis pools use static Water blocks. `WaterSimulator.TriggerSpread` is NOT called — world-gen water does not simulate flow.

---

## What to Keep in Mind for Phase 4

### API Contracts Phase 4 Must Respect

1. **BlockType enum now has 20 values (0–19).** Any new block types added in Phase 4 must start at ID 20, update `TotalBlockTypes` in ChunkMesher, and add `GameConstants.Block*` constants.

2. **FeatureGenerator.Decorate is called inside WorldGenerator.Generate.** Phase 4 should not call it separately.

3. **River channel is at x=6..10, y=62..63 in Riverside chunks.** Player physics must handle swimming in Water blocks (SeaLevel=64 is the reference).

4. **Boat block (ID 18) is static.** When Phase 5 adds boat entity, the FeatureGenerator.PlaceBoat should be updated to spawn a BoatEntity instead of placing a static block.

5. **Village house floor is at surface level.** Door is 2 blocks tall on south face. Player needs to be able to enter (clear 2-block gap). Phase 4 player height is 1.8f — door clears this.

### Phase 4 Scope (from plan)
- Third-person player controller (WASD + mouse look)
- Character customizer (skin tone, hijab/kufi, clothing)
- Character creation screen
- Animation states
- Inventory system (hotbar + backpack)
- Crafting system
- Health/hunger system

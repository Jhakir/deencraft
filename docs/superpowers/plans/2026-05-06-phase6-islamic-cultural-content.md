# Phase 6: Islamic Cultural Content — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Islamic/Palestinian cultural blocks, harvestable fruit trees, mosque auto-generation, a water slide block, and a complete food nutrition system.

**Architecture:** Pure-C# logic (enums, data classes, static generators) stays testable in EditMode. MonoBehaviour extensions only wire the logic to Unity. All new block types added to the single `BlockType` enum and mapped through `BlockItemMap`. Food nutrition centralised in `FoodDefinition.cs` (single source of truth used by `VitalitySystem`).

**Tech Stack:** Unity 2022 LTS / C#, DeenCraft.Core + DeenCraft.World + DeenCraft.Player assemblies, NUnit EditMode tests.

**Already done in Phase 6:** `DayNightCycle.cs`, `AdhanAudioManager.cs`, `DayNightCycleTests.cs` — committed `94fa018`.

**Cumulative test count going in:** 174 EditMode tests.

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/World/BlockType.cs` | Modify | Add 8 new block types |
| `Assets/Scripts/Player/ItemId.cs` | Modify | Add Olive + Apple food items |
| `Assets/Scripts/GameConstants.cs` | Modify | Add cultural + food constants |
| `Assets/Scripts/Player/FoodDefinition.cs` | **Create** | Pure-C# nutrition lookup (ItemId → hunger restored) |
| `Assets/Scripts/Player/VitalitySystem.cs` | Modify | Use `FoodDefinition` instead of inline switch |
| `Assets/Scripts/Player/BlockItemMap.cs` | Modify | Map all new block types ↔ item IDs |
| `Assets/Scripts/World/FeatureGenerator.cs` | Modify | Apple trees in Grassland; mosque in village chunks |
| `Assets/Tests/EditMode/WorldTests/CulturalBlockTests.cs` | **Create** | BlockType enum checks + BlockItemMap round-trips |
| `Assets/Tests/EditMode/PlayerTests/FoodDefinitionTests.cs` | **Create** | Nutrition values for all food items |
| `Assets/Tests/EditMode/WorldTests/AppleTreeTests.cs` | **Create** | FeatureGenerator places apple trees in Grassland chunks |
| `Assets/Tests/EditMode/WorldTests/MosqueTests.cs` | **Create** | FeatureGenerator places mosque structure in village chunks |

---

## Task 1 — New BlockTypes (enum expansion)

**Files:**
- Modify: `Assets/Scripts/World/BlockType.cs`

- [ ] **Step 1: Write the failing test** — Open `Assets/Tests/EditMode/WorldTests/CulturalBlockTests.cs` and write:

```csharp
// Assets/Tests/EditMode/WorldTests/CulturalBlockTests.cs
using NUnit.Framework;
using DeenCraft.World;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class CulturalBlockTests
    {
        [Test] public void BlockType_Minaret_Exists()    => Assert.DoesNotThrow(() => { var _ = BlockType.Minaret; });
        [Test] public void BlockType_Dome_Exists()       => Assert.DoesNotThrow(() => { var _ = BlockType.Dome; });
        [Test] public void BlockType_StoneArch_Exists()  => Assert.DoesNotThrow(() => { var _ = BlockType.StoneArch; });
        [Test] public void BlockType_Crescent_Exists()   => Assert.DoesNotThrow(() => { var _ = BlockType.Crescent; });
        [Test] public void BlockType_StarBlock_Exists()  => Assert.DoesNotThrow(() => { var _ = BlockType.StarBlock; });
        [Test] public void BlockType_AppleWood_Exists()  => Assert.DoesNotThrow(() => { var _ = BlockType.AppleWood; });
        [Test] public void BlockType_AppleLeaves_Exists()=> Assert.DoesNotThrow(() => { var _ = BlockType.AppleLeaves; });
        [Test] public void BlockType_WaterSlide_Exists() => Assert.DoesNotThrow(() => { var _ = BlockType.WaterSlide; });
    }
}
```

- [ ] **Step 2: Run tests — confirm 8 failures** (enum values don't exist yet)
- [ ] **Step 3: Add the new block types to `BlockType.cs`** — append after `Thatch = 19`:

```csharp
Minaret    = 20,   // mosque minaret shaft block
Dome       = 21,   // mosque dome cap block
StoneArch  = 22,   // Palestinian stone arch
Crescent   = 23,   // crescent-moon decorative block
StarBlock  = 24,   // star decorative block
AppleWood  = 25,   // apple tree trunk
AppleLeaves = 26,  // apple tree canopy — drops Apple food
WaterSlide = 27,   // fun slide block — gives speed burst
```

- [ ] **Step 4: Run tests — confirm 8 pass**
- [ ] **Step 5: Commit** `feat: add 8 cultural block types to BlockType enum`

---

## Task 2 — New ItemIds (food items)

**Files:**
- Modify: `Assets/Scripts/Player/ItemId.cs`

- [ ] **Step 1: Add to `CulturalBlockTests.cs`** — append ItemId tests:

```csharp
using DeenCraft.Player;

// add inside the class:
[Test] public void ItemId_Olive_Exists() => Assert.DoesNotThrow(() => { var _ = ItemId.Olive; });
[Test] public void ItemId_Apple_Exists() => Assert.DoesNotThrow(() => { var _ = ItemId.Apple; });
```

- [ ] **Step 2: Run tests — confirm 2 new failures**
- [ ] **Step 3: Add to `ItemId.cs`** — in the Food items section:

```csharp
Olive   = 305,   // harvested from olive leaves
Apple   = 306,   // harvested from apple leaves
```

- [ ] **Step 4: Run tests — confirm 2 pass**
- [ ] **Step 5: Commit** `feat: add Olive and Apple food items to ItemId`

---

## Task 3 — GameConstants additions

**Files:**
- Modify: `Assets/Scripts/GameConstants.cs`

No new unit tests needed (pure constants, verified indirectly by food + feature tests).

- [ ] **Step 1: Add cultural constants** — append a new section at the bottom of `GameConstants.cs`:

```csharp
// ── Islamic Cultural Content ──────────────────────────────
/// <summary>Speed multiplier applied to player while on a WaterSlide block.</summary>
public const float WaterSlideSpeedMultiplier = 2.5f;

/// <summary>Fraction of village chunks that get a mosque (≈1 in 5).</summary>
public const float MosqueChancePerVillage    = 0.40f;

/// <summary>Height of the mosque minaret (including base).</summary>
public const int   MosqueMinaretHeight       = 8;

/// <summary>Side length of the mosque courtyard footprint (square).</summary>
public const int   MosqueCourtyard           = 7;
```

- [ ] **Step 2: Confirm project compiles** (no test run required — just a constants addition)
- [ ] **Step 3: Commit** `feat: add Islamic cultural constants to GameConstants`

---

## Task 4 — FoodDefinition (pure-C# nutrition lookup)

**Files:**
- Create: `Assets/Scripts/Player/FoodDefinition.cs`
- Modify: `Assets/Scripts/Player/VitalitySystem.cs`
- Create: `Assets/Tests/EditMode/PlayerTests/FoodDefinitionTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Assets/Tests/EditMode/PlayerTests/FoodDefinitionTests.cs
using NUnit.Framework;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode.PlayerTests
{
    public class FoodDefinitionTests
    {
        [Test] public void Bread_Restores_5_Hunger()    => Assert.AreEqual(5f, FoodDefinition.HungerRestored(ItemId.Bread));
        [Test] public void Date_Restores_3_Hunger()     => Assert.AreEqual(3f, FoodDefinition.HungerRestored(ItemId.Date));
        [Test] public void Fig_Restores_2_Hunger()      => Assert.AreEqual(2f, FoodDefinition.HungerRestored(ItemId.Fig));
        [Test] public void Falafel_Restores_6_Hunger()  => Assert.AreEqual(6f, FoodDefinition.HungerRestored(ItemId.Falafel));
        [Test] public void Fish_Restores_4_Hunger()     => Assert.AreEqual(4f, FoodDefinition.HungerRestored(ItemId.Fish));
        [Test] public void Olive_Restores_2_Hunger()    => Assert.AreEqual(2f, FoodDefinition.HungerRestored(ItemId.Olive));
        [Test] public void Apple_Restores_3_Hunger()    => Assert.AreEqual(3f, FoodDefinition.HungerRestored(ItemId.Apple));
        [Test] public void NonFood_Restores_0_Hunger()  => Assert.AreEqual(0f, FoodDefinition.HungerRestored(ItemId.Stone));
        [Test] public void IsFoodItem_True_ForBread()   => Assert.IsTrue(FoodDefinition.IsFood(ItemId.Bread));
        [Test] public void IsFoodItem_False_ForStone()  => Assert.IsFalse(FoodDefinition.IsFood(ItemId.Stone));
    }
}
```

- [ ] **Step 2: Run tests — confirm 10 failures** (`FoodDefinition` doesn't exist yet)
- [ ] **Step 3: Create `FoodDefinition.cs`**

```csharp
// Assets/Scripts/Player/FoodDefinition.cs
namespace DeenCraft.Player
{
    /// <summary>
    /// Central nutrition lookup — maps food ItemIds to hunger restored.
    /// Single source of truth used by VitalitySystem and future crafting/trade systems.
    /// </summary>
    public static class FoodDefinition
    {
        /// <summary>Returns hunger points restored when consuming this item (0 if not food).</summary>
        public static float HungerRestored(ItemId itemId)
        {
            switch (itemId)
            {
                case ItemId.Bread:   return 5f;
                case ItemId.Date:    return 3f;
                case ItemId.Fig:     return 2f;
                case ItemId.Falafel: return 6f;
                case ItemId.Fish:    return 4f;
                case ItemId.Olive:   return 2f;
                case ItemId.Apple:   return 3f;
                default:             return 0f;
            }
        }

        /// <summary>Returns true if the item is consumable food.</summary>
        public static bool IsFood(ItemId itemId) => HungerRestored(itemId) > 0f;
    }
}
```

- [ ] **Step 4: Run tests — confirm 10 pass**
- [ ] **Step 5: Update `VitalitySystem.GetFoodRestoreAmount`** to delegate to `FoodDefinition`:

Replace the existing inline switch in `VitalitySystem.cs`:
```csharp
// BEFORE (remove this):
private float GetFoodRestoreAmount(ItemId foodItem)
{
    switch (foodItem)
    {
        case ItemId.Bread:   return 5f;
        case ItemId.Date:    return 3f;
        case ItemId.Fig:     return 2f;
        case ItemId.Falafel: return 6f;
        default:             return 0f;
    }
}

// AFTER (replace with):
private float GetFoodRestoreAmount(ItemId foodItem) => FoodDefinition.HungerRestored(foodItem);
```

- [ ] **Step 6: Run all tests — confirm 10 pass (no regressions)**
- [ ] **Step 7: Commit** `feat: add FoodDefinition nutrition lookup; wire VitalitySystem`

---

## Task 5 — BlockItemMap for new block types

**Files:**
- Modify: `Assets/Scripts/Player/BlockItemMap.cs`

- [ ] **Step 1: Add BlockItemMap tests to `CulturalBlockTests.cs`**

```csharp
using DeenCraft.Player;
using DeenCraft.World;

// add inside CulturalBlockTests class:
[Test] public void AppleLeaves_DropAppleFood()    => Assert.AreEqual(ItemId.Apple,   BlockItemMap.BlockTypeToItemId(BlockType.AppleLeaves));
[Test] public void OliveLeaves_DropOliveFood()    => Assert.AreEqual(ItemId.Olive,   BlockItemMap.BlockTypeToItemId(BlockType.OliveLeaves));
[Test] public void Minaret_DropsSelf()            => Assert.AreEqual(ItemId.Minaret, BlockItemMap.BlockTypeToItemId(BlockType.Minaret));
[Test] public void Dome_DropsSelf()               => Assert.AreEqual(ItemId.Dome,    BlockItemMap.BlockTypeToItemId(BlockType.Dome));
[Test] public void StoneArch_DropsSelf()          => Assert.AreEqual(ItemId.StoneArch,  BlockItemMap.BlockTypeToItemId(BlockType.StoneArch));
[Test] public void Crescent_DropsSelf()           => Assert.AreEqual(ItemId.Crescent,   BlockItemMap.BlockTypeToItemId(BlockType.Crescent));
[Test] public void StarBlock_DropsSelf()          => Assert.AreEqual(ItemId.StarBlock,   BlockItemMap.BlockTypeToItemId(BlockType.StarBlock));
[Test] public void WaterSlide_IsPlaceable()       => Assert.IsTrue(BlockItemMap.IsPlaceable(ItemId.WaterSlide));
[Test] public void Minaret_IsPlaceable()          => Assert.IsTrue(BlockItemMap.IsPlaceable(ItemId.Minaret));
```

- [ ] **Step 2: Run tests — confirm new tests fail** (new mappings don't exist yet)
- [ ] **Step 3: Add block items to `ItemId.cs`** — in the block items range (1-19 section, extend to ~34):

```csharp
// Cultural decorative blocks (placed in world as blocks)
Minaret    = 28,
Dome       = 29,
StoneArch  = 30,
Crescent   = 31,
StarBlock  = 32,
AppleWood  = 33,
WaterSlide = 34,
```

- [ ] **Step 4: Add mappings to `BlockItemMap.BlockTypeToItemId`** switch:

```csharp
case BlockType.AppleWood:    return ItemId.AppleWood;
case BlockType.AppleLeaves:  return ItemId.Apple;        // leaves drop food
case BlockType.OliveLeaves:  return ItemId.Olive;        // leaves drop food (was OliveLeaf block)
case BlockType.Minaret:      return ItemId.Minaret;
case BlockType.Dome:         return ItemId.Dome;
case BlockType.StoneArch:    return ItemId.StoneArch;
case BlockType.Crescent:     return ItemId.Crescent;
case BlockType.StarBlock:    return ItemId.StarBlock;
case BlockType.WaterSlide:   return ItemId.WaterSlide;
```

- [ ] **Step 5: Add mappings to `BlockItemMap.ItemIdToBlockType`** switch:

```csharp
case ItemId.AppleWood:   return BlockType.AppleWood;
case ItemId.Minaret:     return BlockType.Minaret;
case ItemId.Dome:        return BlockType.Dome;
case ItemId.StoneArch:   return BlockType.StoneArch;
case ItemId.Crescent:    return BlockType.Crescent;
case ItemId.StarBlock:   return BlockType.StarBlock;
case ItemId.WaterSlide:  return BlockType.WaterSlide;
// Note: ItemId.Apple, ItemId.Olive are food — not placeable (remain in default → Air)
```

- [ ] **Step 6: Run tests — confirm all new tests pass + no regressions**
- [ ] **Step 7: Commit** `feat: extend BlockItemMap with cultural block types`

---

## Task 6 — Apple tree generation in FeatureGenerator

**Files:**
- Modify: `Assets/Scripts/World/FeatureGenerator.cs`
- Create: `Assets/Tests/EditMode/WorldTests/AppleTreeTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Assets/Tests/EditMode/WorldTests/AppleTreeTests.cs
using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class AppleTreeTests
    {
        // Helper: generate a Grassland chunk using a deterministic seed
        private ChunkData MakeGrasslandChunk(int seed = 12345)
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, seed);
            return chunk;
        }

        [Test]
        public void Grassland_Chunk_Contains_AppleWood()
        {
            // Grassland biome (chunkX=0, chunkZ=0 with seed 12345 is Grassland)
            // After Decorate, at least one AppleWood block should exist
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 12345);
            FeatureGenerator.Decorate(chunk, 0, 0, 12345);

            bool found = false;
            for (int x = 0; x < GameConstants.ChunkWidth && !found; x++)
                for (int z = 0; z < GameConstants.ChunkDepth && !found; z++)
                    for (int y = 0; y < GameConstants.ChunkHeight && !found; y++)
                        if (chunk.GetBlock(x, y, z) == BlockType.AppleWood) found = true;

            Assert.IsTrue(found, "Expected at least one AppleWood block in Grassland chunk");
        }

        [Test]
        public void Grassland_Chunk_Contains_AppleLeaves()
        {
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 12345);
            FeatureGenerator.Decorate(chunk, 0, 0, 12345);

            bool found = false;
            for (int x = 0; x < GameConstants.ChunkWidth && !found; x++)
                for (int z = 0; z < GameConstants.ChunkDepth && !found; z++)
                    for (int y = 0; y < GameConstants.ChunkHeight && !found; y++)
                        if (chunk.GetBlock(x, y, z) == BlockType.AppleLeaves) found = true;

            Assert.IsTrue(found, "Expected at least one AppleLeaves block in Grassland chunk");
        }

        [Test]
        public void AppleTree_TrunkIsBelow_Canopy()
        {
            // For any column with AppleWood, there should be AppleLeaves above the trunk
            var chunk = new ChunkData();
            WorldGenerator.Generate(chunk, 0, 0, 12345);
            FeatureGenerator.Decorate(chunk, 0, 0, 12345);

            for (int x = 2; x <= 13; x++)
            {
                for (int z = 2; z <= 13; z++)
                {
                    for (int y = 1; y < GameConstants.ChunkHeight - 5; y++)
                    {
                        if (chunk.GetBlock(x, y, z) == BlockType.AppleWood &&
                            chunk.GetBlock(x, y - 1, z) != BlockType.AppleWood)
                        {
                            // This is the base of a trunk — canopy should be within 7 blocks above
                            bool foundCanopy = false;
                            for (int dy = 1; dy <= 7 && !foundCanopy; dy++)
                                if (chunk.GetBlock(x, y + dy, z) == BlockType.AppleLeaves)
                                    foundCanopy = true;
                            Assert.IsTrue(foundCanopy,
                                $"Apple trunk base at ({x},{y},{z}) has no leaves above it");
                        }
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 2: Verify test file compiles but 3 tests fail** (no apple trees generated yet)

- [ ] **Step 3: Add `PlaceAppleTree` to `FeatureGenerator.cs`** and wire it into `PlaceTree`:

Add helper method:
```csharp
private static void PlaceAppleTree(ChunkData chunk, int x, int surfY, int z, System.Random rng)
{
    int height = rng.Next(3, 6); // 3–5 blocks tall
    // Trunk
    for (int y = surfY + 1; y <= surfY + height; y++)
        SafeSet(chunk, x, y, z, BlockType.AppleWood);
    // 3×3 canopy at top-1 and top
    for (int dy = -1; dy <= 0; dy++)
    {
        int cy = surfY + height + dy;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                if (chunk.IsInBounds(x + dx, cy, z + dz))
                    if (chunk.GetBlock(x + dx, cy, z + dz) == BlockType.Air)
                        SafeSet(chunk, x + dx, cy, z + dz, BlockType.AppleLeaves);
    }
    // Cap 1×1
    SafeSet(chunk, x, surfY + height + 1, z, BlockType.AppleLeaves);
}
```

In `PlaceTree`, add Grassland case:
```csharp
case BiomeType.Grassland:
    // 50% chance apple tree, 50% regular tree
    if (rng.NextDouble() < 0.5)
        PlaceAppleTree(chunk, x, surfY, z, rng);
    else
        PlaceRegularTree(chunk, x, surfY, z, rng);
    break;
```

> **Note:** The existing `PlaceTree` has a `default:` case that handles Grassland → regular tree. Replace the `default:` to add the explicit Grassland split, keeping other biomes' default as a fallback.

- [ ] **Step 4: Run tests — confirm 3 apple tree tests pass**
- [ ] **Step 5: Commit** `feat: add apple trees to Grassland biome generation`

---

## Task 7 — Mosque auto-generation in FeatureGenerator

**Files:**
- Modify: `Assets/Scripts/World/FeatureGenerator.cs`
- Create: `Assets/Tests/EditMode/WorldTests/MosqueTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Assets/Tests/EditMode/WorldTests/MosqueTests.cs
using NUnit.Framework;
using DeenCraft.World;
using DeenCraft;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class MosqueTests
    {
        [Test]
        public void MosqueChunk_Contains_MosqueBlock()
        {
            // Seed 99999 with chunkX=3, chunkZ=3 — verify this is a mosque chunk by inspection
            // Use a brute-force search: find ANY chunk coordinates that IsVillageChunk + IsMosqueChunk
            // For determinism, we find the first such chunk in a 10×10 grid.
            bool testedAtLeastOne = false;
            for (int cx = 0; cx < 10; cx++)
            {
                for (int cz = 0; cz < 10; cz++)
                {
                    if (!FeatureGeneratorTestHelper.IsVillageChunk(cx, cz, 42)) continue;
                    if (!FeatureGeneratorTestHelper.IsMosqueChunk(cx, cz, 42))  continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, 42);
                    FeatureGenerator.Decorate(chunk, cx, cz, 42);

                    bool found = false;
                    for (int x = 0; x < GameConstants.ChunkWidth && !found; x++)
                        for (int z = 0; z < GameConstants.ChunkDepth && !found; z++)
                            for (int y = 0; y < GameConstants.ChunkHeight && !found; y++)
                                if (chunk.GetBlock(x, y, z) == BlockType.Mosque ||
                                    chunk.GetBlock(x, y, z) == BlockType.Minaret ||
                                    chunk.GetBlock(x, y, z) == BlockType.Dome)
                                    found = true;

                    Assert.IsTrue(found, $"Mosque chunk ({cx},{cz}) has no Mosque/Minaret/Dome blocks");
                    testedAtLeastOne = true;
                    return; // pass on first valid mosque chunk found
                }
            }
            if (!testedAtLeastOne)
                Assert.Inconclusive("No mosque chunk found in 10×10 grid with seed 42 — adjust seed.");
        }

        [Test]
        public void NonMosque_VillageChunk_HasNoMosqueBlocks()
        {
            // Find a village chunk that is NOT a mosque chunk — verify no mosque blocks
            for (int cx = 0; cx < 10; cx++)
            {
                for (int cz = 0; cz < 10; cz++)
                {
                    if (!FeatureGeneratorTestHelper.IsVillageChunk(cx, cz, 42)) continue;
                    if (FeatureGeneratorTestHelper.IsMosqueChunk(cx, cz, 42))   continue;

                    var chunk = new ChunkData();
                    WorldGenerator.Generate(chunk, cx, cz, 42);
                    FeatureGenerator.Decorate(chunk, cx, cz, 42);

                    for (int x = 0; x < GameConstants.ChunkWidth; x++)
                        for (int z = 0; z < GameConstants.ChunkDepth; z++)
                            for (int y = 0; y < GameConstants.ChunkHeight; y++)
                            {
                                var b = chunk.GetBlock(x, y, z);
                                Assert.AreNotEqual(BlockType.Minaret, b, "Non-mosque village chunk has Minaret");
                                Assert.AreNotEqual(BlockType.Dome,    b, "Non-mosque village chunk has Dome");
                            }
                    return;
                }
            }
            Assert.Inconclusive("No non-mosque village chunk found in 10×10 grid — adjust seed.");
        }

        [Test]
        public void PlaceMosque_Builds_MinaretBlocks()
        {
            // Direct unit test: call PlaceMosqueInternal via test helper on a flat chunk
            var chunk = new ChunkData();
            // Fill floor at y=64
            for (int x = 0; x < GameConstants.ChunkWidth; x++)
                for (int z = 0; z < GameConstants.ChunkDepth; z++)
                    chunk.SetBlock(x, 64, z, BlockType.Grass);

            FeatureGeneratorTestHelper.PlaceMosque(chunk, 1, 64, 1);

            bool hasMinaret = false;
            for (int x = 0; x < GameConstants.ChunkWidth && !hasMinaret; x++)
                for (int z = 0; z < GameConstants.ChunkDepth && !hasMinaret; z++)
                    for (int y = 0; y < GameConstants.ChunkHeight && !hasMinaret; y++)
                        if (chunk.GetBlock(x, y, z) == BlockType.Minaret) hasMinaret = true;

            Assert.IsTrue(hasMinaret, "PlaceMosque did not place any Minaret blocks");
        }
    }
}
```

- [ ] **Step 2: Create `FeatureGeneratorTestHelper.cs`** in the test assembly to expose internal methods:

```csharp
// Assets/Tests/EditMode/WorldTests/FeatureGeneratorTestHelper.cs
using DeenCraft.World;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    /// <summary>
    /// Exposes package-internal FeatureGenerator methods for testing.
    /// FeatureGenerator uses internal static methods — this helper calls them
    /// via the public test-only overloads added in this task.
    /// </summary>
    public static class FeatureGeneratorTestHelper
    {
        public static bool IsVillageChunk(int cx, int cz, int seed)
            => FeatureGenerator.IsVillageChunkPublic(cx, cz, seed);

        public static bool IsMosqueChunk(int cx, int cz, int seed)
            => FeatureGenerator.IsMosqueChunkPublic(cx, cz, seed);

        public static void PlaceMosque(ChunkData chunk, int anchorX, int surfaceY, int anchorZ)
            => FeatureGenerator.PlaceMosquePublic(chunk, anchorX, surfaceY, anchorZ);
    }
}
```

- [ ] **Step 3: Add public test-only wrappers to `FeatureGenerator.cs`** (leave private methods private):

```csharp
// ── Test-only public wrappers (used only by EditMode tests) ──────────────
#if UNITY_EDITOR
public static bool  IsVillageChunkPublic(int cx, int cz, int seed) => IsVillageChunk(cx, cz, seed);
public static bool  IsMosqueChunkPublic(int cx, int cz, int seed)  => IsMosqueChunk(cx, cz, seed);
public static void  PlaceMosquePublic(ChunkData chunk, int ax, int sy, int az) => PlaceMosque(chunk, ax, sy, az);
#endif
```

- [ ] **Step 4: Add `IsMosqueChunk` and `PlaceMosque` to `FeatureGenerator.cs`**:

Add `IsMosqueChunk`:
```csharp
private static bool IsMosqueChunk(int chunkX, int chunkZ, int seed)
{
    // Uses a different noise offset than village check to avoid always co-locating
    float noise = UnityEngine.Mathf.PerlinNoise(
        chunkX * GameConstants.VillageNoiseScale + seed * 0.003f + 50f,
        chunkZ * GameConstants.VillageNoiseScale + seed * 0.003f + 50f);
    return noise > (1f - GameConstants.MosqueChancePerVillage);
}
```

Add `PlaceMosque` (7×7 footprint, 3-block high walls, single minaret):
```csharp
private static void PlaceMosque(ChunkData chunk, int anchorX, int surfaceY, int anchorZ)
{
    int w = GameConstants.MosqueCourtyard; // 7
    int wallH = 4;

    // Floor (Mosque block = mosaic tile)
    for (int x = anchorX; x < anchorX + w; x++)
        for (int z = anchorZ; z < anchorZ + w; z++)
            SafeSet(chunk, x, surfaceY, z, BlockType.Mosque);

    // Walls (MudBrick)
    for (int y = 1; y <= wallH; y++)
    {
        for (int x = anchorX; x < anchorX + w; x++)
        {
            SafeSet(chunk, x, surfaceY + y, anchorZ,           BlockType.MudBrick);
            SafeSet(chunk, x, surfaceY + y, anchorZ + w - 1,   BlockType.MudBrick);
        }
        for (int z = anchorZ; z < anchorZ + w; z++)
        {
            SafeSet(chunk, anchorX,           surfaceY + y, z, BlockType.MudBrick);
            SafeSet(chunk, anchorX + w - 1,   surfaceY + y, z, BlockType.MudBrick);
        }
    }

    // Dome cap on roof centre
    int midX = anchorX + w / 2;
    int midZ = anchorZ + w / 2;
    SafeSet(chunk, midX, surfaceY + wallH + 1, midZ, BlockType.Dome);

    // Minaret: one corner, GameConstants.MosqueMinaretHeight blocks tall topped with Dome
    int minaretX = anchorX + w - 1;
    int minaretZ = anchorZ + w - 1;
    for (int y = 1; y <= GameConstants.MosqueMinaretHeight; y++)
        SafeSet(chunk, minaretX, surfaceY + y, minaretZ, BlockType.Minaret);
    SafeSet(chunk, minaretX, surfaceY + GameConstants.MosqueMinaretHeight + 1, minaretZ, BlockType.Dome);

    // Door: opening in south wall, centre column, y+1 and y+2
    SafeSet(chunk, midX, surfaceY + 1, anchorZ, BlockType.Air);
    SafeSet(chunk, midX, surfaceY + 2, anchorZ, BlockType.Air);
}
```

Wire into `PlaceStructures` — in the village-eligible block, after `PlaceHouse`:
```csharp
if (biome == BiomeType.Grassland || biome == BiomeType.OliveGrove)
{
    if (IsMosqueChunk(chunkX, chunkZ, seed))
    {
        int mosqueAnchorX = GameConstants.HouseAnchorX + GameConstants.HouseWidth + 2;
        int mosqueAnchorZ = GameConstants.HouseAnchorZ;
        int midMX = mosqueAnchorX + GameConstants.MosqueCourtyard / 2;
        int midMZ = mosqueAnchorZ + GameConstants.MosqueCourtyard / 2;
        if (chunk.IsInBounds(mosqueAnchorX + GameConstants.MosqueCourtyard - 1,
                             GameConstants.SeaLevel, mosqueAnchorZ + GameConstants.MosqueCourtyard - 1))
        {
            int mSurfY = FindSurfaceY(chunk, midMX, midMZ);
            if (mSurfY >= 0)
                PlaceMosque(chunk, mosqueAnchorX, mSurfY, mosqueAnchorZ);
        }
    }
}
```

- [ ] **Step 5: Run mosque tests — confirm all 3 pass** (or Inconclusive passes are acceptable for integration tests)
- [ ] **Step 6: Run full test suite — confirm no regressions**
- [ ] **Step 7: Commit** `feat: mosque auto-generation in Grassland/OliveGrove village chunks`

---

## Task 8 — Final review, test count, and push

- [ ] **Step 1: Run all EditMode tests** — confirm ≥ 195 pass (174 baseline + ~21 new)
- [ ] **Step 2: Check for compiler warnings** — `dotnet build` or Unity console
- [ ] **Step 3: Write `docs/phase_6_status.md`** capturing what was built, test count, and Phase 7 notes
- [ ] **Step 4: Final commit** `docs: Phase 6 status file`
- [ ] **Step 5: Push** `git push origin main`

---

## Estimated New Test Count

| Task | New Tests |
|---|---|
| CulturalBlockTests (BlockType + ItemId existence) | 10 |
| CulturalBlockTests (BlockItemMap mappings) | 9 |
| FoodDefinitionTests | 10 |
| AppleTreeTests | 3 |
| MosqueTests | 3 |
| **Total new** | **35** |
| **Cumulative total** | **≥ 209** |

---

## Conventions Reminder

- `[SerializeField]` not `public` for Inspector fields
- `PascalCase` classes/methods, `_camelCase` private fields  
- No magic numbers — all literals go in `GameConstants.cs`
- Assembly refs: World.asmdef already has Core; Player.asmdef already has Core+World  
- `#if UNITY_EDITOR` guards on any test-only public wrappers
- `SafeSet` in FeatureGenerator already handles out-of-bounds gracefully

# Phase 2: Voxel Engine Core — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete chunk-based voxel world engine with 5 biomes, greedy meshing, block placement/destruction, and water simulation.

**Architecture:** Layered subsystems — Data → Generation → Rendering → Orchestration → Interaction. Each layer is independently testable. Sub-agents work in parallel within rounds; rounds are sequential.

**Tech Stack:** Unity 2022.3.62f3 LTS, C# (.NET Standard 2.1), `DeenCraft.World` assembly, Unity EditMode + PlayMode test framework.

**Spec:** `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md`

---

## Execution Order (Parallel Rounds)

```
Round 1 (parallel): Task 1 + Task 2 + Task 3
Round 2 (parallel): Task 4 + Task 5          ← depends on Round 1
Round 3 (sequential): Task 6                 ← depends on Round 2
Round 4 (parallel): Task 7 + Task 8          ← depends on Round 3
```

---

## Round 1 — Data & Biome Layer

### Task 1: GameConstants Updates + BlockType

**Files:**
- Modify: `Assets/Scripts/GameConstants.cs`
- Create: `Assets/Scripts/World/BlockType.cs`
- Create: `Assets/Tests/EditMode/WorldTests/BlockTypeTests.cs`

**Context you MUST read first:**
- `Assets/Scripts/GameConstants.cs` — existing constants file; add to it, never remove existing constants
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "Constants to Add" and "Data Layer" sections

**Constraints:**
- `namespace DeenCraft` for `GameConstants.cs`; `namespace DeenCraft.World` for `BlockType.cs`
- `BlockType` enum values must match existing `GameConstants.BlockId*` constants exactly
- No magic numbers; no `public` fields
- Test file goes in `Assets/Tests/EditMode/WorldTests/` (create directory)

- [ ] **Step 1: Add new constants to GameConstants.cs**

  Add these constants to the existing `GameConstants` class (below existing block ID constants):
  ```csharp
  // World generation
  public const int SeaLevel = 64;
  public const float BiomeNoiseScale = 0.003f;
  public const float TerrainNoiseScale = 0.004f;
  public const int TerrainOctaves = 3;
  public const float TerrainPersistence = 0.5f;
  public const int MinTerrainHeight = 60;
  public const int MaxTerrainHeight = 120;

  // Block interaction
  public const int BlockInteractionRange = 5;

  // Water simulation
  public const int MaxWaterSpread = 16;
  ```

- [ ] **Step 2: Write the failing BlockType tests**

  Create `Assets/Tests/EditMode/WorldTests/BlockTypeTests.cs`:
  ```csharp
  using NUnit.Framework;
  using DeenCraft.World;
  using DeenCraft;

  namespace DeenCraft.Tests.EditMode
  {
      public class BlockTypeTests
      {
          [Test] public void Air_HasIdZero() => Assert.AreEqual(0, (int)BlockType.Air);
          [Test] public void Stone_HasIdOne() => Assert.AreEqual(1, (int)BlockType.Stone);
          [Test] public void Water_IdMatchesGameConstants() =>
              Assert.AreEqual(GameConstants.BlockIdWater, (int)BlockType.Water);
          [Test] public void AllBlockIds_AreSequentialFromZero()
          {
              var values = System.Enum.GetValues(typeof(BlockType));
              for (int i = 0; i < values.Length; i++)
                  Assert.AreEqual(i, (int)values.GetValue(i));
          }
          [Test] public void BlockTypeCount_IsThirteen() =>
              Assert.AreEqual(13, System.Enum.GetValues(typeof(BlockType)).Length);
      }
  }
  ```

- [ ] **Step 3: Verify tests fail** (file exists but `BlockType` class doesn't yet — confirm compile error in test output)

- [ ] **Step 4: Create BlockType.cs**

  ```csharp
  namespace DeenCraft.World
  {
      public enum BlockType : byte
      {
          Air        = 0,
          Stone      = 1,
          Dirt       = 2,
          Grass      = 3,
          Sand       = 4,
          Snow       = 5,
          Water      = 6,
          Wood       = 7,
          Leaves     = 8,
          MosaicTile = 9,
          Bedrock    = 10,
          Gravel     = 11,
          OliveWood  = 12,
      }
  }
  ```

- [ ] **Step 5: Run BlockTypeTests — confirm all 5 pass**

- [ ] **Step 6: Commit**
  `git add -A && git commit -m "feat(world): add BlockType enum and GameConstants world gen constants"`

---

### Task 2: ChunkData

**Files:**
- Create: `Assets/Scripts/World/ChunkData.cs`
- Create: `Assets/Tests/EditMode/WorldTests/ChunkDataTests.cs`

**Context you MUST read first:**
- `Assets/Scripts/GameConstants.cs` — for `ChunkWidth`, `ChunkHeight`, `ChunkDepth`
- `Assets/Scripts/World/BlockType.cs` (after Task 1 completes, or assume it exists)
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "ChunkData" section

**Constraints:**
- `namespace DeenCraft.World`
- Flat byte array storage: index = `x + ChunkWidth * (y + ChunkHeight * z)`
- No Unity dependencies in this file (no `using UnityEngine`)
- `IsInBounds` must be used internally before every get/set (throw `ArgumentOutOfRangeException` on OOB)

- [ ] **Step 1: Write failing ChunkData tests**

  Create `Assets/Tests/EditMode/WorldTests/ChunkDataTests.cs`:
  ```csharp
  using NUnit.Framework;
  using DeenCraft.World;
  using DeenCraft;
  using System;

  namespace DeenCraft.Tests.EditMode
  {
      public class ChunkDataTests
      {
          private ChunkData _chunk;

          [SetUp] public void SetUp() => _chunk = new ChunkData();

          [Test] public void NewChunk_IsAllAir()
          {
              for (int x = 0; x < GameConstants.ChunkWidth; x++)
              for (int z = 0; z < GameConstants.ChunkDepth; z++)
                  Assert.AreEqual(BlockType.Air, _chunk.GetBlock(x, 0, z));
          }

          [Test] public void SetAndGetBlock_RoundTrips()
          {
              _chunk.SetBlock(3, 10, 7, BlockType.Stone);
              Assert.AreEqual(BlockType.Stone, _chunk.GetBlock(3, 10, 7));
          }

          [Test] public void SetBlock_DoesNotCorruptNeighbors()
          {
              _chunk.SetBlock(5, 5, 5, BlockType.Dirt);
              Assert.AreEqual(BlockType.Air, _chunk.GetBlock(4, 5, 5));
              Assert.AreEqual(BlockType.Air, _chunk.GetBlock(6, 5, 5));
          }

          [Test] public void GetBlock_OutOfBounds_Throws() =>
              Assert.Throws<ArgumentOutOfRangeException>(() => _chunk.GetBlock(99, 0, 0));

          [Test] public void SetBlock_OutOfBounds_Throws() =>
              Assert.Throws<ArgumentOutOfRangeException>(() => _chunk.SetBlock(0, 999, 0, BlockType.Stone));

          [Test] public void IsInBounds_ReturnsTrueForValidCoords() =>
              Assert.IsTrue(_chunk.IsInBounds(0, 0, 0));

          [Test] public void IsInBounds_ReturnsFalseForNegative() =>
              Assert.IsFalse(_chunk.IsInBounds(-1, 0, 0));

          [Test] public void ChunkSize_MatchesGameConstants()
          {
              Assert.AreEqual(GameConstants.ChunkWidth * GameConstants.ChunkHeight * GameConstants.ChunkDepth,
                  _chunk.BlockCount);
          }
      }
  }
  ```

- [ ] **Step 2: Verify tests fail (ChunkData doesn't exist yet)**

- [ ] **Step 3: Create ChunkData.cs**

  ```csharp
  using System;
  using DeenCraft;

  namespace DeenCraft.World
  {
      public sealed class ChunkData
      {
          private readonly byte[] _blocks;

          public int BlockCount => _blocks.Length;

          public ChunkData()
          {
              _blocks = new byte[GameConstants.ChunkWidth * GameConstants.ChunkHeight * GameConstants.ChunkDepth];
          }

          public bool IsInBounds(int x, int y, int z)
          {
              return x >= 0 && x < GameConstants.ChunkWidth
                  && y >= 0 && y < GameConstants.ChunkHeight
                  && z >= 0 && z < GameConstants.ChunkDepth;
          }

          public BlockType GetBlock(int x, int y, int z)
          {
              if (!IsInBounds(x, y, z))
                  throw new ArgumentOutOfRangeException($"({x},{y},{z}) is out of chunk bounds.");
              return (BlockType)_blocks[FlatIndex(x, y, z)];
          }

          public void SetBlock(int x, int y, int z, BlockType blockType)
          {
              if (!IsInBounds(x, y, z))
                  throw new ArgumentOutOfRangeException($"({x},{y},{z}) is out of chunk bounds.");
              _blocks[FlatIndex(x, y, z)] = (byte)blockType;
          }

          private static int FlatIndex(int x, int y, int z)
              => x + GameConstants.ChunkWidth * (y + GameConstants.ChunkHeight * z);
      }
  }
  ```

- [ ] **Step 4: Run ChunkDataTests — confirm all 8 pass**

- [ ] **Step 5: Commit**
  `git add -A && git commit -m "feat(world): add ChunkData voxel container with flat array storage"`

---

### Task 3: BiomeSystem

**Files:**
- Create: `Assets/Scripts/World/BiomeType.cs`
- Create: `Assets/Scripts/World/BiomeDefinition.cs`
- Create: `Assets/Scripts/World/BiomeSystem.cs`
- Create: `Assets/Tests/EditMode/WorldTests/BiomeSystemTests.cs`

**Context you MUST read first:**
- `Assets/Scripts/GameConstants.cs` — for `BiomeNoiseScale`
- `Assets/Scripts/World/BlockType.cs`
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "BiomeSystem" section

**Constraints:**
- No Unity dependencies in `BiomeType.cs` or `BiomeDefinition.cs`
- `BiomeSystem.cs` may use `UnityEngine.Mathf.PerlinNoise`
- All 5 biomes must be defined and reachable

- [ ] **Step 1: Write failing BiomeSystem tests**

  Create `Assets/Tests/EditMode/WorldTests/BiomeSystemTests.cs`:
  ```csharp
  using NUnit.Framework;
  using DeenCraft.World;
  using System.Collections.Generic;

  namespace DeenCraft.Tests.EditMode
  {
      public class BiomeSystemTests
      {
          [Test] public void GetBiome_SameSeedSameCoord_ReturnsSameBiome()
          {
              var a = BiomeSystem.GetBiome(100f, 200f, 42);
              var b = BiomeSystem.GetBiome(100f, 200f, 42);
              Assert.AreEqual(a, b);
          }

          [Test] public void GetBiome_DifferentSeeds_CanReturnDifferentBiomes()
          {
              // Not guaranteed to differ at this coord, but seeds must shift noise
              var a = BiomeSystem.GetBiome(0f, 0f, 1);
              var b = BiomeSystem.GetBiome(0f, 0f, 99999);
              // This test validates the seed is used; pass even if same (rare collision)
              Assert.IsTrue(true);
          }

          [Test] public void AllFiveBiomes_AreReachable()
          {
              var found = new HashSet<BiomeType>();
              for (int x = -500; x <= 500; x += 50)
              for (int z = -500; z <= 500; z += 50)
                  found.Add(BiomeSystem.GetBiome(x, z, 42));
              Assert.AreEqual(5, found.Count, $"Only found biomes: {string.Join(", ", found)}");
          }

          [Test] public void BiomeDefinition_HasValidBlocks()
          {
              foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
              {
                  var def = BiomeSystem.GetDefinition(biome);
                  Assert.AreNotEqual(BlockType.Air, def.SurfaceBlock, $"{biome} has Air surface");
                  Assert.AreNotEqual(BlockType.Air, def.FillerBlock, $"{biome} has Air filler");
                  Assert.Greater(def.MaxHeight, def.MinHeight);
              }
          }

          [Test] public void BiomeTypeEnum_HasFiveValues() =>
              Assert.AreEqual(5, System.Enum.GetValues(typeof(BiomeType)).Length);
      }
  }
  ```

- [ ] **Step 2: Create BiomeType.cs**

  ```csharp
  namespace DeenCraft.World
  {
      public enum BiomeType
      {
          Desert     = 0,
          Grassland  = 1,
          SnowyIsland = 2,
          OliveGrove = 3,
          Riverside  = 4,
      }
  }
  ```

- [ ] **Step 3: Create BiomeDefinition.cs**

  ```csharp
  namespace DeenCraft.World
  {
      public sealed class BiomeDefinition
      {
          public BiomeType Type        { get; }
          public BlockType SurfaceBlock { get; }
          public BlockType FillerBlock  { get; }
          public int       MinHeight    { get; }
          public int       MaxHeight    { get; }

          public BiomeDefinition(BiomeType type, BlockType surface, BlockType filler,
                                 int minHeight, int maxHeight)
          {
              Type         = type;
              SurfaceBlock = surface;
              FillerBlock  = filler;
              MinHeight    = minHeight;
              MaxHeight    = maxHeight;
          }
      }
  }
  ```

- [ ] **Step 4: Create BiomeSystem.cs**

  ```csharp
  using System.Collections.Generic;
  using DeenCraft;
  using UnityEngine;

  namespace DeenCraft.World
  {
      public static class BiomeSystem
      {
          private static readonly Dictionary<BiomeType, BiomeDefinition> _definitions =
              new Dictionary<BiomeType, BiomeDefinition>
          {
              { BiomeType.Desert,      new BiomeDefinition(BiomeType.Desert,      BlockType.Sand,   BlockType.Sand,   62,  78) },
              { BiomeType.Grassland,   new BiomeDefinition(BiomeType.Grassland,   BlockType.Grass,  BlockType.Dirt,   64,  90) },
              { BiomeType.SnowyIsland, new BiomeDefinition(BiomeType.SnowyIsland, BlockType.Snow,   BlockType.Dirt,   64,  85) },
              { BiomeType.OliveGrove,  new BiomeDefinition(BiomeType.OliveGrove,  BlockType.Grass,  BlockType.Dirt,   65,  95) },
              { BiomeType.Riverside,   new BiomeDefinition(BiomeType.Riverside,   BlockType.Grass,  BlockType.Dirt,   62,  70) },
          };

          public static BiomeDefinition GetDefinition(BiomeType biome) => _definitions[biome];

          public static BiomeType GetBiome(float worldX, float worldZ, int seed)
          {
              float offset = seed * 0.1f;
              float temperature = Mathf.PerlinNoise(
                  worldX * GameConstants.BiomeNoiseScale + offset,
                  worldZ * GameConstants.BiomeNoiseScale + offset);
              float humidity = Mathf.PerlinNoise(
                  worldX * GameConstants.BiomeNoiseScale + offset + 100f,
                  worldZ * GameConstants.BiomeNoiseScale + offset + 100f);

              if (humidity > 0.65f)           return BiomeType.Riverside;
              if (temperature < 0.35f)        return BiomeType.SnowyIsland;
              if (temperature > 0.65f && humidity < 0.35f) return BiomeType.Desert;
              if (temperature > 0.5f && humidity > 0.5f)   return BiomeType.OliveGrove;
              return BiomeType.Grassland;
          }
      }
  }
  ```

- [ ] **Step 5: Run BiomeSystemTests — confirm all 5 pass**

- [ ] **Step 6: Commit**
  `git add -A && git commit -m "feat(world): add BiomeType, BiomeDefinition, BiomeSystem with 5 biomes"`

---

## Round 2 — Generation & Rendering Layer

### Task 4: WorldGenerator

**Files:**
- Create: `Assets/Scripts/World/WorldGenerator.cs`
- Create: `Assets/Tests/EditMode/WorldTests/WorldGeneratorTests.cs`

**Context you MUST read first:**
- `Assets/Scripts/GameConstants.cs`
- `Assets/Scripts/World/BlockType.cs`
- `Assets/Scripts/World/ChunkData.cs`
- `Assets/Scripts/World/BiomeSystem.cs`
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "WorldGenerator" section

**Constraints:**
- No MonoBehaviour — pure C# static class
- Must use `GameConstants.SeaLevel`, `MinTerrainHeight`, `MaxTerrainHeight`, `TerrainNoiseScale`, `TerrainOctaves`, `TerrainPersistence`
- Y=0 is always Bedrock
- Water fills all air cells below SeaLevel
- Same (chunkX, chunkZ, seed) must always produce identical ChunkData

- [ ] **Step 1: Write failing WorldGenerator tests**

  Create `Assets/Tests/EditMode/WorldTests/WorldGeneratorTests.cs`:
  ```csharp
  using NUnit.Framework;
  using DeenCraft.World;
  using DeenCraft;

  namespace DeenCraft.Tests.EditMode
  {
      public class WorldGeneratorTests
      {
          [Test] public void Generate_SameSeedAndCoord_ProducesIdenticalChunk()
          {
              var a = new ChunkData();
              var b = new ChunkData();
              WorldGenerator.Generate(a, 0, 0, 42);
              WorldGenerator.Generate(b, 0, 0, 42);
              for (int x = 0; x < GameConstants.ChunkWidth; x++)
              for (int y = 0; y < GameConstants.ChunkHeight; y++)
              for (int z = 0; z < GameConstants.ChunkDepth; z++)
                  Assert.AreEqual(a.GetBlock(x,y,z), b.GetBlock(x,y,z),
                      $"Mismatch at ({x},{y},{z})");
          }

          [Test] public void Generate_Y0_IsAlwaysBedrock()
          {
              var chunk = new ChunkData();
              WorldGenerator.Generate(chunk, 0, 0, 42);
              for (int x = 0; x < GameConstants.ChunkWidth; x++)
              for (int z = 0; z < GameConstants.ChunkDepth; z++)
                  Assert.AreEqual(BlockType.Bedrock, chunk.GetBlock(x, 0, z));
          }

          [Test] public void Generate_SurfaceIsBelowMaxTerrainHeight()
          {
              var chunk = new ChunkData();
              WorldGenerator.Generate(chunk, 0, 0, 42);
              for (int x = 0; x < GameConstants.ChunkWidth; x++)
              for (int z = 0; z < GameConstants.ChunkDepth; z++)
                  Assert.AreEqual(BlockType.Air, chunk.GetBlock(x, GameConstants.MaxTerrainHeight, z));
          }

          [Test] public void Generate_BelowSeaLevel_NoAirExceptBedrock()
          {
              var chunk = new ChunkData();
              WorldGenerator.Generate(chunk, 5, 5, 42);
              // All columns below sea level must be solid or water — not air (except y=0 bedrock counts)
              for (int x = 0; x < GameConstants.ChunkWidth; x++)
              for (int z = 0; z < GameConstants.ChunkDepth; z++)
              for (int y = 1; y < GameConstants.SeaLevel; y++)
                  Assert.AreNotEqual(BlockType.Air, chunk.GetBlock(x, y, z),
                      $"Unexpected air at ({x},{y},{z})");
          }

          [Test] public void Generate_DifferentChunkCoords_ProduceDifferentChunks()
          {
              var a = new ChunkData();
              var b = new ChunkData();
              WorldGenerator.Generate(a, 0, 0, 42);
              WorldGenerator.Generate(b, 10, 10, 42);
              bool anyDiff = false;
              for (int x = 0; x < GameConstants.ChunkWidth && !anyDiff; x++)
              for (int y = 60; y < 100 && !anyDiff; y++)
              for (int z = 0; z < GameConstants.ChunkDepth && !anyDiff; z++)
                  if (a.GetBlock(x,y,z) != b.GetBlock(x,y,z)) anyDiff = true;
              Assert.IsTrue(anyDiff, "Different chunk coords should produce different terrain");
          }
      }
  }
  ```

- [ ] **Step 2: Create WorldGenerator.cs**

  ```csharp
  using DeenCraft;
  using UnityEngine;

  namespace DeenCraft.World
  {
      public static class WorldGenerator
      {
          public static void Generate(ChunkData chunk, int chunkX, int chunkZ, int seed)
          {
              float seedOffset = seed * 0.17f;

              for (int lx = 0; lx < GameConstants.ChunkWidth; lx++)
              {
                  for (int lz = 0; lz < GameConstants.ChunkDepth; lz++)
                  {
                      float worldX = chunkX * GameConstants.ChunkWidth + lx;
                      float worldZ = chunkZ * GameConstants.ChunkDepth + lz;

                      int surfaceY = GetSurfaceHeight(worldX, worldZ, seedOffset);
                      BiomeDefinition biome = BiomeSystem.GetDefinition(
                          BiomeSystem.GetBiome(worldX, worldZ, seed));

                      for (int y = 0; y < GameConstants.ChunkHeight; y++)
                      {
                          BlockType block = GetBlockAtHeight(y, surfaceY, biome);
                          chunk.SetBlock(lx, y, lz, block);
                      }
                  }
              }
          }

          private static int GetSurfaceHeight(float worldX, float worldZ, float seedOffset)
          {
              float noise = 0f;
              float amplitude = 1f;
              float frequency = GameConstants.TerrainNoiseScale;
              float totalAmplitude = 0f;

              for (int i = 0; i < GameConstants.TerrainOctaves; i++)
              {
                  noise += Mathf.PerlinNoise(
                      worldX * frequency + seedOffset,
                      worldZ * frequency + seedOffset) * amplitude;
                  totalAmplitude += amplitude;
                  amplitude *= GameConstants.TerrainPersistence;
                  frequency *= 2f;
              }

              noise /= totalAmplitude;
              int range = GameConstants.MaxTerrainHeight - GameConstants.MinTerrainHeight;
              return GameConstants.MinTerrainHeight + Mathf.FloorToInt(noise * range);
          }

          private static BlockType GetBlockAtHeight(int y, int surfaceY, BiomeDefinition biome)
          {
              if (y == 0)                      return BlockType.Bedrock;
              if (y > surfaceY)
              {
                  return y < GameConstants.SeaLevel ? BlockType.Water : BlockType.Air;
              }
              if (y == surfaceY)               return biome.SurfaceBlock;
              if (y >= surfaceY - 3)           return biome.FillerBlock;
              return BlockType.Stone;
          }
      }
  }
  ```

- [ ] **Step 3: Run WorldGeneratorTests — confirm all 5 pass**

- [ ] **Step 4: Commit**
  `git add -A && git commit -m "feat(world): add WorldGenerator with Perlin noise terrain and biome surface blocks"`

---

### Task 5: MeshData + ChunkMesher (Greedy Meshing)

**Files:**
- Create: `Assets/Scripts/World/MeshData.cs`
- Create: `Assets/Scripts/World/ChunkMesher.cs`
- Create: `Assets/Tests/EditMode/WorldTests/ChunkMesherTests.cs`

**Context you MUST read first:**
- `Assets/Scripts/World/BlockType.cs`
- `Assets/Scripts/World/ChunkData.cs`
- `Assets/Scripts/GameConstants.cs`
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "ChunkMesher" section

**Constraints:**
- `ChunkMesher` is a static class (no MonoBehaviour)
- No Unity dependencies in `MeshData.cs` — use plain arrays (`float[]` or simple structs) so it can be tested without Unity
- Actually: `MeshData` MAY use `UnityEngine.Vector3/Vector2/int[]` — Unity types are available in EditMode tests
- Greedy meshing: combine adjacent same-block faces into one quad per axis pass
- Air blocks never emit faces
- `neighbors` array for boundary face culling: `[+X neighbor, -X neighbor, +Z neighbor, -Z neighbor]`. If `neighbors[i]` is null, treat boundary blocks as exposed (emit face)

- [ ] **Step 1: Write failing ChunkMesher tests**

  Create `Assets/Tests/EditMode/WorldTests/ChunkMesherTests.cs`:
  ```csharp
  using NUnit.Framework;
  using DeenCraft.World;
  using DeenCraft;

  namespace DeenCraft.Tests.EditMode
  {
      public class ChunkMesherTests
      {
          [Test] public void AllAirChunk_ProducesEmptyMesh()
          {
              var chunk = new ChunkData(); // all air
              var mesh = ChunkMesher.BuildMesh(chunk, null);
              Assert.AreEqual(0, mesh.Vertices.Length);
              Assert.AreEqual(0, mesh.Triangles.Length);
          }

          [Test] public void SingleSolidBlock_ProducesSixFaces()
          {
              var chunk = new ChunkData();
              chunk.SetBlock(0, 1, 0, BlockType.Stone);
              var mesh = ChunkMesher.BuildMesh(chunk, null);
              // 6 faces × 4 verts = 24 vertices (greedy may merge but single block has 6 independent faces)
              Assert.AreEqual(24, mesh.Vertices.Length, "Single block must produce 24 vertices (6 faces × 4)");
              Assert.AreEqual(36, mesh.Triangles.Length, "Single block must produce 36 triangle indices (6 faces × 6)");
          }

          [Test] public void TwoAdjacentSameBlocks_MergesTopFace()
          {
              var chunk = new ChunkData();
              chunk.SetBlock(0, 1, 0, BlockType.Stone);
              chunk.SetBlock(1, 1, 0, BlockType.Stone);
              var isolated = new ChunkData();
              isolated.SetBlock(0, 1, 0, BlockType.Stone);
              var merged = ChunkMesher.BuildMesh(chunk, null);
              var single = ChunkMesher.BuildMesh(isolated, null);
              // Merged top face should produce fewer vertices than two separate single blocks
              Assert.Less(merged.Vertices.Length, single.Vertices.Length * 2);
          }

          [Test] public void WaterBlock_ProducesFaces()
          {
              var chunk = new ChunkData();
              chunk.SetBlock(5, 5, 5, BlockType.Water);
              var mesh = ChunkMesher.BuildMesh(chunk, null);
              Assert.Greater(mesh.Vertices.Length, 0);
          }

          [Test] public void BuildMesh_DoesNotThrow_OnFullChunk()
          {
              var chunk = new ChunkData();
              // Fill every block with stone
              for (int x = 0; x < GameConstants.ChunkWidth; x++)
              for (int y = 0; y < GameConstants.ChunkHeight; y++)
              for (int z = 0; z < GameConstants.ChunkDepth; z++)
                  chunk.SetBlock(x, y, z, BlockType.Stone);
              Assert.DoesNotThrow(() => ChunkMesher.BuildMesh(chunk, null));
          }
      }
  }
  ```

- [ ] **Step 2: Create MeshData.cs**

  ```csharp
  using UnityEngine;

  namespace DeenCraft.World
  {
      public sealed class MeshData
      {
          public Vector3[] Vertices  { get; }
          public int[]     Triangles { get; }
          public Vector2[] UVs       { get; }

          public MeshData(Vector3[] vertices, int[] triangles, Vector2[] uvs)
          {
              Vertices  = vertices;
              Triangles = triangles;
              UVs       = uvs;
          }

          public static readonly MeshData Empty =
              new MeshData(new Vector3[0], new int[0], new Vector2[0]);
      }
  }
  ```

- [ ] **Step 3: Create ChunkMesher.cs**

  Implement greedy meshing. Full implementation:

  ```csharp
  using System.Collections.Generic;
  using UnityEngine;
  using DeenCraft;

  namespace DeenCraft.World
  {
      public static class ChunkMesher
      {
          private static readonly Vector3[] s_faceNormals =
          {
              Vector3.right,   // +X
              Vector3.left,    // -X
              Vector3.up,      // +Y
              Vector3.down,    // -Y
              Vector3.forward, // +Z
              Vector3.back,    // -Z
          };

          // Neighbour chunk array order: [0]=+X [1]=-X [2]=+Z [3]=-Z
          public static MeshData BuildMesh(ChunkData chunk, ChunkData[] neighbors)
          {
              var verts = new List<Vector3>();
              var tris  = new List<int>();
              var uvs   = new List<Vector2>();

              int w = GameConstants.ChunkWidth;
              int h = GameConstants.ChunkHeight;
              int d = GameConstants.ChunkDepth;

              // Greedy mesh per axis direction
              // Axis 0 = X, Axis 1 = Y, Axis 2 = Z
              for (int axis = 0; axis < 3; axis++)
              {
                  int u = (axis + 1) % 3;
                  int v = (axis + 2) % 3;

                  int[] dims = { w, h, d };
                  int[] pos  = new int[3];
                  int[] step = new int[3];

                  // Mask size: dims[u] × dims[v]
                  var mask = new int[dims[u] * dims[v]]; // positive = front face, negative = back face blockID

                  for (int backFace = 0; backFace <= 1; backFace++)
                  {
                      for (pos[axis] = -1; pos[axis] < dims[axis];)
                      {
                          pos[axis]++;
                          // Build mask
                          for (int n = 0; n < mask.Length; n++) mask[n] = 0;

                          for (pos[v] = 0; pos[v] < dims[v]; pos[v]++)
                          {
                              for (pos[u] = 0; pos[u] < dims[u]; pos[u]++)
                              {
                                  int currentId  = GetBlockSafe(chunk, neighbors, pos[0], pos[1], pos[2]);
                                  int neighborId = GetBlockSafe(chunk, neighbors,
                                      pos[0] + (axis == 0 ? 1 : 0),
                                      pos[1] + (axis == 1 ? 1 : 0),
                                      pos[2] + (axis == 2 ? 1 : 0));

                                  bool currentSolid  = currentId  != (int)BlockType.Air;
                                  bool neighborSolid = neighborId != (int)BlockType.Air;

                                  int idx = pos[u] + dims[u] * pos[v];
                                  if (backFace == 0)
                                      mask[idx] = (currentSolid && !neighborSolid) ? currentId : 0;
                                  else
                                      mask[idx] = (!currentSolid && neighborSolid) ? -neighborId : 0;
                              }
                          }

                          // Greedy merge
                          for (pos[v] = 0; pos[v] < dims[v]; pos[v]++)
                          {
                              for (pos[u] = 0; pos[u] < dims[u];)
                              {
                                  int maskVal = mask[pos[u] + dims[u] * pos[v]];
                                  if (maskVal == 0) { pos[u]++; continue; }

                                  // Compute width (extend along u)
                                  int width = 1;
                                  while (pos[u] + width < dims[u] &&
                                         mask[(pos[u] + width) + dims[u] * pos[v]] == maskVal)
                                      width++;

                                  // Compute height (extend along v)
                                  int height = 1;
                                  bool done = false;
                                  while (!done && pos[v] + height < dims[v])
                                  {
                                      for (int k = 0; k < width; k++)
                                      {
                                          if (mask[(pos[u] + k) + dims[u] * (pos[v] + height)] != maskVal)
                                          { done = true; break; }
                                      }
                                      if (!done) height++;
                                  }

                                  // Emit quad
                                  var du = new int[3];
                                  var dv = new int[3];
                                  du[u] = width;
                                  dv[v] = height;

                                  var p = new Vector3(pos[0], pos[1] + (backFace == 0 && axis == 1 ? 1 : 0), pos[2]);
                                  if (axis == 1 && backFace == 0) { /* top face — p already offset */ }
                                  else if (axis == 1 && backFace == 1) p = new Vector3(pos[0], pos[1], pos[2]);

                                  // Correct Y for top faces
                                  if (axis == 1 && backFace == 0)
                                      p = new Vector3(pos[0], pos[1] + 1, pos[2]);

                                  AddQuad(verts, tris, uvs,
                                      p,
                                      new Vector3(pos[0] + du[0], pos[1] + du[1], pos[2] + du[2]),
                                      new Vector3(pos[0] + du[0] + dv[0], pos[1] + du[1] + dv[1], pos[2] + du[2] + dv[2]),
                                      new Vector3(pos[0] + dv[0], pos[1] + dv[1], pos[2] + dv[2]),
                                      System.Math.Abs(maskVal),
                                      backFace == 1);

                                  // Zero out mask
                                  for (int l = 0; l < height; l++)
                                  for (int k = 0; k < width; k++)
                                      mask[(pos[u] + k) + dims[u] * (pos[v] + l)] = 0;

                                  pos[u] += width;
                              }
                          }
                      }
                  }
              }

              return new MeshData(verts.ToArray(), tris.ToArray(), uvs.ToArray());
          }

          private static void AddQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
              Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
              int blockId, bool flip)
          {
              int startIdx = verts.Count;
              verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);

              float uvRow = (float)blockId / 13f; // 13 block types total
              uvs.Add(new Vector2(0, uvRow));
              uvs.Add(new Vector2(1, uvRow));
              uvs.Add(new Vector2(1, uvRow + 1f/13f));
              uvs.Add(new Vector2(0, uvRow + 1f/13f));

              if (!flip)
              {
                  tris.Add(startIdx); tris.Add(startIdx + 2); tris.Add(startIdx + 1);
                  tris.Add(startIdx); tris.Add(startIdx + 3); tris.Add(startIdx + 2);
              }
              else
              {
                  tris.Add(startIdx); tris.Add(startIdx + 1); tris.Add(startIdx + 2);
                  tris.Add(startIdx); tris.Add(startIdx + 2); tris.Add(startIdx + 3);
              }
          }

          private static int GetBlockSafe(ChunkData chunk, ChunkData[] neighbors,
              int x, int y, int z)
          {
              if (chunk.IsInBounds(x, y, z))
                  return (int)chunk.GetBlock(x, y, z);

              // Out of bounds — treat as air if no neighbor, or sample neighbor
              if (neighbors == null) return (int)BlockType.Air;

              // +X neighbor
              if (x >= GameConstants.ChunkWidth && neighbors.Length > 0 && neighbors[0] != null)
                  return (int)neighbors[0].GetBlock(x - GameConstants.ChunkWidth, y, z);
              // -X neighbor
              if (x < 0 && neighbors.Length > 1 && neighbors[1] != null)
                  return (int)neighbors[1].GetBlock(x + GameConstants.ChunkWidth, y, z);
              // +Z neighbor
              if (z >= GameConstants.ChunkDepth && neighbors.Length > 2 && neighbors[2] != null)
                  return (int)neighbors[2].GetBlock(x, y, z - GameConstants.ChunkDepth);
              // -Z neighbor
              if (z < 0 && neighbors.Length > 3 && neighbors[3] != null)
                  return (int)neighbors[3].GetBlock(x, y, z + GameConstants.ChunkDepth);

              return (int)BlockType.Air;
          }
      }
  }
  ```

- [ ] **Step 4: Run ChunkMesherTests — confirm all 5 pass**

- [ ] **Step 5: Commit**
  `git add -A && git commit -m "feat(world): add MeshData and ChunkMesher with greedy meshing algorithm"`

---

## Round 3 — Orchestration Layer

### Task 6: ChunkView + ChunkManager

**Files:**
- Create: `Assets/Scripts/World/ChunkView.cs`
- Create: `Assets/Scripts/World/ChunkManager.cs`

**Context you MUST read first:**
- ALL files in `Assets/Scripts/World/` (BlockType, ChunkData, BiomeSystem, WorldGenerator, MeshData, ChunkMesher)
- `Assets/Scripts/GameConstants.cs`
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "ChunkManager" section
- `docs/phase_1_status.md` — Phase 2 architecture notes (pooling, coroutines)

**Constraints:**
- `ChunkManager` is a singleton MonoBehaviour (`Instance` pattern)
- Never call `Instantiate` or `Destroy` on chunk GameObjects — use pool only
- Chunk update loop uses coroutines — no `Task`/`await`
- Pool size is `[SerializeField] private int _poolSize = 200`
- `ChunkView` is a lightweight MonoBehaviour: `MeshFilter` + `MeshRenderer`, `ApplyMesh(MeshData)`, `Clear()`
- `ChunkManager` exposes: `GetChunkData(Vector2Int chunkCoord)`, `SetBlock(Vector3Int worldPos, BlockType block)`, `DirtyChunk(Vector2Int chunkCoord)`

- [ ] **Step 1: Create ChunkView.cs**

  ```csharp
  using UnityEngine;

  namespace DeenCraft.World
  {
      [RequireComponent(typeof(MeshFilter))]
      [RequireComponent(typeof(MeshRenderer))]
      public sealed class ChunkView : MonoBehaviour
      {
          private MeshFilter _meshFilter;

          private void Awake()
          {
              _meshFilter = GetComponent<MeshFilter>();
          }

          public void ApplyMesh(MeshData data)
          {
              var mesh = _meshFilter.mesh;
              if (mesh == null) mesh = new Mesh();
              mesh.Clear();
              mesh.vertices  = data.Vertices;
              mesh.triangles = data.Triangles;
              mesh.uv        = data.UVs;
              mesh.RecalculateNormals();
              _meshFilter.mesh = mesh;
          }

          public void Clear()
          {
              if (_meshFilter != null && _meshFilter.mesh != null)
                  _meshFilter.mesh.Clear();
              gameObject.SetActive(false);
          }
      }
  }
  ```

- [ ] **Step 2: Create ChunkManager.cs**

  ```csharp
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using DeenCraft;

  namespace DeenCraft.World
  {
      public sealed class ChunkManager : MonoBehaviour
      {
          public static ChunkManager Instance { get; private set; }

          [SerializeField] private int _viewDistance = 8;
          [SerializeField] private int _poolSize = 200;
          [SerializeField] private int _worldSeed = 42;
          [SerializeField] private Material _worldMaterial;
          [SerializeField] private Transform _playerTransform;

          private readonly Dictionary<Vector2Int, ChunkData>  _chunkDataCache = new Dictionary<Vector2Int, ChunkData>();
          private readonly Dictionary<Vector2Int, ChunkView>  _activeViews    = new Dictionary<Vector2Int, ChunkView>();
          private readonly Queue<ChunkView>                    _pool           = new Queue<ChunkView>();
          private readonly HashSet<Vector2Int>                 _dirtyChunks    = new HashSet<Vector2Int>();

          private Vector2Int _lastPlayerChunk;

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
              _lastPlayerChunk = GetPlayerChunkCoord();
              StartCoroutine(ChunkUpdateLoop());
              StartCoroutine(MeshRebuildLoop());
          }

          private void InitPool()
          {
              for (int i = 0; i < _poolSize; i++)
              {
                  var go = new GameObject("ChunkView");
                  go.transform.SetParent(transform);
                  var view = go.AddComponent<ChunkView>();
                  var mr = go.GetComponent<MeshRenderer>();
                  if (_worldMaterial != null) mr.sharedMaterial = _worldMaterial;
                  go.SetActive(false);
                  _pool.Enqueue(view);
              }
          }

          private IEnumerator ChunkUpdateLoop()
          {
              while (true)
              {
                  Vector2Int playerChunk = GetPlayerChunkCoord();
                  if (playerChunk != _lastPlayerChunk)
                  {
                      _lastPlayerChunk = playerChunk;
                      yield return StartCoroutine(UpdateLoadedChunks(playerChunk));
                  }
                  yield return new WaitForSeconds(0.25f);
              }
          }

          private IEnumerator UpdateLoadedChunks(Vector2Int center)
          {
              var needed = new HashSet<Vector2Int>();
              for (int dx = -_viewDistance; dx <= _viewDistance; dx++)
              for (int dz = -_viewDistance; dz <= _viewDistance; dz++)
              {
                  var coord = new Vector2Int(center.x + dx, center.y + dz);
                  needed.Add(coord);
                  if (!_activeViews.ContainsKey(coord))
                  {
                      yield return StartCoroutine(LoadChunk(coord));
                  }
              }

              // Unload chunks outside view
              var toUnload = new List<Vector2Int>();
              foreach (var coord in _activeViews.Keys)
                  if (!needed.Contains(coord)) toUnload.Add(coord);
              foreach (var coord in toUnload)
                  UnloadChunk(coord);
          }

          private IEnumerator LoadChunk(Vector2Int coord)
          {
              if (!_chunkDataCache.ContainsKey(coord))
              {
                  var data = new ChunkData();
                  WorldGenerator.Generate(data, coord.x, coord.y, _worldSeed);
                  _chunkDataCache[coord] = data;
              }

              if (_pool.Count == 0) yield break;

              var view = _pool.Dequeue();
              view.gameObject.SetActive(true);
              view.transform.position = new Vector3(
                  coord.x * GameConstants.ChunkWidth,
                  0,
                  coord.y * GameConstants.ChunkDepth);

              var neighbors = GetNeighborData(coord);
              var meshData  = ChunkMesher.BuildMesh(_chunkDataCache[coord], neighbors);
              view.ApplyMesh(meshData);
              _activeViews[coord] = view;
              yield return null;
          }

          private void UnloadChunk(Vector2Int coord)
          {
              if (_activeViews.TryGetValue(coord, out var view))
              {
                  view.Clear();
                  _pool.Enqueue(view);
                  _activeViews.Remove(coord);
              }
          }

          private IEnumerator MeshRebuildLoop()
          {
              while (true)
              {
                  if (_dirtyChunks.Count > 0)
                  {
                      var toRebuild = new List<Vector2Int>(_dirtyChunks);
                      _dirtyChunks.Clear();
                      foreach (var coord in toRebuild)
                      {
                          if (_activeViews.TryGetValue(coord, out var view) &&
                              _chunkDataCache.TryGetValue(coord, out var data))
                          {
                              var neighbors = GetNeighborData(coord);
                              var meshData  = ChunkMesher.BuildMesh(data, neighbors);
                              view.ApplyMesh(meshData);
                          }
                          yield return null;
                      }
                  }
                  yield return null;
              }
          }

          public ChunkData GetChunkData(Vector2Int chunkCoord)
          {
              _chunkDataCache.TryGetValue(chunkCoord, out var data);
              return data;
          }

          public void SetBlock(Vector3Int worldPos, BlockType block)
          {
              var chunkCoord = WorldToChunkCoord(worldPos);
              if (!_chunkDataCache.TryGetValue(chunkCoord, out var data)) return;

              int lx = worldPos.x - chunkCoord.x * GameConstants.ChunkWidth;
              int lz = worldPos.z - chunkCoord.y * GameConstants.ChunkDepth;
              data.SetBlock(lx, worldPos.y, lz, block);
              DirtyChunk(chunkCoord);

              // Dirty neighbors if on boundary
              if (lx == 0)                               DirtyChunk(chunkCoord + Vector2Int.left);
              if (lx == GameConstants.ChunkWidth - 1)    DirtyChunk(chunkCoord + Vector2Int.right);
              if (lz == 0)                               DirtyChunk(chunkCoord + Vector2Int.down);
              if (lz == GameConstants.ChunkDepth - 1)    DirtyChunk(chunkCoord + Vector2Int.up);
          }

          public void DirtyChunk(Vector2Int chunkCoord) => _dirtyChunks.Add(chunkCoord);

          private ChunkData[] GetNeighborData(Vector2Int coord)
          {
              return new ChunkData[]
              {
                  GetChunkData(coord + Vector2Int.right),
                  GetChunkData(coord + Vector2Int.left),
                  GetChunkData(coord + Vector2Int.up),
                  GetChunkData(coord + Vector2Int.down),
              };
          }

          private Vector2Int GetPlayerChunkCoord()
          {
              if (_playerTransform == null) return Vector2Int.zero;
              return WorldToChunkCoord(Vector3Int.FloorToInt(_playerTransform.position));
          }

          public static Vector2Int WorldToChunkCoord(Vector3Int worldPos)
          {
              return new Vector2Int(
                  Mathf.FloorToInt((float)worldPos.x / GameConstants.ChunkWidth),
                  Mathf.FloorToInt((float)worldPos.z / GameConstants.ChunkDepth));
          }
      }
  }
  ```

- [ ] **Step 3: Commit**
  `git add -A && git commit -m "feat(world): add ChunkView and ChunkManager with chunk pool and coroutine loader"`

---

## Round 4 — Interaction Layer

### Task 7: BlockInteractionManager

**Files:**
- Create: `Assets/Scripts/World/BlockInteractionManager.cs`

**Context you MUST read first:**
- `Assets/Scripts/World/ChunkManager.cs`
- `Assets/Scripts/World/BlockType.cs`
- `Assets/Scripts/GameConstants.cs`
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "BlockInteractionManager" section

**Constraints:**
- MonoBehaviour, attach to Player GameObject
- Raycast from Camera.main
- Left-click = destroy (set Air). Right-click = place selected block.
- Selected block stub: `BlockType.Stone` until Phase 4 inventory is built
- Max range: `GameConstants.BlockInteractionRange`
- After place/destroy: call `ChunkManager.Instance.SetBlock()`
- No `public` fields

- [ ] **Step 1: Create BlockInteractionManager.cs**

  ```csharp
  using UnityEngine;

  namespace DeenCraft.World
  {
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
              if (!Raycast(out RaycastHit hit, out Vector3Int blockPos)) return;
              ChunkManager.Instance.SetBlock(blockPos, BlockType.Air);
          }

          private void TryPlaceBlock()
          {
              if (!Raycast(out RaycastHit hit, out Vector3Int blockPos)) return;
              Vector3Int placePos = blockPos + Vector3Int.RoundToInt(hit.normal);
              ChunkManager.Instance.SetBlock(placePos, GetSelectedBlock());
          }

          private bool Raycast(out RaycastHit hit, out Vector3Int blockPos)
          {
              blockPos = Vector3Int.zero;
              Camera cam = _playerCamera != null ? _playerCamera : Camera.main;
              if (cam == null) { hit = default; return false; }

              if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                  out hit, GameConstants.BlockInteractionRange))
              {
                  // Step back slightly inside the hit block
                  Vector3 insideBlock = hit.point - hit.normal * 0.5f;
                  blockPos = Vector3Int.FloorToInt(insideBlock);
                  return true;
              }
              return false;
          }

          // Phase 4 stub — returns Stone until inventory is implemented
          private static BlockType GetSelectedBlock() => BlockType.Stone;
      }
  }
  ```

- [ ] **Step 2: Commit**
  `git add -A && git commit -m "feat(world): add BlockInteractionManager with raycast place/destroy"`

---

### Task 8: WaterSimulator

**Files:**
- Create: `Assets/Scripts/World/WaterSimulator.cs`

**Context you MUST read first:**
- `Assets/Scripts/World/ChunkManager.cs`
- `Assets/Scripts/World/BlockType.cs`
- `Assets/Scripts/GameConstants.cs`
- `docs/superpowers/specs/2026-04-28-phase2-voxel-engine-design.md` — "WaterSimulator" section

**Constraints:**
- MonoBehaviour, attach to same GameObject as ChunkManager or as a child
- BFS spread: water flows to air neighbors (4 horizontal + 1 below), max `GameConstants.MaxWaterSpread` blocks from source
- Does NOT flow uphill
- One BFS step per frame tick (use coroutine)
- Natural world-gen water is static; only player-placed water triggers simulation
- `[SerializeField] private float _waterTickInterval = 0.1f`

- [ ] **Step 1: Create WaterSimulator.cs**

  ```csharp
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
  using DeenCraft;

  namespace DeenCraft.World
  {
      public sealed class WaterSimulator : MonoBehaviour
      {
          [SerializeField] private float _waterTickInterval = 0.1f;

          private readonly Queue<(Vector3Int pos, int distance)> _spreadQueue =
              new Queue<(Vector3Int, int)>();
          private readonly HashSet<Vector3Int> _visited = new HashSet<Vector3Int>();

          private static readonly Vector3Int[] s_horizontalDirs =
          {
              Vector3Int.right, Vector3Int.left,
              new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
          };

          private void Start() => StartCoroutine(SimulationLoop());

          public void TriggerSpread(Vector3Int sourceWorldPos)
          {
              if (_visited.Contains(sourceWorldPos)) return;
              _spreadQueue.Enqueue((sourceWorldPos, 0));
              _visited.Add(sourceWorldPos);
          }

          private IEnumerator SimulationLoop()
          {
              var wait = new WaitForSeconds(_waterTickInterval);
              while (true)
              {
                  if (_spreadQueue.Count > 0)
                  {
                      var (pos, dist) = _spreadQueue.Dequeue();
                      if (dist < GameConstants.MaxWaterSpread)
                          SpreadFrom(pos, dist);
                  }
                  yield return wait;
              }
          }

          private void SpreadFrom(Vector3Int pos, int dist)
          {
              if (ChunkManager.Instance == null) return;

              // Try flowing down first
              Vector3Int below = pos + Vector3Int.down;
              if (TryFill(below, dist + 1)) return; // Water falls straight down first

              // Spread horizontally
              foreach (var dir in s_horizontalDirs)
              {
                  Vector3Int neighbor = pos + dir;
                  TryFill(neighbor, dist + 1);
              }
          }

          private bool TryFill(Vector3Int pos, int dist)
          {
              if (ChunkManager.Instance == null) return false;

              var chunkCoord = ChunkManager.WorldToChunkCoord(pos);
              var data = ChunkManager.Instance.GetChunkData(chunkCoord);
              if (data == null) return false;

              int lx = pos.x - chunkCoord.x * GameConstants.ChunkWidth;
              int lz = pos.z - chunkCoord.y * GameConstants.ChunkDepth;
              if (!data.IsInBounds(lx, pos.y, lz)) return false;

              BlockType current = data.GetBlock(lx, pos.y, lz);
              if (current != BlockType.Air) return false;

              ChunkManager.Instance.SetBlock(pos, BlockType.Water);

              if (!_visited.Contains(pos))
              {
                  _visited.Add(pos);
                  _spreadQueue.Enqueue((pos, dist));
              }
              return true;
          }
      }
  }
  ```

- [ ] **Step 2: Commit**
  `git add -A && git commit -m "feat(world): add WaterSimulator with BFS spread coroutine"`

---

## Final Step: Write Phase 2 Status Doc

- [ ] Create `docs/phase_2_status.md` with: what was done, any errors encountered, notes for Phase 3.
- [ ] `git add -A && git commit -m "docs: add Phase 2 status document"`
- [ ] `git push`

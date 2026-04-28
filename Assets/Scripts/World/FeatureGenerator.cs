using UnityEngine;
using DeenCraft;

namespace DeenCraft.World
{
    /// <summary>
    /// Decorates a generated chunk with trees, vegetation, structures, oases, and boats.
    /// Called at the end of WorldGenerator.Generate.
    /// All features are self-contained within the chunk (no cross-chunk writes).
    /// Deterministic: identical inputs always produce identical output.
    /// </summary>
    public static class FeatureGenerator
    {
        // ── Entry point ───────────────────────────────────────────────────────

        public static void Decorate(ChunkData chunk, int chunkX, int chunkZ, int seed)
        {
            var rng = new System.Random(HashSeed(chunkX, chunkZ, seed));

            BiomeType biome = BiomeSystem.GetBiome(
                chunkX * GameConstants.ChunkWidth  + GameConstants.ChunkWidth  / 2,
                chunkZ * GameConstants.ChunkDepth  + GameConstants.ChunkDepth  / 2,
                seed);

            PlantTrees(chunk, chunkX, chunkZ, seed, biome, rng);
            PlantVegetation(chunk, chunkX, chunkZ, seed, biome, rng);
            PlaceStructures(chunk, chunkX, chunkZ, seed, biome, rng);
        }

        // ── Trees ─────────────────────────────────────────────────────────────

        private static void PlantTrees(ChunkData chunk, int chunkX, int chunkZ, int seed, BiomeType biome, System.Random rng)
        {
            // Desert trees only come from oasis — handled in PlaceStructures
            if (biome == BiomeType.Desert) return;

            float threshold;
            switch (biome)
            {
                case BiomeType.Grassland:   threshold = 0.65f; break;
                case BiomeType.OliveGrove:  threshold = 0.60f; break;
                case BiomeType.Riverside:   threshold = 0.70f; break;
                case BiomeType.SnowyIsland: threshold = 0.55f; break;
                default: return;
            }

            int treesPlanted = 0;
            // Only plant in columns ≥2 from chunk edge to keep features self-contained
            for (int lx = 2; lx <= 13 && treesPlanted < GameConstants.MaxTreesPerChunk; lx++)
            {
                for (int lz = 2; lz <= 13 && treesPlanted < GameConstants.MaxTreesPerChunk; lz++)
                {
                    float worldX = chunkX * GameConstants.ChunkWidth  + lx;
                    float worldZ = chunkZ * GameConstants.ChunkDepth  + lz;

                    float treeNoise = Mathf.PerlinNoise(
                        worldX * GameConstants.TreeNoiseScale,
                        worldZ * GameConstants.TreeNoiseScale);

                    if (treeNoise <= threshold) continue;

                    int surfY = FindSurfaceY(chunk, lx, lz);
                    if (surfY < 0) continue;

                    BlockType surfBlock = chunk.GetBlock(lx, surfY, lz);
                    bool validSurface = surfBlock == BlockType.Grass ||
                                        (biome == BiomeType.SnowyIsland && surfBlock == BlockType.Snow);
                    if (!validSurface) continue;

                    PlaceTree(chunk, lx, surfY, lz, biome, rng);
                    treesPlanted++;
                }
            }
        }

        private static void PlaceTree(ChunkData chunk, int x, int surfY, int z, BiomeType biome, System.Random rng)
        {
            switch (biome)
            {
                case BiomeType.Riverside:
                    PlacePalmTree(chunk, x, surfY, z, rng);
                    break;
                case BiomeType.OliveGrove:
                    PlaceOliveTree(chunk, x, surfY, z, rng);
                    break;
                case BiomeType.SnowyIsland:
                    PlaceSnowTree(chunk, x, surfY, z, rng);
                    break;
                default:
                    PlaceRegularTree(chunk, x, surfY, z, rng);
                    break;
            }
        }

        private static void PlaceRegularTree(ChunkData chunk, int x, int surfY, int z, System.Random rng)
        {
            int height = rng.Next(4, 7); // 4–6
            // Trunk
            for (int y = surfY + 1; y <= surfY + height; y++)
                SafeSet(chunk, x, y, z, BlockType.Wood);
            // Canopy 3×3 at top-1 and top
            for (int dy = -1; dy <= 0; dy++)
            {
                int cy = surfY + height + dy;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                        if (chunk.IsInBounds(x + dx, cy, z + dz))
                            if (chunk.GetBlock(x + dx, cy, z + dz) == BlockType.Air)
                                SafeSet(chunk, x + dx, cy, z + dz, BlockType.Leaves);
            }
            // Cap 1×1
            SafeSet(chunk, x, surfY + height + 1, z, BlockType.Leaves);
        }

        private static void PlaceOliveTree(ChunkData chunk, int x, int surfY, int z, System.Random rng)
        {
            int height = rng.Next(3, 6); // 3–5
            // Trunk
            for (int y = surfY + 1; y <= surfY + height; y++)
                SafeSet(chunk, x, y, z, BlockType.Wood);
            // Canopy 3×3 at top and top-1
            for (int dy = -1; dy <= 0; dy++)
            {
                int cy = surfY + height + dy;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                        if (chunk.IsInBounds(x + dx, cy, z + dz))
                            if (chunk.GetBlock(x + dx, cy, z + dz) == BlockType.Air)
                                SafeSet(chunk, x + dx, cy, z + dz, BlockType.OliveLeaves);
            }
        }

        private static void PlacePalmTree(ChunkData chunk, int x, int surfY, int z, System.Random rng)
        {
            int height = rng.Next(5, 8); // 5–7
            // Trunk
            for (int y = surfY + 1; y <= surfY + height; y++)
                SafeSet(chunk, x, y, z, BlockType.PalmWood);
            // Cross-shaped top
            int top = surfY + height;
            SafeSet(chunk, x,     top, z,     BlockType.Leaves);
            SafeSet(chunk, x - 1, top, z,     BlockType.Leaves);
            SafeSet(chunk, x + 1, top, z,     BlockType.Leaves);
            SafeSet(chunk, x,     top, z - 1, BlockType.Leaves);
            SafeSet(chunk, x,     top, z + 1, BlockType.Leaves);
        }

        private static void PlaceSnowTree(ChunkData chunk, int x, int surfY, int z, System.Random rng)
        {
            int height = rng.Next(4, 7); // 4–6
            // Same shape as regular tree
            for (int y = surfY + 1; y <= surfY + height; y++)
                SafeSet(chunk, x, y, z, BlockType.Wood);
            for (int dy = -1; dy <= 0; dy++)
            {
                int cy = surfY + height + dy;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                        if (chunk.IsInBounds(x + dx, cy, z + dz))
                            if (chunk.GetBlock(x + dx, cy, z + dz) == BlockType.Air)
                                SafeSet(chunk, x + dx, cy, z + dz, BlockType.Leaves);
            }
            SafeSet(chunk, x, surfY + height + 1, z, BlockType.Leaves);
            // Snow cap on topmost canopy layer
            SafeSet(chunk, x, surfY + height + 2, z, BlockType.Snow);
        }

        // ── Vegetation ────────────────────────────────────────────────────────

        private static void PlantVegetation(ChunkData chunk, int chunkX, int chunkZ, int seed, BiomeType biome, System.Random rng)
        {
            for (int lx = 0; lx < GameConstants.ChunkWidth; lx++)
            {
                for (int lz = 0; lz < GameConstants.ChunkDepth; lz++)
                {
                    int surfY = FindSurfaceY(chunk, lx, lz);
                    if (surfY < 0) continue;

                    int  plantY   = surfY + 1;
                    if (plantY >= GameConstants.ChunkHeight) continue;
                    if (chunk.GetBlock(lx, plantY, lz) != BlockType.Air) continue;

                    float worldX = chunkX * GameConstants.ChunkWidth  + lx;
                    float worldZ = chunkZ * GameConstants.ChunkDepth  + lz;

                    TryPlaceVegetation(chunk, lx, plantY, lz, chunk.GetBlock(lx, surfY, lz), worldX, worldZ, biome, rng);
                }
            }
        }

        private static void TryPlaceVegetation(ChunkData chunk, int x, int plantY, int z,
                                                BlockType surfBlock, float worldX, float worldZ,
                                                BiomeType biome, System.Random rng)
        {
            float vegNoise = Mathf.PerlinNoise(worldX * 0.15f + 71.3f, worldZ * 0.15f + 31.7f);

            if (biome == BiomeType.Desert && surfBlock == BlockType.Sand)
            {
                if (vegNoise > 0.70f)
                {
                    int cactusHeight = rng.Next(1, 4); // 1–3
                    for (int dy = 0; dy < cactusHeight; dy++)
                    {
                        int cy = plantY + dy;
                        if (cy >= GameConstants.ChunkHeight) break;
                        chunk.SetBlock(x, cy, z, BlockType.Cactus);
                    }
                }
                return;
            }

            if (surfBlock != BlockType.Grass) return;

            switch (biome)
            {
                case BiomeType.Grassland:
                    if (vegNoise > 0.78f) SafeSet(chunk, x, plantY, z, BlockType.Flower);
                    break;
                case BiomeType.Riverside:
                    if (vegNoise > 0.78f)      SafeSet(chunk, x, plantY, z, BlockType.Flower);
                    else if (vegNoise > 0.72f) SafeSet(chunk, x, plantY, z, BlockType.Wheat);
                    break;
                case BiomeType.OliveGrove:
                    if (vegNoise > 0.74f) SafeSet(chunk, x, plantY, z, BlockType.Flower);
                    break;
            }
        }

        // ── Structures ────────────────────────────────────────────────────────

        private static void PlaceStructures(ChunkData chunk, int chunkX, int chunkZ, int seed, BiomeType biome, System.Random rng)
        {
            if (biome == BiomeType.Desert)
            {
                PlaceOasis(chunk, chunkX, chunkZ, seed, rng);
                return;
            }

            bool villageEligible = biome == BiomeType.Grassland ||
                                   biome == BiomeType.OliveGrove ||
                                   biome == BiomeType.Riverside;

            if (villageEligible && IsVillageChunk(chunkX, chunkZ, seed))
            {
                int anchorX = GameConstants.HouseAnchorX;
                int anchorZ = GameConstants.HouseAnchorZ;
                int midX    = anchorX + GameConstants.HouseWidth  / 2;
                int midZ    = anchorZ + GameConstants.HouseDepth  / 2;
                int surfY   = FindSurfaceY(chunk, midX, midZ);

                if (surfY >= 0)
                    PlaceHouse(chunk, anchorX, surfY, anchorZ);

                if (biome == BiomeType.Riverside)
                    PlaceBoat(chunk, chunkX, chunkZ, seed, rng);
            }
        }

        private static bool IsVillageChunk(int chunkX, int chunkZ, int seed)
        {
            float noise = Mathf.PerlinNoise(
                chunkX * GameConstants.VillageNoiseScale + seed * 0.001f,
                chunkZ * GameConstants.VillageNoiseScale);
            return noise > GameConstants.VillageNoiseThreshold;
        }

        private static void PlaceHouse(ChunkData chunk, int anchorX, int surfaceY, int anchorZ)
        {
            int w = GameConstants.HouseWidth;   // 7
            int d = GameConstants.HouseDepth;   // 5
            int h = GameConstants.HouseHeight;  // 4

            // Floor
            for (int x = anchorX; x < anchorX + w; x++)
                for (int z = anchorZ; z < anchorZ + d; z++)
                    SafeSet(chunk, x, surfaceY, z, BlockType.MudBrick);

            // Walls (4 sides, h layers tall)
            for (int y = 1; y <= h; y++)
            {
                for (int x = anchorX; x < anchorX + w; x++)
                {
                    SafeSet(chunk, x, surfaceY + y, anchorZ,           BlockType.MudBrick); // south
                    SafeSet(chunk, x, surfaceY + y, anchorZ + d - 1,   BlockType.MudBrick); // north
                }
                for (int z = anchorZ; z < anchorZ + d; z++)
                {
                    SafeSet(chunk, anchorX,           surfaceY + y, z, BlockType.MudBrick); // west
                    SafeSet(chunk, anchorX + w - 1,   surfaceY + y, z, BlockType.MudBrick); // east
                }
            }

            // Door: Air in south wall, centre column, y+1 and y+2
            int doorX = anchorX + w / 2;
            SafeSet(chunk, doorX, surfaceY + 1, anchorZ, BlockType.Air);
            SafeSet(chunk, doorX, surfaceY + 2, anchorZ, BlockType.Air);

            // Window: Air in east wall at y+2, middle Z
            int winZ = anchorZ + d / 2;
            SafeSet(chunk, anchorX + w - 1, surfaceY + 2, winZ, BlockType.Air);

            // Roof: Thatch covering full footprint at y+h+1
            for (int x = anchorX; x < anchorX + w; x++)
                for (int z = anchorZ; z < anchorZ + d; z++)
                    SafeSet(chunk, x, surfaceY + h + 1, z, BlockType.Thatch);

            // Interior shelf: Wood along north interior wall at y+1
            for (int x = anchorX + 1; x < anchorX + w - 1; x++)
                SafeSet(chunk, x, surfaceY + 1, anchorZ + d - 2, BlockType.Wood);
        }

        // ── Oasis ─────────────────────────────────────────────────────────────

        private static void PlaceOasis(ChunkData chunk, int chunkX, int chunkZ, int seed, System.Random rng)
        {
            float noise = Mathf.PerlinNoise(
                chunkX * 0.5f + seed * 0.001f,
                chunkZ * 0.5f);
            if (noise <= GameConstants.OasisNoiseThreshold) return;

            int cx = 8, cz = 8;

            // Fill water in centre 5×5
            for (int x = cx - 2; x <= cx + 2; x++)
            {
                for (int z = cz - 2; z <= cz + 2; z++)
                {
                    for (int y = GameConstants.SeaLevel + 10; y >= GameConstants.SeaLevel - 1; y--)
                        SafeSet(chunk, x, y, z, BlockType.Air);
                    SafeSet(chunk, x, GameConstants.SeaLevel - 1, z, BlockType.Sand);
                    SafeSet(chunk, x, GameConstants.SeaLevel,     z, BlockType.Water);
                    SafeSet(chunk, x, GameConstants.SeaLevel + 1, z, BlockType.Water);
                }
            }

            // Sand border (1 block around water)
            for (int x = cx - 3; x <= cx + 3; x++)
            {
                for (int z = cz - 3; z <= cz + 3; z++)
                {
                    bool isWater = (x >= cx - 2 && x <= cx + 2 && z >= cz - 2 && z <= cz + 2);
                    if (!isWater)
                    {
                        int surfY = FindSurfaceY(chunk, x, z);
                        if (surfY >= 0) SafeSet(chunk, x, surfY, z, BlockType.Sand);
                    }
                }
            }

            // 4 Palm trees at corners (all at chunk positions 5 and 11 — well within safe zone [2,13])
            int[] cornersX = { cx - 3, cx + 3 };
            int[] cornersZ = { cz - 3, cz + 3 };
            int palmH = 5;
            foreach (int px in cornersX)
            {
                foreach (int pz in cornersZ)
                {
                    int surfY = FindSurfaceY(chunk, px, pz);
                    if (surfY < 0) continue;
                    for (int y = surfY + 1; y <= surfY + palmH; y++)
                        SafeSet(chunk, px, y, pz, BlockType.PalmWood);
                    int top = surfY + palmH;
                    SafeSet(chunk, px,     top, pz,     BlockType.Leaves);
                    SafeSet(chunk, px - 1, top, pz,     BlockType.Leaves);
                    SafeSet(chunk, px + 1, top, pz,     BlockType.Leaves);
                    SafeSet(chunk, px,     top, pz - 1, BlockType.Leaves);
                    SafeSet(chunk, px,     top, pz + 1, BlockType.Leaves);
                }
            }
        }

        // ── Boat ──────────────────────────────────────────────────────────────

        private static void PlaceBoat(ChunkData chunk, int chunkX, int chunkZ, int seed, System.Random rng)
        {
            if (!IsVillageChunk(chunkX, chunkZ, seed)) return;

            int boatX = GameConstants.RiverXMax + 1;
            int boatZ = GameConstants.ChunkDepth / 2;
            int boatY = GameConstants.SeaLevel;
            SafeSet(chunk, boatX, boatY, boatZ, BlockType.Boat);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int FindSurfaceY(ChunkData chunk, int x, int z)
        {
            for (int y = GameConstants.ChunkHeight - 1; y >= 0; y--)
            {
                if (chunk.GetBlock(x, y, z) != BlockType.Air)
                    return y;
            }
            return -1;
        }

        private static void SafeSet(ChunkData chunk, int x, int y, int z, BlockType block)
        {
            if (!chunk.IsInBounds(x, y, z)) return;
            chunk.SetBlock(x, y, z, block);
        }

        private static int HashSeed(int chunkX, int chunkZ, int seed)
        {
            return chunkX * 1000003 ^ chunkZ * 1000033 ^ seed;
        }
    }
}

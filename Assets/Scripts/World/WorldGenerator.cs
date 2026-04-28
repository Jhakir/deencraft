using DeenCraft;
using UnityEngine;

namespace DeenCraft.World
{
    /// <summary>
    /// Fills a ChunkData with procedurally generated terrain.
    /// Pure static — no MonoBehaviour, no side effects.
    /// Deterministic: identical inputs always produce identical output.
    /// </summary>
    public static class WorldGenerator
    {
        public static void Generate(ChunkData chunk, int chunkX, int chunkZ, int seed)
        {
            float seedOffset = seed * 0.17f;

            for (int lx = 0; lx < GameConstants.ChunkWidth; lx++)
            {
                for (int lz = 0; lz < GameConstants.ChunkDepth; lz++)
                {
                    float worldX = chunkX * GameConstants.ChunkWidth  + lx;
                    float worldZ = chunkZ * GameConstants.ChunkDepth  + lz;

                    int             surfaceY = GetSurfaceHeight(worldX, worldZ, seedOffset);
                    BiomeDefinition biome    = BiomeSystem.GetDefinition(
                                                  BiomeSystem.GetBiome(worldX, worldZ, seed));

                    for (int y = 0; y < GameConstants.ChunkHeight; y++)
                    {
                        chunk.SetBlock(lx, y, lz, ChooseBlock(y, surfaceY, biome));
                    }
                }
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static int GetSurfaceHeight(float worldX, float worldZ, float seedOffset)
        {
            float noise          = 0f;
            float amplitude      = 1f;
            float frequency      = GameConstants.TerrainNoiseScale;
            float totalAmplitude = 0f;

            for (int i = 0; i < GameConstants.TerrainOctaves; i++)
            {
                noise += Mathf.PerlinNoise(
                    worldX * frequency + seedOffset,
                    worldZ * frequency + seedOffset) * amplitude;
                totalAmplitude += amplitude;
                amplitude  *= GameConstants.TerrainPersistence;
                frequency  *= 2f;
            }

            noise /= totalAmplitude;

            int range = GameConstants.MaxTerrainHeight - GameConstants.MinTerrainHeight;
            return GameConstants.MinTerrainHeight + Mathf.FloorToInt(noise * range);
        }

        private static BlockType ChooseBlock(int y, int surfaceY, BiomeDefinition biome)
        {
            if (y == 0)            return BlockType.Stone;   // solid floor (no Bedrock block type)
            if (y > surfaceY)      return y < GameConstants.SeaLevel ? BlockType.Water : BlockType.Air;
            if (y == surfaceY)     return biome.SurfaceBlock;
            if (y >= surfaceY - 3) return biome.FillerBlock;
            return BlockType.Stone;
        }
    }
}

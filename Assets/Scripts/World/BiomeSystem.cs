using System.Collections.Generic;
using DeenCraft;
using UnityEngine;

namespace DeenCraft.World
{
    /// <summary>
    /// Classifies world coordinates into biomes using two independent Perlin noise channels
    /// (temperature and humidity). Seed offsets the noise origin.
    /// </summary>
    public static class BiomeSystem
    {
        private static readonly Dictionary<BiomeType, BiomeDefinition> s_definitions =
            new Dictionary<BiomeType, BiomeDefinition>
        {
            { BiomeType.Desert,      new BiomeDefinition(BiomeType.Desert,      BlockType.Sand,  BlockType.Sand,  62, 78) },
            { BiomeType.Grassland,   new BiomeDefinition(BiomeType.Grassland,   BlockType.Grass, BlockType.Dirt,  64, 90) },
            { BiomeType.SnowyIsland, new BiomeDefinition(BiomeType.SnowyIsland, BlockType.Snow,  BlockType.Dirt,  64, 85) },
            { BiomeType.OliveGrove,  new BiomeDefinition(BiomeType.OliveGrove,  BlockType.Grass, BlockType.Dirt,  65, 95) },
            { BiomeType.Riverside,   new BiomeDefinition(BiomeType.Riverside,   BlockType.Grass, BlockType.Dirt,  62, 70) },
        };

        /// <summary>Returns the BiomeDefinition for the given biome type.</summary>
        public static BiomeDefinition GetDefinition(BiomeType biome) => s_definitions[biome];

        /// <summary>
        /// Classifies a world-space (x, z) position into a biome.
        /// Deterministic: same (worldX, worldZ, seed) always returns the same BiomeType.
        /// </summary>
        public static BiomeType GetBiome(float worldX, float worldZ, int seed)
        {
            float offset      = seed * 0.1f;
            float scale       = GameConstants.BiomeNoiseScale;

            float temperature = Mathf.PerlinNoise(worldX * scale + offset,
                                                  worldZ * scale + offset);
            float humidity    = Mathf.PerlinNoise(worldX * scale + offset + 100f,
                                                  worldZ * scale + offset + 100f);

            if (humidity    > 0.65f)                           return BiomeType.Riverside;
            if (temperature < 0.35f)                           return BiomeType.SnowyIsland;
            if (temperature > 0.65f && humidity < 0.35f)       return BiomeType.Desert;
            if (temperature > 0.50f && humidity > 0.50f)       return BiomeType.OliveGrove;
            return BiomeType.Grassland;
        }
    }
}

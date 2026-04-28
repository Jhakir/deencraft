using NUnit.Framework;
using DeenCraft.World;
using System.Collections.Generic;

namespace DeenCraft.Tests.EditMode
{
    public class BiomeSystemTests
    {
        [Test]
        public void GetBiome_SameSeedAndCoord_ReturnsSameBiome()
        {
            var a = BiomeSystem.GetBiome(100f, 200f, 42);
            var b = BiomeSystem.GetBiome(100f, 200f, 42);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void AllFiveBiomes_AreReachableWithSeed42()
        {
            var found = new HashSet<BiomeType>();
            for (int x = -2000; x <= 2000; x += 100)
            for (int z = -2000; z <= 2000; z += 100)
                found.Add(BiomeSystem.GetBiome(x, z, 42));
            Assert.AreEqual(5, found.Count,
                $"Only found {found.Count} biomes: {string.Join(", ", found)}");
        }

        [Test]
        public void BiomeDefinition_HasValidBlocks()
        {
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                var def = BiomeSystem.GetDefinition(biome);
                Assert.AreNotEqual(BlockType.Air, def.SurfaceBlock,
                    $"{biome} surface block must not be Air");
                Assert.AreNotEqual(BlockType.Air, def.FillerBlock,
                    $"{biome} filler block must not be Air");
                Assert.Greater(def.MaxHeight, def.MinHeight,
                    $"{biome} MaxHeight must exceed MinHeight");
            }
        }

        [Test]
        public void BiomeTypeEnum_HasFiveValues() =>
            Assert.AreEqual(5, System.Enum.GetValues(typeof(BiomeType)).Length);

        [Test]
        public void GetDefinition_ReturnsDefinitionForAllBiomes()
        {
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                var def = BiomeSystem.GetDefinition(biome);
                Assert.IsNotNull(def, $"Missing definition for {biome}");
                Assert.AreEqual(biome, def.Type);
            }
        }

        [Test]
        public void Riverside_HeightRange_IsRelativelyLow()
        {
            var def = BiomeSystem.GetDefinition(BiomeType.Riverside);
            // Riverside should be at or below sea level (64) at its minimum
            Assert.LessOrEqual(def.MinHeight, 64);
        }
    }
}

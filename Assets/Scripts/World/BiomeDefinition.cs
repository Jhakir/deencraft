namespace DeenCraft.World
{
    /// <summary>
    /// Describes the terrain characteristics of a biome.
    /// Pure data — no Unity dependencies.
    /// </summary>
    public sealed class BiomeDefinition
    {
        public BiomeType Type         { get; }
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

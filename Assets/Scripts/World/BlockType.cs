namespace DeenCraft.World
{
    /// <summary>
    /// Voxel block types. Values MUST stay in sync with GameConstants.Block* byte constants.
    /// </summary>
    public enum BlockType : byte
    {
        Air    = 0,
        Grass  = 1,
        Dirt   = 2,
        Stone  = 3,
        Sand   = 4,
        Snow   = 5,
        Water  = 6,
        Wood   = 7,
        Leaves = 8,
        Mosque = 9,
        Ice    = 10,
        Moss   = 11,
        Wheat  = 12,
    }
}

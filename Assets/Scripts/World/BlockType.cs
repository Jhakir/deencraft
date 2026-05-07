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
        Ice        = 10,
        Moss       = 11,
        Wheat      = 12,
        Cactus     = 13,
        MudBrick   = 14,
        PalmWood   = 15,
        OliveLeaves = 16,
        Flower     = 17,
        Boat       = 18,
        Thatch     = 19,

        // Phase 6 — Islamic cultural content
        Minaret    = 20,   // mosque minaret shaft block
        Dome       = 21,   // mosque dome cap block
        StoneArch  = 22,   // Palestinian stone arch
        Crescent   = 23,   // crescent-moon decorative block
        StarBlock  = 24,   // star decorative block
        AppleWood  = 25,   // apple tree trunk
        AppleLeaves = 26,  // apple tree canopy — drops Apple food item
        WaterSlide = 27,   // fun slide block — gives speed burst to player
    }
}

// Assets/Scripts/Player/ItemId.cs

namespace DeenCraft.Player
{
    public enum ItemId
    {
        None = 0,

        // Block items (match BlockType byte values 1-19)
        Grass     = 1,
        Dirt      = 2,
        Stone     = 3,
        Sand      = 4,
        Wood      = 5,
        Leaves    = 6,
        Water     = 7,
        OliveWood = 8,
        OliveLeaf = 9,
        PalmWood  = 10,
        PalmLeaf  = 11,
        MudBrick  = 12,
        SnowBlock = 13,
        IceBlock  = 14,
        Cobblestone = 15,
        Gravel    = 16,
        Flower    = 17,
        Wheat     = 18,
        Thatch    = 19,

        // Tool items
        WoodPickaxe  = 100,
        StonePickaxe = 101,
        WoodAxe      = 102,
        StoneAxe     = 103,
        WoodShovel   = 104,
        StoneShovel  = 105,

        // Crafting ingredients
        Stick  = 200,
        String = 201,

        // Food items
        Bread   = 300,
        Date    = 301,
        Fig     = 302,
        Falafel = 303,
    }
}

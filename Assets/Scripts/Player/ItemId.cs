// Assets/Scripts/Player/ItemId.cs
// NOTE: Block-range items do NOT have the same integer values as BlockType.
// Use BlockItemMap.BlockTypeToItemId / ItemIdToBlockType for bidirectional conversion.
// Ranges: block items 1-34, tools 100-105, currency 110-111, animal drops 120-124,
//         crafting 200-201, food 300-306.

namespace DeenCraft.Player
{
    public enum ItemId
    {
        None = 0,

        // Block items — range 1-19 (NOT matching BlockType integer values; see BlockItemMap)
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

        // Trade / currency items
        Diamond = 110,
        Gold    = 111,

        // Animal drops
        Wool    = 120,
        Meat    = 121,
        Egg     = 122,
        Milk    = 123,
        Feather = 124,

        // Crafting ingredients
        Stick  = 200,
        String = 201,

        // Cultural decorative blocks (placeable in world)
        Minaret    = 28,
        Dome       = 29,
        StoneArch  = 30,
        Crescent   = 31,
        StarBlock  = 32,
        AppleWood  = 33,
        WaterSlide = 34,

        // Food items
        Bread   = 300,
        Date    = 301,
        Fig     = 302,
        Falafel = 303,
        Fish    = 304,
        Olive   = 305,   // harvested from OliveLeaves
        Apple   = 306,   // harvested from AppleLeaves
    }
}


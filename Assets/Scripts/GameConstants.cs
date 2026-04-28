// ============================================================
// GameConstants.cs
// Deencraft — all magic-number constants in one place.
// Never use literal numbers in game code; reference this file.
// ============================================================
namespace DeenCraft
{
    public static class GameConstants
    {
        // ── Chunk / World ────────────────────────────────────
        public const int ChunkWidth  = 16;
        public const int ChunkHeight = 256;
        public const int ChunkDepth  = 16;

        /// <summary>How many chunks to load in each direction around the player.</summary>
        public const int RenderDistance = 8;

        /// <summary>Chunks kept in memory beyond render distance before being pooled.</summary>
        public const int ChunkPoolOverhang = 2;

        // ── Biomes ───────────────────────────────────────────
        public const int BiomeCount = 5;

        // ── Player ───────────────────────────────────────────
        public const float PlayerMoveSpeed    = 5.0f;
        public const float PlayerSprintSpeed  = 8.0f;
        public const float PlayerJumpForce    = 7.0f;
        public const float PlayerSwimSpeed    = 3.0f;
        public const float PlayerReach        = 5.0f;  // block-interaction range in world units
        public const float PlayerHeight       = 1.8f;
        public const float PlayerWidth        = 0.6f;

        // ── Inventory ────────────────────────────────────────
        public const int HotbarSlots   = 9;
        public const int BackpackSlots = 27;
        public const int MaxStackSize  = 64;

        // ── Health & Hunger ──────────────────────────────────
        public const int   MaxHealth          = 20;  // hearts × 2
        public const int   MaxHunger          = 20;
        public const float HungerDrainRate    = 0.05f; // points per second while active
        public const float HungerRegenThresh  = 18;    // above this hunger, health regenerates
        public const float HealthRegenRate    = 0.5f;  // HP per second when well-fed

        // ── Animals ──────────────────────────────────────────
        public const float AnimalWanderRadius   = 10.0f;
        public const float AnimalFleeRadius     = 6.0f;
        public const float AnimalFollowRadius   = 8.0f;
        public const float HorseRideSpeed       = 12.0f;

        // ── Crafting ─────────────────────────────────────────
        public const int CraftingGridSize = 3;  // 3×3 crafting table grid

        // ── Villager Trade ───────────────────────────────────
        public const int MaxTradesPerVillager = 8;

        // ── UI ───────────────────────────────────────────────
        public const float UIFadeSpeed = 3.0f;  // alpha units per second

        // ── Serialization / Save ─────────────────────────────
        /// <summary>Firestore collection for child world saves.</summary>
        public const string FirestoreWorldSavesCollection = "worldSaves";

        /// <summary>Firestore collection for child profiles.</summary>
        public const string FirestoreChildProfilesCollection = "childProfiles";

        /// <summary>Key used to cache the active child UID in PlayerPrefs.</summary>
        public const string PrefsActiveChildKey = "deencraft_active_child";

        /// <summary>Key used to cache the parent UID for persistent login.</summary>
        public const string PrefsParentUidKey = "deencraft_parent_uid";

        // ── Firebase Config ──────────────────────────────────
        /// <summary>Relative path inside StreamingAssets where firebase-config.json lives.</summary>
        public const string FirebaseConfigFileName = "firebase-config.json";

        // ── Build / WebGL ────────────────────────────────────
        public const int   TargetFrameRate       = 60;
        public const int   WebGLMemorySizeMB     = 512;
        public const float MaxLoadTimeSeconds    = 10.0f;  // performance target

        // ── Block IDs ────────────────────────────────────────
        // Keep in sync with BlockType enum in World/BlockType.cs
        public const byte BlockAir      = 0;
        public const byte BlockGrass    = 1;
        public const byte BlockDirt     = 2;
        public const byte BlockStone    = 3;
        public const byte BlockSand     = 4;
        public const byte BlockSnow     = 5;
        public const byte BlockWater    = 6;
        public const byte BlockWood     = 7;
        public const byte BlockLeaves   = 8;
        public const byte BlockMosque   = 9;  // decorative mosque tile
        public const byte BlockIce      = 10;
        public const byte BlockMoss     = 11;
        public const byte BlockWheat    = 12;
    }
}

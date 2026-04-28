using NUnit.Framework;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    /// <summary>
    /// Verifies that GameConstants values match design specifications.
    /// These tests will FAIL intentionally if someone accidentally changes a
    /// constant that would break game systems.
    /// </summary>
    [TestFixture]
    public class GameConstantsTests
    {
        // ── Chunk dimensions ─────────────────────────────────────────────

        [Test]
        public void ChunkWidth_Is16()
        {
            Assert.AreEqual(16, GameConstants.ChunkWidth,
                "Chunk width must be 16 — changing this breaks the entire voxel engine.");
        }

        [Test]
        public void ChunkHeight_Is256()
        {
            Assert.AreEqual(256, GameConstants.ChunkHeight,
                "Chunk height must be 256 — matches Minecraft standard and all world gen code.");
        }

        [Test]
        public void ChunkDepth_Is16()
        {
            Assert.AreEqual(16, GameConstants.ChunkDepth,
                "Chunk depth must be 16.");
        }

        [Test]
        public void ChunkDimensions_ArePositive()
        {
            Assert.Positive(GameConstants.ChunkWidth);
            Assert.Positive(GameConstants.ChunkHeight);
            Assert.Positive(GameConstants.ChunkDepth);
        }

        // ── Inventory ────────────────────────────────────────────────────

        [Test]
        public void HotbarSlots_Is9()
        {
            Assert.AreEqual(9, GameConstants.HotbarSlots,
                "Hotbar must have 9 slots — standard Minecraft layout.");
        }

        [Test]
        public void BackpackSlots_Is27()
        {
            Assert.AreEqual(27, GameConstants.BackpackSlots);
        }

        [Test]
        public void MaxStackSize_Is64()
        {
            Assert.AreEqual(64, GameConstants.MaxStackSize);
        }

        // ── Health & Hunger ──────────────────────────────────────────────

        [Test]
        public void MaxHealth_Is20()
        {
            Assert.AreEqual(20, GameConstants.MaxHealth,
                "Health is hearts × 2 — 20 = 10 hearts.");
        }

        [Test]
        public void MaxHunger_Is20()
        {
            Assert.AreEqual(20, GameConstants.MaxHunger);
        }

        [Test]
        public void HungerRegenThreshold_IsLessThanMaxHunger()
        {
            Assert.Less(GameConstants.HungerRegenThresh, GameConstants.MaxHunger,
                "Regen threshold must be below max hunger so it's achievable.");
        }

        // ── Player ───────────────────────────────────────────────────────

        [Test]
        public void PlayerSprintSpeed_IsGreaterThanMoveSpeed()
        {
            Assert.Greater(GameConstants.PlayerSprintSpeed, GameConstants.PlayerMoveSpeed,
                "Sprint speed must exceed normal move speed.");
        }

        [Test]
        public void PlayerReach_IsPositive()
        {
            Assert.Positive(GameConstants.PlayerReach);
        }

        // ── Block IDs ────────────────────────────────────────────────────

        [Test]
        public void BlockAir_IsZero()
        {
            Assert.AreEqual(0, GameConstants.BlockAir,
                "Air block ID must be 0 — used as the 'empty' sentinel throughout the engine.");
        }

        [Test]
        public void BlockIds_AreUnique()
        {
            var ids = new[]
            {
                GameConstants.BlockAir,
                GameConstants.BlockGrass,
                GameConstants.BlockDirt,
                GameConstants.BlockStone,
                GameConstants.BlockSand,
                GameConstants.BlockSnow,
                GameConstants.BlockWater,
                GameConstants.BlockWood,
                GameConstants.BlockLeaves,
                GameConstants.BlockMosque,
                GameConstants.BlockIce,
                GameConstants.BlockMoss,
                GameConstants.BlockWheat,
            };

            var set = new System.Collections.Generic.HashSet<byte>(ids);
            Assert.AreEqual(ids.Length, set.Count, "Every block ID must be unique.");
        }

        // ── Firestore paths ──────────────────────────────────────────────

        [Test]
        public void FirestoreCollectionNames_AreNotEmpty()
        {
            Assert.IsNotEmpty(GameConstants.FirestoreWorldSavesCollection);
            Assert.IsNotEmpty(GameConstants.FirestoreChildProfilesCollection);
        }

        [Test]
        public void FirestoreCollectionNames_ContainNoSpaces()
        {
            Assert.IsFalse(GameConstants.FirestoreWorldSavesCollection.Contains(" "));
            Assert.IsFalse(GameConstants.FirestoreChildProfilesCollection.Contains(" "));
        }

        // ── Performance ──────────────────────────────────────────────────

        [Test]
        public void WebGLMemorySize_IsAtLeast256MB()
        {
            Assert.GreaterOrEqual(GameConstants.WebGLMemorySizeMB, 256,
                "WebGL needs enough memory to run the voxel engine.");
        }
    }
}

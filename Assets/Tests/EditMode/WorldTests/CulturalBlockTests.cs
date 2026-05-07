// Assets/Tests/EditMode/WorldTests/CulturalBlockTests.cs
using NUnit.Framework;
using DeenCraft.World;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    /// <summary>
    /// Verifies Phase 6 cultural block types exist in the enum and
    /// that BlockItemMap correctly maps them to/from ItemIds.
    /// </summary>
    public class CulturalBlockTests
    {
        // ── BlockType existence ───────────────────────────────────────────────

        [Test] public void BlockType_Minaret_Exists()     => Assert.DoesNotThrow(() => { var _ = BlockType.Minaret; });
        [Test] public void BlockType_Dome_Exists()        => Assert.DoesNotThrow(() => { var _ = BlockType.Dome; });
        [Test] public void BlockType_StoneArch_Exists()   => Assert.DoesNotThrow(() => { var _ = BlockType.StoneArch; });
        [Test] public void BlockType_Crescent_Exists()    => Assert.DoesNotThrow(() => { var _ = BlockType.Crescent; });
        [Test] public void BlockType_StarBlock_Exists()   => Assert.DoesNotThrow(() => { var _ = BlockType.StarBlock; });
        [Test] public void BlockType_AppleWood_Exists()   => Assert.DoesNotThrow(() => { var _ = BlockType.AppleWood; });
        [Test] public void BlockType_AppleLeaves_Exists() => Assert.DoesNotThrow(() => { var _ = BlockType.AppleLeaves; });
        [Test] public void BlockType_WaterSlide_Exists()  => Assert.DoesNotThrow(() => { var _ = BlockType.WaterSlide; });

        // ── ItemId existence ──────────────────────────────────────────────────

        [Test] public void ItemId_Olive_Exists() => Assert.DoesNotThrow(() => { var _ = ItemId.Olive; });
        [Test] public void ItemId_Apple_Exists() => Assert.DoesNotThrow(() => { var _ = ItemId.Apple; });

        // ── BlockItemMap: block → item drop ──────────────────────────────────

        [Test] public void AppleLeaves_DropsAppleFood()
            => Assert.AreEqual(ItemId.Apple,     BlockItemMap.BlockTypeToItemId(BlockType.AppleLeaves));

        [Test] public void OliveLeaves_DropsOliveFood()
            => Assert.AreEqual(ItemId.Olive,     BlockItemMap.BlockTypeToItemId(BlockType.OliveLeaves));

        [Test] public void Minaret_DropsSelf()
            => Assert.AreEqual(ItemId.Minaret,   BlockItemMap.BlockTypeToItemId(BlockType.Minaret));

        [Test] public void Dome_DropsSelf()
            => Assert.AreEqual(ItemId.Dome,      BlockItemMap.BlockTypeToItemId(BlockType.Dome));

        [Test] public void StoneArch_DropsSelf()
            => Assert.AreEqual(ItemId.StoneArch, BlockItemMap.BlockTypeToItemId(BlockType.StoneArch));

        [Test] public void Crescent_DropsSelf()
            => Assert.AreEqual(ItemId.Crescent,  BlockItemMap.BlockTypeToItemId(BlockType.Crescent));

        [Test] public void StarBlock_DropsSelf()
            => Assert.AreEqual(ItemId.StarBlock,  BlockItemMap.BlockTypeToItemId(BlockType.StarBlock));

        // ── BlockItemMap: item → block placement ──────────────────────────────

        [Test] public void WaterSlide_IsPlaceable()
            => Assert.IsTrue(BlockItemMap.IsPlaceable(ItemId.WaterSlide));

        [Test] public void Minaret_IsPlaceable()
            => Assert.IsTrue(BlockItemMap.IsPlaceable(ItemId.Minaret));

        [Test] public void Apple_IsNotPlaceable()
            => Assert.IsFalse(BlockItemMap.IsPlaceable(ItemId.Apple));

        [Test] public void Olive_IsNotPlaceable()
            => Assert.IsFalse(BlockItemMap.IsPlaceable(ItemId.Olive));
    }
}

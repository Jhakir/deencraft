// Assets/Tests/EditMode/WorldTests/BlockColourTests.cs
using NUnit.Framework;
using UnityEngine;
using DeenCraft.World;

namespace DeenCraft.Tests.EditMode
{
    public class BlockColourTests
    {
        [Test]
        public void GetBlockColour_Air_IsFullyTransparent()
        {
            Assert.AreEqual(0, ChunkMesher.GetBlockColour(BlockType.Air).a);
        }

        [Test]
        public void GetBlockColour_SolidBlocks_AreOpaque()
        {
            Assert.AreEqual(255, ChunkMesher.GetBlockColour(BlockType.Stone).a);
            Assert.AreEqual(255, ChunkMesher.GetBlockColour(BlockType.Grass).a);
            Assert.AreEqual(255, ChunkMesher.GetBlockColour(BlockType.Dome).a);
        }

        [Test]
        public void GetBlockColour_DistinguishesBlockTypes()
        {
            Color32 grass = ChunkMesher.GetBlockColour(BlockType.Grass);
            Color32 sand  = ChunkMesher.GetBlockColour(BlockType.Sand);
            Assert.IsFalse(grass.r == sand.r && grass.g == sand.g && grass.b == sand.b);
        }

        [Test]
        public void GetBlockColour_OutOfRangeBlock_DoesNotThrow()
        {
            // Guards against a new BlockType being added without a colour entry.
            Assert.DoesNotThrow(() => ChunkMesher.GetBlockColour((BlockType)200));
        }
    }
}

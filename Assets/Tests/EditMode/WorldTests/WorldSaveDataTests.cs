using NUnit.Framework;
using DeenCraft.Auth.Models;

namespace DeenCraft.Tests.EditMode.WorldTests
{
    public class WorldSaveDataTests
    {
        [Test]
        public void IsValid_ReturnsTrue_ForWellFormedSave()
        {
            var save = new WorldSaveData("child-1", 42, "My World");
            Assert.IsTrue(save.IsValid());
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenIdEmpty()
        {
            var save = new WorldSaveData("child-1", 42, "My World");
            save.id = "";
            Assert.IsFalse(save.IsValid());
        }

        [Test]
        public void DefaultPosition_IsSeaLevel()
        {
            var save = new WorldSaveData("child-1", 42, "My World");
            Assert.AreEqual("0,64,0", save.playerPosition);
        }

        [Test]
        public void EncodeDecodePosition_RoundTrips()
        {
            float inX = 1.5f, inY = 64.0f, inZ = -3.75f;
            string encoded = WorldSaveData.EncodePosition(inX, inY, inZ);
            WorldSaveData.DecodePosition(encoded, out float x, out float y, out float z);
            Assert.AreEqual(inX, x, 0.001f);
            Assert.AreEqual(inY, y, 0.001f);
            Assert.AreEqual(inZ, z, 0.001f);
        }

        [Test]
        public void DecodePosition_DefaultString_ReturnsSeaLevel()
        {
            WorldSaveData.DecodePosition("0,64,0", out float x, out float y, out float z);
            Assert.AreEqual(0f,  x, 0.001f);
            Assert.AreEqual(64f, y, 0.001f);
            Assert.AreEqual(0f,  z, 0.001f);
        }

        [Test]
        public void DecodePosition_MalformedString_ReturnsSafeDefault()
        {
            WorldSaveData.DecodePosition("NOT_VALID", out float x, out float y, out float z);
            Assert.AreEqual(0f,  x, 0.001f);
            Assert.AreEqual(64f, y, 0.001f, "y should default to sea level");
            Assert.AreEqual(0f,  z, 0.001f);
        }

        [Test]
        public void EncodePosition_LargeCoordinates_RoundTrips()
        {
            float inX = 10000.5f, inY = 255f, inZ = -9999.99f;
            string encoded = WorldSaveData.EncodePosition(inX, inY, inZ);
            WorldSaveData.DecodePosition(encoded, out float x, out float y, out float z);
            Assert.AreEqual(inX, x, 0.01f);
            Assert.AreEqual(inY, y, 0.001f);
            Assert.AreEqual(inZ, z, 0.01f);
        }
    }
}

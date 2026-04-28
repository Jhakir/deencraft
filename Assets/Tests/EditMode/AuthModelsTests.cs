using System;
using NUnit.Framework;
using DeenCraft.Auth.Models;

namespace DeenCraft.Tests.EditMode
{
    [TestFixture]
    public class AuthModelsTests
    {
        // ── ParentAccount ────────────────────────────────────────────────

        [Test]
        public void ParentAccount_Constructor_SetsFieldsCorrectly()
        {
            var account = new ParentAccount("uid-123", "Ahmed Ali");

            Assert.AreEqual("uid-123", account.uid);
            Assert.AreEqual("Ahmed Ali", account.displayName);
            Assert.IsNotEmpty(account.createdAt);
            Assert.IsNotEmpty(account.lastLoginAt);
        }

        [Test]
        public void ParentAccount_IsValid_ReturnsTrueForValidData()
        {
            var account = new ParentAccount("uid-123", "Fatima Hassan");
            Assert.IsTrue(account.IsValid());
        }

        [Test]
        public void ParentAccount_Constructor_ThrowsOnEmptyUid()
        {
            Assert.Throws<ArgumentException>(() => new ParentAccount("", "Test User"));
        }

        [Test]
        public void ParentAccount_Constructor_ThrowsOnEmptyDisplayName()
        {
            Assert.Throws<ArgumentException>(() => new ParentAccount("uid-1", ""));
        }

        [Test]
        public void ParentAccount_Constructor_ThrowsOnWhitespaceUid()
        {
            Assert.Throws<ArgumentException>(() => new ParentAccount("   ", "Test"));
        }

        [Test]
        public void ParentAccount_DefaultConstructor_IsInvalid()
        {
            var account = new ParentAccount();
            Assert.IsFalse(account.IsValid(),
                "Default-constructed account should be invalid until fields are set.");
        }

        // ── ChildProfile ─────────────────────────────────────────────────

        [Test]
        public void ChildProfile_Constructor_SetsFieldsCorrectly()
        {
            var profile = new ChildProfile("Yusuf", 2);

            Assert.AreEqual("Yusuf", profile.username);
            Assert.AreEqual(2, profile.avatarIndex);
            Assert.IsNotEmpty(profile.id);
            Assert.IsEmpty(profile.pinHash, "No PIN should be set by default.");
            Assert.IsNotNull(profile.character);
        }

        [Test]
        public void ChildProfile_IsValid_ReturnsTrueForValidData()
        {
            var profile = new ChildProfile("Maryam", 0);
            Assert.IsTrue(profile.IsValid());
        }

        [Test]
        public void ChildProfile_Constructor_ThrowsOnEmptyUsername()
        {
            Assert.Throws<ArgumentException>(() => new ChildProfile("", 0));
        }

        [Test]
        public void ChildProfile_Constructor_ThrowsOnUsernameTooLong()
        {
            var longName = new string('a', 21);
            Assert.Throws<ArgumentException>(() => new ChildProfile(longName, 0));
        }

        [Test]
        public void ChildProfile_Constructor_ThrowsOnNegativeAvatarIndex()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChildProfile("Valid", -1));
        }

        [Test]
        public void ChildProfile_Constructor_TrimsUsernameWhitespace()
        {
            var profile = new ChildProfile("  Ibrahim  ", 1);
            Assert.AreEqual("Ibrahim", profile.username);
        }

        [Test]
        public void ChildProfile_TwoInstances_HaveDifferentIds()
        {
            var a = new ChildProfile("Aisha", 0);
            var b = new ChildProfile("Khadija", 1);
            Assert.AreNotEqual(a.id, b.id, "Each profile must have a unique ID.");
        }

        [Test]
        public void ChildProfile_UsernameMaxLength_IsExactly20()
        {
            var name20 = new string('z', 20);
            Assert.DoesNotThrow(() => new ChildProfile(name20, 0));
        }

        // ── CharacterData ────────────────────────────────────────────────

        [Test]
        public void CharacterData_DefaultConstructor_IsValid()
        {
            var character = new CharacterData();
            Assert.IsTrue(character.IsValid(),
                "Default character should be valid out of the box.");
        }

        [Test]
        public void CharacterData_DefaultSkinTone_IsInRange()
        {
            var character = new CharacterData();
            Assert.GreaterOrEqual(character.skinToneIndex, 0);
            Assert.LessOrEqual(character.skinToneIndex, 5);
        }

        [Test]
        public void CharacterData_DefaultColors_AreHexStrings()
        {
            var character = new CharacterData();
            Assert.IsTrue(character.headCoveringColor.StartsWith("#"),
                "Color should be a hex string starting with #");
            Assert.IsTrue(character.outfitPrimaryColor.StartsWith("#"));
        }

        // ── WorldSaveData ────────────────────────────────────────────────

        [Test]
        public void WorldSaveData_Constructor_SetsFieldsCorrectly()
        {
            var save = new WorldSaveData("child-id-1", 42, "My World");

            Assert.AreEqual("child-id-1", save.childProfileId);
            Assert.AreEqual(42, save.seed);
            Assert.AreEqual("My World", save.worldName);
            Assert.AreEqual(0, save.dayCount);
            Assert.IsNotEmpty(save.playerPosition);
            Assert.IsNotEmpty(save.id);
        }

        [Test]
        public void WorldSaveData_IsValid_ReturnsTrueForValidData()
        {
            var save = new WorldSaveData("child-id-1", 12345, "Test World");
            Assert.IsTrue(save.IsValid());
        }

        [Test]
        public void WorldSaveData_Constructor_ThrowsOnEmptyChildId()
        {
            Assert.Throws<ArgumentException>(() =>
                new WorldSaveData("", 1, "My World"));
        }

        [Test]
        public void WorldSaveData_Constructor_ThrowsOnEmptyWorldName()
        {
            Assert.Throws<ArgumentException>(() =>
                new WorldSaveData("child-1", 1, ""));
        }

        [Test]
        public void WorldSaveData_TwoInstances_HaveDifferentIds()
        {
            var a = new WorldSaveData("child-1", 1, "World A");
            var b = new WorldSaveData("child-1", 2, "World B");
            Assert.AreNotEqual(a.id, b.id);
        }
    }
}

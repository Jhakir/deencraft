using System;
using System.IO;
using NUnit.Framework;
using DeenCraft.Auth;
using DeenCraft.Auth.Models;

namespace DeenCraft.Tests.EditMode
{
    /// <summary>
    /// Tests for LocalFileBackend — runs synchronously in Edit Mode,
    /// uses a temp directory so it never touches real app data.
    /// </summary>
    [TestFixture]
    public class LocalFileBackendTests
    {
        private LocalFileBackend _backend;
        private string _tempRoot;

        // ── Setup / Teardown ──────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Redirect persistentDataPath to a temp dir so tests are isolated.
            _tempRoot = Path.Combine(Path.GetTempPath(), "deencraft_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);

            // LocalFileBackend reads Application.persistentDataPath.
            // We patch it via reflection on the static helper in LocalFileBackend.
            LocalFileBackend.OverrideRootForTesting(_tempRoot);
            _backend = new LocalFileBackend();
        }

        [TearDown]
        public void TearDown()
        {
            _backend.WipeAllData();
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
            LocalFileBackend.OverrideRootForTesting(null);
        }

        // ── CreateParentAsync ─────────────────────────────────────────────

        [Test]
        public void CreateParent_ValidData_ReturnsAccount()
        {
            var account = _backend.CreateParentAsync("parent@test.com", "password123", "Test Parent").Result;
            Assert.IsNotNull(account);
            Assert.AreEqual("Test Parent", account.displayName);
            Assert.IsNotEmpty(account.uid);
        }

        [Test]
        public void CreateParent_DuplicateEmail_Throws()
        {
            _backend.CreateParentAsync("dup@test.com", "password123", "Parent A").Wait();
            var ex = Assert.Throws<AggregateException>(() =>
                _backend.CreateParentAsync("dup@test.com", "password123", "Parent B").Wait());
            Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
        }

        [Test]
        public void CreateParent_ShortPassword_Throws()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                _backend.CreateParentAsync("x@test.com", "short", "Name").Wait());
            Assert.IsInstanceOf<ArgumentException>(ex.InnerException);
        }

        // ── SignInParentAsync ─────────────────────────────────────────────

        [Test]
        public void SignIn_CorrectCredentials_ReturnsAccount()
        {
            _backend.CreateParentAsync("signin@test.com", "validpass1", "Alice").Wait();
            var account = _backend.SignInParentAsync("signin@test.com", "validpass1").Result;
            Assert.IsNotNull(account);
            Assert.AreEqual("Alice", account.displayName);
        }

        [Test]
        public void SignIn_WrongPassword_Throws()
        {
            _backend.CreateParentAsync("pw@test.com", "correctpass", "Bob").Wait();
            var ex = Assert.Throws<AggregateException>(() =>
                _backend.SignInParentAsync("pw@test.com", "wrongpass").Wait());
            Assert.IsInstanceOf<UnauthorizedAccessException>(ex.InnerException);
        }

        [Test]
        public void SignIn_UnknownEmail_Throws()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                _backend.SignInParentAsync("ghost@test.com", "anypass1").Wait());
            Assert.IsInstanceOf<UnauthorizedAccessException>(ex.InnerException);
        }

        [Test]
        public void SignIn_EmailCaseInsensitive_Succeeds()
        {
            _backend.CreateParentAsync("Case@Test.COM", "validpass1", "Caser").Wait();
            var account = _backend.SignInParentAsync("case@test.com", "validpass1").Result;
            Assert.IsNotNull(account);
        }

        // ── GetCachedParentAsync ──────────────────────────────────────────

        [Test]
        public void GetCachedParent_KnownUid_ReturnsAccount()
        {
            var created = _backend.CreateParentAsync("cached@test.com", "password1!", "Cached").Result;
            var fetched = _backend.GetCachedParentAsync(created.uid).Result;
            Assert.IsNotNull(fetched);
            Assert.AreEqual(created.uid, fetched.uid);
        }

        [Test]
        public void GetCachedParent_UnknownUid_ReturnsNull()
        {
            var result = _backend.GetCachedParentAsync("nonexistent-uid").Result;
            Assert.IsNull(result);
        }

        // ── Child Profiles ────────────────────────────────────────────────

        [Test]
        public void CreateAndGetChildProfile_RoundTrip()
        {
            var parent = _backend.CreateParentAsync("child@test.com", "password1!", "Parent").Result;
            var profile = new ChildProfile("Amira", 2);

            _backend.CreateChildProfileAsync(parent.uid, profile).Wait();
            var profiles = _backend.GetChildProfilesAsync(parent.uid).Result;

            Assert.AreEqual(1, profiles.Count);
            Assert.AreEqual("Amira", profiles[0].username);
            Assert.AreEqual(2, profiles[0].avatarIndex);
        }

        [Test]
        public void UpdateChildProfile_PersistsChanges()
        {
            var parent = _backend.CreateParentAsync("upd@test.com", "password1!", "Parent").Result;
            var profile = new ChildProfile("Bilal", 0);
            _backend.CreateChildProfileAsync(parent.uid, profile).Wait();

            profile.avatarIndex = 3;
            _backend.UpdateChildProfileAsync(parent.uid, profile).Wait();

            var profiles = _backend.GetChildProfilesAsync(parent.uid).Result;
            Assert.AreEqual(3, profiles[0].avatarIndex);
        }

        [Test]
        public void DeleteChildProfile_RemovesFromList()
        {
            var parent = _backend.CreateParentAsync("del@test.com", "password1!", "Parent").Result;
            var profile = new ChildProfile("Zara", 1);
            _backend.CreateChildProfileAsync(parent.uid, profile).Wait();
            _backend.DeleteChildProfileAsync(parent.uid, profile.id).Wait();

            var profiles = _backend.GetChildProfilesAsync(parent.uid).Result;
            Assert.AreEqual(0, profiles.Count);
        }

        // ── World Saves ───────────────────────────────────────────────────

        [Test]
        public void SaveAndGetWorld_RoundTrip()
        {
            var parent = _backend.CreateParentAsync("world@test.com", "password1!", "Parent").Result;
            var child  = new ChildProfile("Omar", 0);
            _backend.CreateChildProfileAsync(parent.uid, child).Wait();

            var save = new WorldSaveData(child.id, 42, "My World");
            _backend.SaveWorldAsync(parent.uid, child.id, save).Wait();

            var loaded = _backend.GetWorldSaveAsync(parent.uid, child.id, save.id).Result;
            Assert.IsNotNull(loaded);
            Assert.AreEqual("My World", loaded.worldName);
            Assert.AreEqual(42, loaded.seed);
        }

        [Test]
        public void ListWorldSaves_ReturnsSavedEntries()
        {
            var parent = _backend.CreateParentAsync("list@test.com", "password1!", "Parent").Result;
            var child  = new ChildProfile("Yusuf", 0);
            _backend.CreateChildProfileAsync(parent.uid, child).Wait();

            _backend.SaveWorldAsync(parent.uid, child.id, new WorldSaveData(child.id, 1, "World 1")).Wait();
            _backend.SaveWorldAsync(parent.uid, child.id, new WorldSaveData(child.id, 2, "World 2")).Wait();

            var saves = _backend.ListWorldSavesAsync(parent.uid, child.id).Result;
            Assert.AreEqual(2, saves.Count);
        }

        [Test]
        public void DeleteWorldSave_RemovesEntry()
        {
            var parent = _backend.CreateParentAsync("dws@test.com", "password1!", "Parent").Result;
            var child  = new ChildProfile("Fatima", 0);
            _backend.CreateChildProfileAsync(parent.uid, child).Wait();

            var save = new WorldSaveData(child.id, 99, "Test World");
            _backend.SaveWorldAsync(parent.uid, child.id, save).Wait();
            _backend.DeleteWorldSaveAsync(parent.uid, child.id, save.id).Wait();

            var saves = _backend.ListWorldSavesAsync(parent.uid, child.id).Result;
            Assert.AreEqual(0, saves.Count);
        }

        // ── WipeAllData ───────────────────────────────────────────────────

        [Test]
        public void WipeAllData_ClearsEverything()
        {
            _backend.CreateParentAsync("wipe@test.com", "password1!", "Wipe Me").Wait();
            _backend.WipeAllData();

            // After wipe, sign-in should fail
            var ex = Assert.Throws<AggregateException>(() =>
                _backend.SignInParentAsync("wipe@test.com", "password1!").Wait());
            Assert.IsInstanceOf<UnauthorizedAccessException>(ex.InnerException);
        }
    }
}

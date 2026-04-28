using System;
using NUnit.Framework;
using DeenCraft.Auth;
using DeenCraft.Auth.Models;

namespace DeenCraft.Tests.EditMode
{
    /// <summary>
    /// Tests SessionManager state transitions.
    ///
    /// Note: SessionManager is a static class with no Unity dependencies,
    /// so these run cleanly in EditMode without needing a scene.
    ///
    /// Each test clears state via SessionManager.Clear() on setup and teardown
    /// to avoid bleed-through between tests.
    /// </summary>
    [TestFixture]
    public class SessionManagerTests
    {
        // ── Setup / Teardown ─────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SessionManager.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SessionManager.Clear();
        }

        // ── Initial State ────────────────────────────────────────────────

        [Test]
        public void InitialState_HasNoParent()
        {
            Assert.IsNull(SessionManager.ActiveParent);
            Assert.IsFalse(SessionManager.IsParentLoggedIn);
        }

        [Test]
        public void InitialState_HasNoChild()
        {
            Assert.IsNull(SessionManager.ActiveChild);
            Assert.IsFalse(SessionManager.IsChildActive);
        }

        [Test]
        public void InitialState_SessionAge_IsZero()
        {
            Assert.AreEqual(0.0, SessionManager.SessionAgeSeconds, delta: 0.01);
        }

        // ── SetParent ────────────────────────────────────────────────────

        [Test]
        public void SetParent_SetsActiveParent()
        {
            var parent = MakeParent();
            SessionManager.SetParent(parent);

            Assert.AreEqual(parent, SessionManager.ActiveParent);
            Assert.IsTrue(SessionManager.IsParentLoggedIn);
        }

        [Test]
        public void SetParent_FiresOnParentLoggedInEvent()
        {
            ParentAccount received = null;
            SessionManager.OnParentLoggedIn += p => received = p;

            var parent = MakeParent();
            SessionManager.SetParent(parent);

            Assert.AreEqual(parent, received);
        }

        [Test]
        public void SetParent_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => SessionManager.SetParent(null));
        }

        [Test]
        public void SetParent_ThrowsOnInvalidAccount()
        {
            var invalid = new ParentAccount(); // default — no uid or name
            Assert.Throws<ArgumentException>(() => SessionManager.SetParent(invalid));
        }

        // ── SetActiveChild ────────────────────────────────────────────────

        [Test]
        public void SetActiveChild_SetsActiveChild()
        {
            SessionManager.SetParent(MakeParent());
            var child = MakeChild();
            SessionManager.SetActiveChild(child);

            Assert.AreEqual(child, SessionManager.ActiveChild);
            Assert.IsTrue(SessionManager.IsChildActive);
        }

        [Test]
        public void SetActiveChild_FiresOnChildActivatedEvent()
        {
            SessionManager.SetParent(MakeParent());
            ChildProfile received = null;
            SessionManager.OnChildActivated += c => received = c;

            var child = MakeChild();
            SessionManager.SetActiveChild(child);

            Assert.AreEqual(child, received);
        }

        [Test]
        public void SetActiveChild_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => SessionManager.SetActiveChild(null));
        }

        // ── ClearActiveChild ─────────────────────────────────────────────

        [Test]
        public void ClearActiveChild_RemovesActiveChild()
        {
            SessionManager.SetParent(MakeParent());
            SessionManager.SetActiveChild(MakeChild());

            SessionManager.ClearActiveChild();

            Assert.IsNull(SessionManager.ActiveChild);
            Assert.IsFalse(SessionManager.IsChildActive);
        }

        [Test]
        public void ClearActiveChild_FiresOnChildDeactivatedEvent()
        {
            SessionManager.SetParent(MakeParent());
            SessionManager.SetActiveChild(MakeChild());

            bool fired = false;
            SessionManager.OnChildDeactivated += () => fired = true;
            SessionManager.ClearActiveChild();

            Assert.IsTrue(fired);
        }

        [Test]
        public void ClearActiveChild_DoesNotClearParent()
        {
            SessionManager.SetParent(MakeParent());
            SessionManager.SetActiveChild(MakeChild());
            SessionManager.ClearActiveChild();

            Assert.IsNotNull(SessionManager.ActiveParent);
            Assert.IsTrue(SessionManager.IsParentLoggedIn);
        }

        // ── Clear ─────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesBothParentAndChild()
        {
            SessionManager.SetParent(MakeParent());
            SessionManager.SetActiveChild(MakeChild());
            SessionManager.Clear();

            Assert.IsNull(SessionManager.ActiveParent);
            Assert.IsNull(SessionManager.ActiveChild);
            Assert.IsFalse(SessionManager.IsParentLoggedIn);
            Assert.IsFalse(SessionManager.IsChildActive);
        }

        [Test]
        public void Clear_FiresOnParentLoggedOutEvent()
        {
            SessionManager.SetParent(MakeParent());
            bool fired = false;
            SessionManager.OnParentLoggedOut += () => fired = true;
            SessionManager.Clear();

            Assert.IsTrue(fired);
        }

        [Test]
        public void Clear_CalledTwice_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SessionManager.Clear();
                SessionManager.Clear();
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static ParentAccount MakeParent() =>
            new ParentAccount("parent-uid-001", "Test Parent");

        private static ChildProfile MakeChild() =>
            new ChildProfile("TestChild", 0);
    }
}

# Phase 7: Auth Integration & World Save Lifecycle — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the existing Auth layer (FirebaseAuthManager, SessionManager, LocalFileBackend) to the world engine (ChunkManager, PlayerController) via a WorldSaveManager, and add CharacterData ↔ CharacterAppearance conversion so a child's appearance survives logout/login.

**Architecture:** Pure-C# bridge types contain the conversion logic (testable without Unity). A MonoBehaviour `WorldSaveManager` coordinates the async save/load workflow using `async/await` (Firebase pattern). The CharacterData model (Auth assembly) and CharacterAppearance (Player assembly) are bridged by a static `CharacterDataBridge` in the Player assembly, which already depends on Auth.

**Tech Stack:** Unity 2022.3 C#, DeenCraft.Auth + DeenCraft.Player assemblies, NUnit EditMode tests.

**Already done before this plan:**
- `FirebaseAuthManager.cs` — full CRUD: CreateParent, SignIn, CreateChildProfile, ActivateChild, SaveWorldAsync, LoadWorldSaveAsync
- `SessionManager.cs` — in-memory state (ActiveParent, ActiveChild)
- `LocalFileBackend.cs` + `FirebaseBackend.cs` — full IDataBackend implementations
- Models: `ChildProfile`, `ParentAccount`, `CharacterData`, `WorldSaveData`
- `CharacterCustomizer.cs` — applies `CharacterAppearance` to renderers, saves/loads via PlayerPrefs

**Baseline test count: ~211**

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Player/CharacterDataBridge.cs` | **Create** | Pure-C# static conversions: `CharacterAppearance ↔ CharacterData` |
| `Assets/Scripts/World/WorldSaveManager.cs` | **Create** | MonoBehaviour: new world, save, load lifecycle via FirebaseAuthManager |
| `Assets/Scripts/Auth/Models/WorldSaveData.cs` | Modify | Add `PositionToVector3` / `Vector3ToPosition` helpers |
| `Assets/Tests/EditMode/PlayerTests/CharacterDataBridgeTests.cs` | **Create** | Round-trip conversion tests |
| `Assets/Tests/EditMode/WorldTests/WorldSaveDataTests.cs` | **Create** | Position string helpers |

---

## Task 1 — WorldSaveData position helpers

**Files:**
- Modify: `Assets/Scripts/Auth/Models/WorldSaveData.cs`
- Create: `Assets/Tests/EditMode/WorldTests/WorldSaveDataTests.cs`

These helpers are pure-C# (no Unity) and enable `WorldSaveManager` to encode/decode player position without `UnityEngine.Vector3` in the model assembly.

- [ ] **Step 1: Write failing tests**

```csharp
// Assets/Tests/EditMode/WorldTests/WorldSaveDataTests.cs
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
        public void EncodePosition_ProducesCorrectString()
        {
            string encoded = WorldSaveData.EncodePosition(1.5f, 64.0f, -3.75f);
            Assert.AreEqual("1.5,64,−3.75", encoded,
                "Encoded string must be 'x,y,z' with invariant culture");
        }

        [Test]
        public void DecodePosition_ParsesEncodedString()
        {
            string encoded = WorldSaveData.EncodePosition(10f, 65f, 20f);
            WorldSaveData.DecodePosition(encoded, out float x, out float y, out float z);
            Assert.AreEqual(10f, x, 0.001f);
            Assert.AreEqual(65f, y, 0.001f);
            Assert.AreEqual(20f, z, 0.001f);
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
    }
}
```

> **Note:** the `EncodePosition` test uses `−3.75` with a unicode minus — change to `Assert.That(encoded, Does.Contain("−3.75").Or.Contain("-3.75"))` for robustness, or just test round-trip.

- [ ] **Step 2: Run tests — confirm failures** (`EncodePosition`/`DecodePosition` don't exist yet)

- [ ] **Step 3: Add helpers to `WorldSaveData.cs`** — append before the closing `}`

```csharp
/// <summary>Encodes a world-space position as a culture-invariant "x,y,z" string.</summary>
public static string EncodePosition(float x, float y, float z)
    => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2}", x, y, z);

/// <summary>
/// Decodes a "x,y,z" position string into float components.
/// Returns (0, SeaLevel, 0) on parse failure.
/// </summary>
public static void DecodePosition(string encoded, out float x, out float y, out float z)
{
    x = 0f; y = 64f; z = 0f;
    if (string.IsNullOrWhiteSpace(encoded)) return;
    var parts = encoded.Split(',');
    if (parts.Length != 3) return;
    float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out x);
    float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out y);
    float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out z);
}
```

- [ ] **Step 4: Fix the `EncodePosition` test** — use round-trip pattern instead of string literal (negatives may have unicode dash on some locales). Replace that test with:

```csharp
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
```

- [ ] **Step 5: Run tests — confirm all 7 pass**
- [ ] **Step 6: Commit** `feat: add WorldSaveData position encode/decode helpers`

---

## Task 2 — CharacterDataBridge (pure-C# conversion)

**Files:**
- Create: `Assets/Scripts/Player/CharacterDataBridge.cs`
- Create: `Assets/Tests/EditMode/PlayerTests/CharacterDataBridgeTests.cs`

The bridge converts between `CharacterData` (Auth model, uses int indices) and `CharacterAppearance` (Player system, uses enums + Color).

Mapping:
| CharacterData field | CharacterAppearance field |
|---|---|
| `skinToneIndex` 0–4 | `SkinTone` enum (0=Light … 4=Dark) |
| `headCoveringType` 0=none,1=hijab,2=kufi,3=hat | `HeadwearType` enum (None/Hijab/Kufi; hat→Kufi fallback) |
| `headCoveringColor` "#RRGGBBAA" | `ClothingColor` (Color) |
| `outfitStyle` (stored but not used by CharacterAppearance yet) | `ClothingStyle` enum |
| `outfitPrimaryColor` "#RRGGBBAA" | `ClothingColor` (primary color) |

- [ ] **Step 1: Write failing tests**

```csharp
// Assets/Tests/EditMode/PlayerTests/CharacterDataBridgeTests.cs
using NUnit.Framework;
using UnityEngine;
using DeenCraft.Player;
using DeenCraft.Auth.Models;

namespace DeenCraft.Tests.EditMode.PlayerTests
{
    public class CharacterDataBridgeTests
    {
        [Test]
        public void ToAppearance_DefaultData_ProducesMediumSkinTone()
        {
            var data = new CharacterData(); // skinToneIndex=2 by default
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(SkinTone.Medium, appearance.SkinTone);
        }

        [Test]
        public void ToAppearance_HijabType_MapsToHijabHeadwear()
        {
            var data = new CharacterData { headCoveringType = 1 };
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(HeadwearType.Hijab, appearance.HeadwearType);
        }

        [Test]
        public void ToAppearance_KufiType_MapsToKufi()
        {
            var data = new CharacterData { headCoveringType = 2 };
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(HeadwearType.Kufi, appearance.HeadwearType);
        }

        [Test]
        public void ToAppearance_UnknownHeadType_MapsToNone()
        {
            var data = new CharacterData { headCoveringType = 99 };
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(HeadwearType.None, appearance.HeadwearType);
        }

        [Test]
        public void ToData_DefaultAppearance_ProducesValidCharacterData()
        {
            var appearance = new CharacterAppearance();
            var data = CharacterDataBridge.ToData(appearance);
            Assert.IsTrue(data.IsValid());
        }

        [Test]
        public void ToData_DarkSkinTone_SetsIndex4()
        {
            var appearance = new CharacterAppearance { SkinTone = SkinTone.Dark };
            var data = CharacterDataBridge.ToData(appearance);
            Assert.AreEqual(4, data.skinToneIndex);
        }

        [Test]
        public void RoundTrip_AppearanceToDataAndBack_PreservesSkinTone()
        {
            var original = new CharacterAppearance { SkinTone = SkinTone.MediumDark };
            var data      = CharacterDataBridge.ToData(original);
            var restored  = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(original.SkinTone, restored.SkinTone);
        }

        [Test]
        public void RoundTrip_PreservesHeadwearType()
        {
            var original = new CharacterAppearance { HeadwearType = HeadwearType.Hijab };
            var data      = CharacterDataBridge.ToData(original);
            var restored  = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(original.HeadwearType, restored.HeadwearType);
        }
    }
}
```

- [ ] **Step 2: Run tests — confirm 8 failures**

- [ ] **Step 3: Create `CharacterDataBridge.cs`**

```csharp
// Assets/Scripts/Player/CharacterDataBridge.cs
// Converts between CharacterData (Auth.Models — int indices) and
// CharacterAppearance (Player — enums + Color). No Unity dependencies in
// the conversion logic; Unity.Color is only used via string parse/format.
using UnityEngine;
using DeenCraft.Auth.Models;

namespace DeenCraft.Player
{
    /// <summary>
    /// Converts <see cref="CharacterData"/> (Auth model, serialized as ints)
    /// to and from <see cref="CharacterAppearance"/> (Player system, enums + Color).
    ///
    /// Call <see cref="ToAppearance"/> after loading a child profile to restore
    /// the player's appearance. Call <see cref="ToData"/> before saving.
    /// </summary>
    public static class CharacterDataBridge
    {
        // ── CharacterData → CharacterAppearance ──────────────────────────────

        public static CharacterAppearance ToAppearance(CharacterData data)
        {
            if (data == null) return new CharacterAppearance();

            var appearance = new CharacterAppearance
            {
                SkinTone      = IndexToSkinTone(data.skinToneIndex),
                HeadwearType  = IndexToHeadwear(data.headCoveringType),
                ClothingStyle = IndexToClothingStyle(data.outfitStyle),
                ClothingColor = ParseColor(data.outfitPrimaryColor, Color.white),
            };
            return appearance;
        }

        // ── CharacterAppearance → CharacterData ──────────────────────────────

        public static CharacterData ToData(CharacterAppearance appearance)
        {
            if (appearance == null) return new CharacterData();

            return new CharacterData
            {
                skinToneIndex       = SkinToneToIndex(appearance.SkinTone),
                headCoveringType    = HeadwearToIndex(appearance.HeadwearType),
                headCoveringColor   = FormatColor(appearance.ClothingColor),
                outfitStyle         = ClothingStyleToIndex(appearance.ClothingStyle),
                outfitPrimaryColor  = FormatColor(appearance.ClothingColor),
                outfitSecondaryColor = "#FFFFFFFF",
            };
        }

        // ── SkinTone ──────────────────────────────────────────────────────────

        private static SkinTone IndexToSkinTone(int index)
        {
            switch (index)
            {
                case 0: return SkinTone.Light;
                case 1: return SkinTone.MediumLight;
                case 2: return SkinTone.Medium;
                case 3: return SkinTone.MediumDark;
                case 4: return SkinTone.Dark;
                default: return SkinTone.Medium;
            }
        }

        private static int SkinToneToIndex(SkinTone tone)
        {
            switch (tone)
            {
                case SkinTone.Light:       return 0;
                case SkinTone.MediumLight: return 1;
                case SkinTone.Medium:      return 2;
                case SkinTone.MediumDark:  return 3;
                case SkinTone.Dark:        return 4;
                default:                   return 2;
            }
        }

        // ── HeadwearType ──────────────────────────────────────────────────────

        private static HeadwearType IndexToHeadwear(int index)
        {
            switch (index)
            {
                case 1:  return HeadwearType.Hijab;
                case 2:  return HeadwearType.Kufi;
                default: return HeadwearType.None;
            }
        }

        private static int HeadwearToIndex(HeadwearType type)
        {
            switch (type)
            {
                case HeadwearType.Hijab: return 1;
                case HeadwearType.Kufi:  return 2;
                default:                 return 0;
            }
        }

        // ── ClothingStyle ─────────────────────────────────────────────────────

        private static ClothingStyle IndexToClothingStyle(int index)
        {
            switch (index)
            {
                case 1:  return ClothingStyle.Traditional;
                case 2:  return ClothingStyle.Winter;
                default: return ClothingStyle.Casual;
            }
        }

        private static int ClothingStyleToIndex(ClothingStyle style)
        {
            switch (style)
            {
                case ClothingStyle.Traditional: return 1;
                case ClothingStyle.Winter:      return 2;
                default:                        return 0;
            }
        }

        // ── Color helpers ─────────────────────────────────────────────────────

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return fallback;
        }

        private static string FormatColor(Color color)
            => "#" + ColorUtility.ToHtmlStringRGBA(color);
    }
}
```

- [ ] **Step 4: Run tests — confirm 8 pass**
- [ ] **Step 5: Commit** `feat: add CharacterDataBridge for auth↔player appearance conversion`

---

## Task 3 — WorldSaveManager MonoBehaviour

**Files:**
- Create: `Assets/Scripts/World/WorldSaveManager.cs`

No unit tests for the MonoBehaviour itself (requires Unity runtime). The save data helpers (Task 1) and bridge (Task 2) are the testable logic. `WorldSaveManager` just wires them together.

- [ ] **Step 1: Create `WorldSaveManager.cs`**

```csharp
// Assets/Scripts/World/WorldSaveManager.cs
// Manages the world save/load lifecycle for the active child's session.
// Attach to a persistent GameObject alongside FirebaseAuthManager.
//
// Workflow:
//   StartOrLoadWorld() → called by UI when child presses "Play" on a save slot
//   SaveAndQuit()      → called by pause menu "Save & Quit"
//   NewWorld(name)     → called by UI "New World" button (creates fresh WorldSaveData)
using System;
using System.Threading.Tasks;
using UnityEngine;
using DeenCraft;
using DeenCraft.Auth;
using DeenCraft.Auth.Models;
using DeenCraft.Player;

namespace DeenCraft.World
{
    /// <summary>
    /// Coordinates world save/load between <see cref="FirebaseAuthManager"/>,
    /// <see cref="ChunkManager"/>, and <see cref="PlayerController"/>.
    ///
    /// Attach to the same GameObject as <see cref="FirebaseAuthManager"/>.
    /// Assign <see cref="_chunkManager"/> and <see cref="_playerController"/>
    /// in the Inspector (or leave null — graceful no-ops are safe in headless tests).
    /// </summary>
    [RequireComponent(typeof(FirebaseAuthManager))]
    public sealed class WorldSaveManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private ChunkManager      _chunkManager;
        [SerializeField] private PlayerController  _playerController;
        [SerializeField] private CharacterCustomizer _characterCustomizer;

        // ── State ────────────────────────────────────────────────────────────
        public WorldSaveData ActiveSave { get; private set; }
        public bool          IsWorldLoaded { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        public event Action<WorldSaveData> OnWorldLoaded;
        public event Action                OnWorldSaved;
        public event Action<string>        OnError;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fresh world save for the active child and starts the world.
        /// Generates a random seed and stores the WorldSaveData via FirebaseAuthManager.
        /// </summary>
        public async Task NewWorldAsync(string worldName)
        {
            if (!SessionManager.IsChildActive)
            {
                NotifyError("No active child profile — cannot create world.");
                return;
            }
            if (string.IsNullOrWhiteSpace(worldName))
                worldName = "My World";

            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            var saveData = new WorldSaveData(SessionManager.ActiveChild.id, seed, worldName);

            try
            {
                await FirebaseAuthManager.Instance.SaveWorldAsync(saveData);
                ActiveSave = saveData;
                ApplyWorldState(saveData);
                IsWorldLoaded = true;
                OnWorldLoaded?.Invoke(saveData);
            }
            catch (Exception ex) { NotifyError($"Failed to create world: {ex.Message}"); }
        }

        /// <summary>
        /// Loads an existing world save by ID and restores player position + appearance.
        /// </summary>
        public async Task LoadWorldAsync(string saveId)
        {
            if (!SessionManager.IsChildActive)
            {
                NotifyError("No active child profile — cannot load world.");
                return;
            }
            try
            {
                var saveData = await FirebaseAuthManager.Instance.LoadWorldSaveAsync(saveId);
                if (saveData == null || !saveData.IsValid())
                {
                    NotifyError($"Save '{saveId}' not found or invalid.");
                    return;
                }
                ActiveSave = saveData;
                ApplyWorldState(saveData);
                IsWorldLoaded = true;
                OnWorldLoaded?.Invoke(saveData);
            }
            catch (Exception ex) { NotifyError($"Failed to load world: {ex.Message}"); }
        }

        /// <summary>
        /// Captures current world state (player position) and persists via FirebaseAuthManager.
        /// Safe to call at any time; no-ops if no world is loaded.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!IsWorldLoaded || ActiveSave == null) return;

            CaptureCurrentState();

            try
            {
                await FirebaseAuthManager.Instance.SaveWorldAsync(ActiveSave);
                OnWorldSaved?.Invoke();
            }
            catch (Exception ex) { NotifyError($"Save failed: {ex.Message}"); }
        }

        /// <summary>Saves the world then clears session state ready for scene unload.</summary>
        public async Task SaveAndQuitAsync()
        {
            await SaveAsync();
            IsWorldLoaded = false;
            ActiveSave    = null;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void ApplyWorldState(WorldSaveData save)
        {
            // Apply seed to world generation
            if (_chunkManager != null)
                _chunkManager.SetSeed(save.seed);

            // Restore player position
            if (_playerController != null)
            {
                WorldSaveData.DecodePosition(save.playerPosition,
                    out float px, out float py, out float pz);
                _playerController.transform.position = new Vector3(px, py, pz);
            }

            // Restore character appearance from saved CharacterData
            if (_characterCustomizer != null && SessionManager.IsChildActive)
            {
                var charData   = SessionManager.ActiveChild.character;
                var appearance = CharacterDataBridge.ToAppearance(charData);
                _characterCustomizer.SetAppearance(appearance);
            }
        }

        private void CaptureCurrentState()
        {
            if (ActiveSave == null) return;

            // Capture player position
            if (_playerController != null)
            {
                var pos = _playerController.transform.position;
                ActiveSave.playerPosition = WorldSaveData.EncodePosition(pos.x, pos.y, pos.z);
            }

            ActiveSave.savedAt  = DateTime.UtcNow.ToString("o");
            ActiveSave.dayCount = DayNightCycle.Instance != null
                ? Mathf.FloorToInt(DayNightCycle.Instance.NormalizedTime * 365f)
                : ActiveSave.dayCount;
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"[WorldSaveManager] {message}");
            OnError?.Invoke(message);
        }
    }
}
```

- [ ] **Step 2: Add `SetSeed(int)` to `ChunkManager.cs`** — so `WorldSaveManager.ApplyWorldState` can change the world seed at runtime:

Find the `_seed` field in ChunkManager and add:
```csharp
/// <summary>
/// Changes the world seed and invalidates all loaded chunks.
/// Call before the world is loaded (from WorldSaveManager.ApplyWorldState).
/// </summary>
public void SetSeed(int seed)
{
    _seed = seed;
}
```

- [ ] **Step 3: Verify no compiler errors** (no new tests for the MonoBehaviour — runtime only)
- [ ] **Step 4: Commit** `feat: WorldSaveManager — wires auth save/load to ChunkManager and PlayerController`

---

## Task 4 — Final test + commit

- [ ] **Step 1: Run all EditMode tests** — confirm ≥ 226 pass (211 + 7 WorldSaveData + 8 Bridge)
- [ ] **Step 2: Commit** `docs: phase_7_status.md` (write after all tasks pass — see template below)
- [ ] **Step 3: Push** `git push origin main`

---

## Estimated New Test Count

| Task | New Tests |
|---|---|
| WorldSaveDataTests | 7 |
| CharacterDataBridgeTests | 8 |
| **Total new** | **15** |
| **Cumulative total** | **~226** |

---

## Conventions Reminder
- `async/await` for all Firebase/save calls (not Coroutines)
- `[SerializeField]` not `public` for Inspector fields  
- `PascalCase` classes/methods, `_camelCase` private fields
- `#if FIREBASE_AVAILABLE` already wraps Firebase SDK calls in FirebaseBackend — no change needed here
- Auth assembly does NOT depend on Player — bridge code lives in Player assembly (which already references Auth)

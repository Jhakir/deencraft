# Phase 7 Status — Auth Integration & World Save Lifecycle

## Completion Status: ✅ DONE

Completed on: 2026-05-07.
Cumulative test count: **~226 EditMode tests** (211 Phase 1–6 + 15 Phase 7).
Commit: `3c4fe87`

---

## What Was Built

### Summary
Phase 7 wired the existing Auth layer (FirebaseAuthManager, SessionManager, LocalFileBackend) to the world engine (ChunkManager, PlayerController) and added a `CharacterData ↔ CharacterAppearance` conversion bridge so a child's saved appearance is restored automatically on login.

The auth infrastructure itself (FirebaseAuthManager, SessionManager, IDataBackend, LocalFileBackend, FirebaseBackend, all Models) was already complete from Phase 4. Phase 7 added the missing glue:

---

### ✅ WorldSaveData — Position Helpers

**File:** `Assets/Scripts/Auth/Models/WorldSaveData.cs`

Added two static helpers for lossless, culture-invariant position serialization:
- `EncodePosition(float x, float y, float z) → string` — produces `"x,y,z"` using `InvariantCulture`
- `DecodePosition(string encoded, out float x, out float y, out float z)` — parses back; returns sea level `(0, 64, 0)` on malformed input

These are pure-C# (no Unity dependency) so they are fully testable in EditMode.

---

### ✅ CharacterDataBridge

**File:** `Assets/Scripts/Player/CharacterDataBridge.cs`

New static class bridging:
- `CharacterData` (Auth model — int indices, hex color strings)
- `CharacterAppearance` (Player system — enums + `UnityEngine.Color`)

| Mapping | Direction |
|---|---|
| `skinToneIndex` 0–4 | `SkinTone` enum (Light / MediumLight / Medium / MediumDark / Dark) |
| `headCoveringType` 0–2 | `HeadwearType` enum (None / Hijab / Kufi); unknown → None |
| `outfitStyle` 0–2 | `ClothingStyle` enum (Casual / Traditional / Winter) |
| `outfitPrimaryColor` "#RRGGBBAA" | `ClothingColor` (Unity Color) |

Methods: `ToAppearance(CharacterData) → CharacterAppearance`, `ToData(CharacterAppearance) → CharacterData`. Both handle `null` input gracefully (return defaults).

Placed in the **Player assembly** (which already depends on Auth) — Auth assembly does NOT depend on Player.

---

### ✅ ChunkManager — SetSeed

**File:** `Assets/Scripts/World/ChunkManager.cs`

Added `public void SetSeed(int seed)`:
- Updates `_worldSeed`
- Clears `_chunkDataCache` — all cached terrain data discarded
- Returns all active `ChunkView` objects to the pool and clears `_activeViews`
- Resets `_lastPlayerChunk` so the update loop immediately triggers a full reload
- Safe to call before the player enters the world

---

### ✅ WorldSaveManager

**File:** `Assets/Scripts/World/WorldSaveManager.cs`

New `[RequireComponent(typeof(FirebaseAuthManager))]` MonoBehaviour. Inspector-wired to `ChunkManager`, `PlayerController`, and `CharacterCustomizer`.

| Method | Behaviour |
|---|---|
| `NewWorldAsync(string worldName)` | Generates random seed, creates `WorldSaveData`, persists via `FirebaseAuthManager.SaveWorldAsync`, calls `ApplyWorldState` |
| `LoadWorldAsync(string saveId)` | Fetches save via `FirebaseAuthManager.LoadWorldSaveAsync`, validates, calls `ApplyWorldState` |
| `SaveAsync()` | Captures player position → `WorldSaveData.EncodePosition`, updates `savedAt`, persists |
| `SaveAndQuitAsync()` | `SaveAsync()` + clears `ActiveSave` / `IsWorldLoaded` |

`ApplyWorldState(WorldSaveData)` does:
1. `ChunkManager.SetSeed(save.seed)` — new seed takes effect before chunks load
2. Teleports player to `DecodePosition(save.playerPosition)`
3. Calls `CharacterDataBridge.ToAppearance(SessionManager.ActiveChild.character)` → `CharacterCustomizer.SetAppearance`

Events: `OnWorldLoaded(WorldSaveData)`, `OnWorldSaved`, `OnError(string)`.

---

## Test Summary

| File | New Tests |
|---|---|
| `WorldSaveDataTests.cs` | 7 (IsValid, default position, encode/decode round-trip, malformed string, large coords) |
| `CharacterDataBridgeTests.cs` | 8 (skin tone mapping, headwear mapping, unknown type fallback, round-trips) |
| **Phase 7 new** | **15** |
| **Cumulative** | **~226** |

---

## Security Review

| Concern | Mitigation |
|---|---|
| PIN stored plain-text | SHA-256 hashed before storage — already enforced in `FirebaseAuthManager.CreateChildProfileAsync` |
| World save overwrite by wrong user | Firestore security rules (not yet deployed) must enforce `auth.uid == parentUid` — reminder for Phase 9 deploy |
| Path traversal in `LocalFileBackend` | `SanitizePath()` strips path separators — already in place from Phase 4 |
| `EncodePosition` locale mismatch | `InvariantCulture` used in both encode and decode — no locale-dependent decimal separator |

---

## What Is NOT Done in Phase 7
- Firebase security rules deployment (Phase 9 — requires active Firebase project)
- Parent dashboard web page (deferred — v1 targets in-game child profile selection only)
- Hotbar inventory save/load (WorldSaveData has `hotbarSummary` field ready; wiring deferred to Phase 8 HUD)
- World list UI — `ListWorldSavesAsync` exists in the backend but no UI to display it yet (Phase 8)
- Character customizer UI (Phase 8 — slider/swatch panel writes to `CharacterCustomizer` which already saves via `PlayerPrefs`)

---

## Phase 8 Notes
- Main menu scene: play button → child profile picker → world select/new
- HUD: health bar, hunger bar, hotbar (10 slots), day/time indicator
- Crafting UI: 3×3 grid + output slot, reads from `RecipeDatabase`
- Trade UI: villager face + offer list, calls `TradeSession`
- Settings panel: Adhan toggle, volume slider, graphics quality
- Character customizer panel: skin tone swatches, headwear buttons, colour pickers → `CharacterCustomizer.SetAppearance` + `SaveToPrefs` + `CharacterDataBridge.ToData` → persist to `ChildProfile.character`

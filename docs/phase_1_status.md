# Phase 1 Status — Project Setup

**Status:** ✅ Complete  
**Date completed:** 2026-04-28  
**Unity version:** 2022.3.62f3 LTS (upgraded from 2022.3.42f1 — security alert on original target)

---

## What Was Done

### Task 1 — Unity Project Foundation
- Created all 12 `ProjectSettings/` files: `ProjectVersion.txt`, `ProjectSettings.asset` (WebGL target, `com.deencraft.game` bundle ID, 512 MB WebGL memory, Brotli compression), `EditorSettings.asset` (force text serialization for git diffability), `TagManager.asset`, `InputManager.asset`, `TimeManager.asset`, `GraphicsSettings.asset`, `QualitySettings.asset` (Low/Med/High tiers), `AudioManager.asset`, `DynamicsManager.asset` (gravity –20 for snappier feel), `NavMeshAreas.asset`, `PresetManager.asset`.
- Created `Packages/manifest.json` with Firebase scoped registry pre-wired (Auth + Firestore), Cinemachine 2.9.7, Unity Input System 1.7.0, TextMeshPro 3.0.6, and all Unity built-in modules.
- Updated `.gitignore` with comprehensive Unity + Firebase exclusions. Confirmed `Assets/StreamingAssets/firebase-config.json` is ignored (secrets never hit git).

### Task 2 — Folder Structure + Assembly Definitions
- Created all script subdirectories with `.gitkeep` placeholders: `World`, `Player`, `Animals`, `Villager`, `Auth/Models`, `UI`, `Crafting`, `Editor`, `Prefabs`, `Art`, `Audio`, `Scenes`, `StreamingAssets`, `Tests/EditMode`, `Tests/PlayMode`.
- Created `Assets/Scripts/GameConstants.cs` — single source of truth for all magic numbers: chunk dimensions (16×16×256), hotbar/backpack slot counts, block IDs 0–12, Firebase collection name constants, PlayerPrefs keys, WebGL memory size.
- Created 8 assembly definition (`.asmdef`) files: `DeenCraft.Core`, `DeenCraft.Auth`, `DeenCraft.World`, `DeenCraft.Player`, `DeenCraft.Animals`, `DeenCraft.Villager`, `DeenCraft.UI`, `DeenCraft.Crafting`, plus `DeenCraft.Tests.EditMode` and `DeenCraft.Tests.PlayMode`.

### Task 3 — Auth Manager + Session Manager + Config Template
- Created 4 data models in `Assets/Scripts/Auth/Models/`:
  - `ParentAccount.cs` — uid, displayName, createdAt, lastLoginAt; `IsValid()` check
  - `ChildProfile.cs` — id, username, avatarIndex, pinHash (SHA-256), character, timestamps; constructor validates + trims
  - `CharacterData.cs` — skinToneIndex (0–5), headCoveringType (0–3), clothing colours as hex strings, outfitStyle (0–3)
  - `WorldSaveData.cs` — id, childProfileId, seed, player position/rotation, savedAt, worldName, dayCount, hotbarSummary
- Created `Assets/Scripts/Auth/FirebaseAuthManager.cs` — MonoBehaviour singleton, pluggable backend via `IDataBackend` interface, `_useLocalBackend` Inspector toggle.
- Created `Assets/Scripts/Auth/SessionManager.cs` — static in-memory session state, events: `OnParentLoggedIn`, `OnParentLoggedOut`, `OnChildActivated`, `OnChildDeactivated`.
- Created `Assets/StreamingAssets/firebase-config.template.json` — placeholder config file (real file gitignored).

### Task 3 (extended) — Local Dev Backend
The plan assumed Firebase would be available immediately. Since the Firebase package requires a live Firebase project and Unity CDN requires a browser session to download the SDK, a pluggable backend pattern was introduced:
- `IDataBackend.cs` — interface abstracting all persistence (auth, child profiles, world saves, session restore).
- `LocalFileBackend.cs` — full implementation using JSON files in `Application.persistentDataPath/deencraft-local/`. Passwords and PINs hashed with SHA-256. Path traversal prevented via `SanitizePath()`. Includes `OverrideRootForTesting()` for Edit Mode test isolation and `WipeAllData()` for Editor tooling.
- `FirebaseBackend.cs` — production implementation with all Firebase SDK calls behind `#if FIREBASE_AVAILABLE`. Compiles cleanly without the SDK installed.

### Task 4 — Editor Build Scripts
- `WebGLBuildConfig.cs` — applies all WebGL PlayerSettings, exposes `Build WebGL (Dev)` and `Build WebGL (Release)` menu items, plus `BuildFromCommandLine()` static entry point for CI.
- `FirebaseConfigValidator.cs` — `[InitializeOnLoad]` validator, checks `firebase-config.json` exists and contains 6 required keys, detects placeholder values, warns clearly in the Console.
- `DeenCraftMenuItems.cs` — `Deencraft/` top-bar menu: Build, Firebase validation, Local Dev (Open Data Folder, Wipe All Local Data), Settings, About.
- `FirestoreRulesTemplate.cs` — generates `firestore.rules` and `firestore.indexes.json` from a template.

### Task 5 — EditMode Tests
- `GameConstantsTests.cs` — 17 tests covering chunk dimensions, block IDs, slot counts, Firebase collection names.
- `AuthModelsTests.cs` — 22 tests covering model construction, validation, serialization, PIN hashing.
- `SessionManagerTests.cs` — 16 tests covering session state, event firing, active child switching.
- `LocalFileBackendTests.cs` — 14 tests covering the full local backend lifecycle: create/sign-in parent, duplicate email rejection, wrong password, case-insensitive email, session restore, child profile CRUD, world save CRUD, wipe.

**Total: 69 EditMode tests.**

---

## Errors Encountered

### 1. Unity CDN requires browser login
- **Symptom:** `curl` on the Unity Hub CDN URL returned a 381-byte HTML redirect (not the `.dmg`). The URL `https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.dmg` resolves only when a valid Unity browser session cookie is present.
- **Resolution:** Manual download. Go to [unity.com/download](https://unity.com/download) in a browser, sign in, download Unity Hub manually.

### 2. Unity version 2022.3.42f1 has a Security Alert
- **Symptom:** Unity Hub flagged the target version (2022.3.42f1) with a red "Security Alert" badge — it is no longer the latest LTS patch.
- **Resolution:** Updated `ProjectSettings/ProjectVersion.txt` to `2022.3.62f3` (latest 2022 LTS as of 2026-04-28).

### 3. `CS0103: GameConstants does not exist` compile errors
- **Symptom:** Unity reported `GameConstants` not found in `SessionManager.cs` and `FirebaseAuthManager.cs` (8 errors total).
- **Root cause:** All `.asmdef` files had empty `"references": []`. `DeenCraft.Auth` (and every other assembly) could not see `DeenCraft.Core` where `GameConstants` lives. Also, two reference entries used `"GUID:AssemblyName"` syntax instead of bare assembly names, which Unity does not resolve by name alone.
- **Resolution:** Added `"DeenCraft.Core"` to the `references` array of all 7 game assemblies. Normalised all reference entries to bare assembly names (no `GUID:` prefix).

### 4. Firebase package manager network error
- **Symptom:** Unity Package Manager logs `Cannot connect to 'unitypackage.firebase.google.com' (ENOTFOUND)` on every editor launch.
- **Root cause:** `Packages/manifest.json` declares the Firebase scoped registry, so Unity tries to reach it on startup even when the package hasn't been imported yet.
- **Impact:** Cosmetic only — the game compiles fine because all Firebase SDK calls are behind `#if FIREBASE_AVAILABLE`. The warning can be dismissed.
- **Resolution for production:** When ready to add Firebase, import the SDK from [firebase.google.com/docs/unity/setup](https://firebase.google.com/docs/unity/setup), then this warning disappears naturally. Alternatively, remove the scoped registry from `manifest.json` until then.

### 5. Terminal heredoc / Python inline-script issues
- **Symptom:** Shell heredoc (`<< 'EOF'`) and `python3 << 'EOF'` caused the terminal to enter continuation mode, printing `>...` instead of executing.
- **Root cause:** The VS Code agent terminal does not reliably handle multi-line heredocs in a zsh session that has already run other commands.
- **Resolution:** Wrote files using `python3 -c "..."` with a single-quoted string, or used the `create_file` / `replace_string_in_file` agent tools directly instead of shell redirection.

---

## Verification Results (all passing)

| Check | Required | Actual |
|---|---|---|
| `ProjectSettings/` file count | ≥ 12 | **20** |
| `Packages/manifest.json` valid JSON | parse OK | **OK** |
| Assembly definitions | ≥ 7 | **8** (+ 2 test asmdefs) |
| `firebase-config.json` gitignored | ignored | **confirmed** |
| C# scripts total | ≥ 15 | **18** |
| Test files | ≥ 3 | **4** |
| `.gitignore` covers firebase secret | match | **confirmed** |
| All files have `namespace DeenCraft.*` | all | **all** |
| No bare `public` fields | none | **none** |
| All 18 key files present | 18/18 | **18/18** |

---

## What to Keep in Mind for Phase 2

### Architecture decisions that Phase 2 must respect

1. **Chunk size is locked at 16×16×256.** `GameConstants.ChunkWidth = 16`, `ChunkHeight = 256`, `ChunkDepth = 16`. Changing these requires updating every system that touches chunk coordinates. Do not touch these values.

2. **Coordinate system:** World space is Unity default (Y-up). Chunk coordinates are in chunk units (not world units). When converting: `worldPos = chunkCoord * ChunkWidth`.

3. **Assembly boundaries:** `World` scripts go in `Assets/Scripts/World/` (assembly `DeenCraft.World`). `Player` scripts go in `Assets/Scripts/Player/` (assembly `DeenCraft.Player`). `DeenCraft.Player` already references `DeenCraft.World`. Any new cross-assembly dependency must be added explicitly to the relevant `.asmdef`.

4. **No `public` fields.** All inspector-exposed values use `[SerializeField] private`. This is enforced in code review.

5. **No magic numbers.** All constants go in `GameConstants.cs`. Add new constants there — never inline `16`, `256`, etc. in chunk or world code.

### Firebase / backend

6. **`_useLocalBackend = true` in the Inspector** for all local development. `FirebaseBackend` will throw `NotSupportedException` if called without the SDK. Do not add Firebase SDK until Phase 7.

7. **Session state lives in `SessionManager`.** Phase 2 world scripts must not try to read player identity or save data directly — they call `SessionManager.ActiveChild` and `FirebaseAuthManager.Instance.SaveWorldAsync()`.

### Performance (WebGL targets)

8. **Greedy meshing is required** — the plan specifies it explicitly. Do not build a naive face-per-voxel mesh for any chunk. Target ≥ 30 FPS in browser.

9. **Chunk pooling is required** — never instantiate/destroy chunk GameObjects. Reuse from a pool. `ChunkManager` should own the pool.

10. **Coroutines for game-side async**, `async/await` only for Firebase calls. Chunk generation should be spread over frames using `IEnumerator` coroutines, not `Task`.

### Known pending items (not blocking Phase 2)

- Firebase project not yet created. Needed for Phase 7, not now.
- `FirestoreRulesTemplate.cs` generates rules but no `firestore.rules` file has been deployed.
- PlayMode test assembly (`DeenCraft.Tests.PlayMode`) exists but has no tests yet. Phase 2 should add at minimum a chunk generation smoke test.
- Git author identity is auto-configured as `ruqayyah@mac.lan` — consider running `git config --global user.email` and `git config --global user.name` to set a proper identity before Phase 2 commits.

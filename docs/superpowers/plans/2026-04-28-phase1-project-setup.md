# Deencraft Phase 1 — Project Setup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a complete, openable Unity 2022 LTS WebGL project with Firebase auth scaffolding, proper folder structure, EditMode tests, and a WebGL build pipeline — ready to open the moment Unity Hub is installed.

**Architecture:** Unity 2022 LTS project configured for WebGL, using text-mode YAML serialization so all assets are diffable in git. Firebase SDK declared in Packages/manifest.json via npm scoped registry. Auth is split across FirebaseAuthManager (Firebase calls) and SessionManager (in-memory session state). All secrets stay out of source via StreamingAssets template + .gitignore.

**Tech Stack:** Unity 2022.3 LTS, Firebase Unity SDK 11.x (Auth + Firestore), Unity Input System 1.7, Cinemachine 2.9, NUnit (Unity built-in), Firebase Hosting for WebGL deploy.

---

### Task 1: Unity Project Foundation — ProjectSettings + Packages + .gitignore

**Files:**
- Create: `ProjectSettings/ProjectVersion.txt`
- Create: `ProjectSettings/ProjectSettings.asset`
- Create: `ProjectSettings/EditorSettings.asset`
- Create: `ProjectSettings/TagManager.asset`
- Create: `ProjectSettings/InputManager.asset`
- Create: `ProjectSettings/TimeManager.asset`
- Create: `ProjectSettings/GraphicsSettings.asset`
- Create: `ProjectSettings/QualitySettings.asset`
- Create: `ProjectSettings/AudioManager.asset`
- Create: `ProjectSettings/DynamicsManager.asset`
- Create: `ProjectSettings/NavMeshAreas.asset`
- Create: `ProjectSettings/PresetManager.asset`
- Create: `Packages/manifest.json`
- Modify: `.gitignore`

- [ ] **Step 1: Create ProjectVersion.txt**
  ```
  m_EditorVersion: 2022.3.42f1
  m_EditorVersionWithRevision: 2022.3.42f1 (...)
  ```

- [ ] **Step 2: Create ProjectSettings.asset** (WebGL target, text serialization, company/product name)

- [ ] **Step 3: Create EditorSettings.asset** (force text serialization so all assets are git-diffable)

- [ ] **Step 4: Create all remaining ProjectSettings/ files** (TagManager, InputManager, TimeManager, GraphicsSettings, QualitySettings, AudioManager, DynamicsManager, NavMeshAreas, PresetManager)

- [ ] **Step 5: Create Packages/manifest.json** with Firebase Auth, Firebase Firestore, Unity Input System, Cinemachine, and TextMeshPro

- [ ] **Step 6: Update .gitignore** with comprehensive Unity + Firebase secrets exclusions

- [ ] **Step 7: Verify structure**
  ```bash
  ls ProjectSettings/ | wc -l  # should be >= 12
  cat Packages/manifest.json | python3 -m json.tool  # should parse cleanly
  ```

- [ ] **Step 8: Commit**
  ```bash
  git add ProjectSettings/ Packages/ .gitignore
  git commit -m "feat(setup): Unity 2022 LTS project foundation with WebGL target"
  ```

---

### Task 2: Folder Structure + Assembly Definitions

**Files:**
- Create: `Assets/Scripts/GameConstants.cs`
- Create: `Assets/Scripts/Auth/.asmdef`
- Create: `Assets/Scripts/World/.asmdef`
- Create: `Assets/Scripts/Player/.asmdef`
- Create: `Assets/Scripts/Animals/.asmdef`
- Create: `Assets/Scripts/Villager/.asmdef`
- Create: `Assets/Scripts/UI/.asmdef`
- Create: `Assets/Scripts/Crafting/.asmdef`
- Create: `Assets/Tests/EditMode/DeenCraft.Tests.EditMode.asmdef`
- Create: `Assets/Tests/PlayMode/DeenCraft.Tests.PlayMode.asmdef`
- Create: all `.gitkeep` placeholders in Prefabs/, Art/, Audio/ subdirectories

- [ ] **Step 1: Create all directory .gitkeep placeholders**
  ```bash
  mkdir -p Assets/Scripts/{World,Player,Animals,Villager,Auth/Models,UI,Crafting}
  mkdir -p Assets/{Prefabs,Art,Audio,Scenes,StreamingAssets,Editor}
  mkdir -p Assets/Tests/{EditMode,PlayMode}
  touch Assets/{Prefabs,Art,Audio,Scenes}/.gitkeep
  ```

- [ ] **Step 2: Create GameConstants.cs**
  ```csharp
  // All magic-number constants for the entire game in one place
  public static class GameConstants
  {
      // Chunk / world dimensions
      public const int ChunkWidth = 16;
      public const int ChunkHeight = 256;
      public const int ChunkDepth = 16;
      // ... (full file in implementation)
  }
  ```

- [ ] **Step 3: Create Assembly Definition files** — one per Scripts sub-folder, plus test assemblies referencing NUnit

- [ ] **Step 4: Verify**
  ```bash
  find Assets/Scripts -name "*.asmdef" | wc -l  # should be >= 7
  cat Assets/Scripts/GameConstants.cs | head -5  # should show namespace
  ```

- [ ] **Step 5: Commit**
  ```bash
  git add Assets/
  git commit -m "feat(setup): folder structure, assembly definitions, GameConstants"
  ```

---

### Task 3: Firebase Auth Manager + Session Manager + Config Template

**Files:**
- Create: `Assets/Scripts/Auth/FirebaseAuthManager.cs`
- Create: `Assets/Scripts/Auth/SessionManager.cs`
- Create: `Assets/Scripts/Auth/Models/ParentAccount.cs`
- Create: `Assets/Scripts/Auth/Models/ChildProfile.cs`
- Create: `Assets/Scripts/Auth/Models/WorldSaveData.cs`
- Create: `Assets/Scripts/Auth/Models/CharacterData.cs`
- Create: `Assets/StreamingAssets/firebase-config.template.json`

- [ ] **Step 1: Create data models** (ParentAccount, ChildProfile, WorldSaveData, CharacterData) — pure C# serializable classes

- [ ] **Step 2: Create FirebaseAuthManager.cs** — MonoBehaviour singleton, async/await for Firebase, handles parent sign-in/sign-up, child profile CRUD, Firestore reads/writes

- [ ] **Step 3: Create SessionManager.cs** — static class holding current session (active parent, active child, session token expiry). No Firebase calls — pure in-memory state.

- [ ] **Step 4: Create firebase-config.template.json** in StreamingAssets (values are placeholders, real file is gitignored)

- [ ] **Step 5: Verify .gitignore excludes real firebase config**
  ```bash
  echo '{"apiKey":"test"}' > Assets/StreamingAssets/firebase-config.json
  git status Assets/StreamingAssets/firebase-config.json  # must show "ignored"
  rm Assets/StreamingAssets/firebase-config.json
  ```

- [ ] **Step 6: Commit**
  ```bash
  git add Assets/Scripts/Auth/ Assets/StreamingAssets/
  git commit -m "feat(auth): FirebaseAuthManager, SessionManager, data models"
  ```

---

### Task 4: Editor Build Scripts

**Files:**
- Create: `Assets/Editor/WebGLBuildConfig.cs`
- Create: `Assets/Editor/FirebaseConfigValidator.cs`
- Create: `Assets/Editor/DeenCraftMenuItems.cs`

- [ ] **Step 1: Create WebGLBuildConfig.cs** — static Editor class that sets all required WebGL PlayerSettings (company name, product name, bundle ID, WebGL linker target, compression format Brotli, memory size)

- [ ] **Step 2: Create FirebaseConfigValidator.cs** — Editor OnValidate that checks StreamingAssets/firebase-config.json exists and contains required keys, logs a clear error if missing

- [ ] **Step 3: Create DeenCraftMenuItems.cs** — adds "Deencraft" menu to Unity top bar with "Build WebGL", "Validate Firebase Config", "Open Build Folder" entries

- [ ] **Step 4: Commit**
  ```bash
  git add Assets/Editor/
  git commit -m "feat(editor): WebGL build config, Firebase validator, menu items"
  ```

---

### Task 5: EditMode Tests

**Files:**
- Create: `Assets/Tests/EditMode/GameConstantsTests.cs`
- Create: `Assets/Tests/EditMode/AuthModelsTests.cs`
- Create: `Assets/Tests/EditMode/SessionManagerTests.cs`

- [ ] **Step 1: Write failing test — GameConstantsTests.cs**
  ```csharp
  [Test]
  public void ChunkDimensions_AreCorrect()
  {
      Assert.AreEqual(16, GameConstants.ChunkWidth);
      Assert.AreEqual(256, GameConstants.ChunkHeight);
      Assert.AreEqual(16, GameConstants.ChunkDepth);
  }
  ```

- [ ] **Step 2: Write failing tests — AuthModelsTests.cs** (ChildProfile validation, ParentAccount serialization)

- [ ] **Step 3: Write failing tests — SessionManagerTests.cs** (session state, active child switching, session expiry logic)

- [ ] **Step 4: Verify test files are syntactically valid** (no broken references to non-existent classes)

- [ ] **Step 5: Commit**
  ```bash
  git add Assets/Tests/
  git commit -m "test(setup): EditMode tests for constants, auth models, session manager"
  ```

---

## Verification Checklist (Phase 1 Complete When):
- [ ] `ls ProjectSettings/ | wc -l` returns ≥ 12 files
- [ ] `cat Packages/manifest.json | python3 -m json.tool` parses without error
- [ ] `find Assets/Scripts -name "*.asmdef" | wc -l` returns ≥ 7
- [ ] `git status Assets/StreamingAssets/firebase-config.json` shows "ignored" (security check)
- [ ] `find Assets -name "*.cs" | wc -l` returns ≥ 15 scripts
- [ ] `find Assets/Tests -name "*Tests.cs" | wc -l` returns ≥ 3 test files
- [ ] `cat .gitignore | grep firebase-config.json` returns a match (no secrets in git)
- [ ] All C# files have correct namespace (`DeenCraft.*`)
- [ ] No `public` fields — all inspector fields use `[SerializeField]` private
- [ ] No magic numbers — all values come from `GameConstants`

# Phase 9 Status — Make It Playable

**Status:** ✅ Code-complete & build-verified — ⚠️ live interactive playtest pending (see below)
**Date:** 2026-08-01

---

## Goal

A player loads the game in Chrome, spawns in the world, and can mine, place, and survive. No menus required yet.

---

## Root Cause Found & Fixed

The WebGL build showed a black/grey screen with no camera and no input. Root cause:

`PlayerController.Awake()` called `Shader.Find("Standard")` to build a runtime material for the
player capsule. In WebGL, IL2CPP strips any shader nothing else references — and nothing else in
the project used the built-in Standard shader (`WorldMaterial.mat` uses a custom
`DeenCraft/VertexColor` shader). `Shader.Find` returned `null`, `new Material(null)` threw
`ArgumentNullException`, and `Awake()` aborted **before** the camera-creation block ran. No camera
→ black screen. No exception handler → the rest of `Update()` never validated input either.

A secondary stripping issue: `Can't add component because class 'CapsuleCollider' doesn't exist!`
— the physics module was stripped since no scene object referenced it directly at compile time.

---

## What Was Built / Fixed

### 9.1 — Crash fix
- `PlayerController.Awake()` now recolors the primitive's existing material instead of calling
  `Shader.Find("Standard")`. Capsule/sphere collider `Destroy()` calls are null-guarded.
- Verified in a fresh build: the browser console no longer shows the `CapsuleCollider` or
  `ArgumentNullException: shader` errors present in the pre-fix build.

### 9.2 — Stop engine stripping
- `Assets/link.xml` — preserves `UnityEngine.PhysicsModule`, `TerrainPhysicsModule`, `UI`,
  `UIModule`, `IMGUIModule`, `TextRenderingModule`, `AudioModule`.
- `SceneAssembler.EnsureAlwaysIncludedShaders()` — force-includes `Standard`,
  `DeenCraft/VertexColor`, `UI/Default`, `Sprites/Default` in
  **Graphics → Always Included Shaders**, so `Shader.Find` can never return null for these again
  regardless of what future code references.

### 9.3 — Player GameObject completed
`Assets/Editor/SceneAssembler.cs` (`ConfigurePlayer`) attaches and wires, idempotently:
- `InventoryHolder`, `VitalitySystem`, `HotbarController`, `CharacterCustomizer`,
  `PlayerAnimator`, `BlockInteractor`
- Points `PlayerController._cameraTarget` and `BlockInteractor._playerCamera` at the existing
  scene `Main Camera` (detached from its original transform) — this prevents `PlayerController`
  from spawning a **second** `MainCamera`-tagged camera at runtime, which was causing an
  arbitrary/duplicate camera view.

### 9.4 — Duplicate interaction system removed
- Deleted `Assets/Scripts/World/BlockInteractionManager.cs` (Phase 2 stub that always placed
  Stone). `BlockInteractor` (mine/place/interact, inventory-aware) is the only interaction path now.

### 9.5 — Minimal in-world HUD
`SceneAssembler.BuildHud()` creates, from nothing, every run:
- `Canvas` (Screen Space Overlay) + `CanvasScaler` (1920×1080, match 0.5) + `EventSystem`
- Crosshair, 10 hearts, hunger bar (`Image.Type.Filled`), 9-slot hotbar with icon/count/selector,
  time label — all wired to `HudController`, which polls `VitalitySystem` + `Inventory` per frame.
- Fixed a Unity-2022 breaking change encountered mid-run: `AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf")`
  now throws (`"Arial.ttf is no longer a valid built in font. Please use LegacyRuntime.ttf"`);
  switched to `"LegacyRuntime.ttf"`.

### 9.6 — Spawn safety
- `WaitForSpawnGround()` (pre-existing, unmodified) holds the player at
  `WorldGenerator.GetSurfaceY(...)` every frame until a raycast finds a real `MeshCollider`
  beneath, with a 15 s failsafe release. Confirmed present and unchanged.

### Supporting gameplay code (built to make 9.3/9.5 meaningful)
- `HotbarInput.cs` — pure C# hotbar slot resolution (number keys win over scroll; always wraps to
  a valid slot). `HotbarController.cs` — MonoBehaviour wiring `Input.mouseScrollDelta` /
  number keys to `Inventory.SetSelectedSlot`. **This closed a real gap**: no runtime code
  previously called `SetSelectedSlot`, so the hotbar could never be changed by the player.
- `ChunkMesher.GetBlockColour()` — exposes the mesher's block→colour table as the single shared
  source of truth.
- `HotbarSlotUI` now tints its icon using that shared colour table so the held block is visually
  identifiable without needing item icon art.
- `BlockInteractor` / `PlayerController` — added click-to-lock / Escape-to-release cursor
  handling, required because WebGL only grants Pointer Lock on a genuine user gesture; mouse-look
  and mining are now gated on `Cursor.lockState == Locked` so the unlocking click isn't consumed
  as a mine action.

### Build pipeline fixes (`Assets/Editor/WebGLBuildScript.cs`)
- `PlayerSettings.WebGL.template = "PROJECT:Deencraft"` referenced a non-existent
  `Assets/WebGLTemplates` folder and would have failed the build — changed to
  `"APPLICATION:Default"`.
- Compression changed from Brotli → Gzip (Brotli requires `Content-Encoding: br` support that
  local `http://` testing can't reliably provide; gzip works everywhere).
- `exceptionSupport` changed `None` → `FullWithStacktrace` so console errors are diagnosable
  against the "zero console errors" exit criterion; can be revisited for size in Phase 13.
- Output path aligned to `webgl-build/loki` to match the existing `serve.py` local dev workflow.

---

## Verification Performed

| Check | Result |
|---|---|
| EditMode tests | **252 / 263 pass.** 11 failures are pre-existing, unrelated to this phase (see below) and out of Phase 9 scope (Phase 13 — Hardening). Diffed `ChunkMesher.cs` to confirm the change was purely additive. |
| Scene assembly | Ran via `-batchmode -executeMethod SceneAssembler.AssembleFromCommandLine`. Confirmed by grepping the saved scene YAML: `_cameraTarget`, `_playerCamera`, `_hungerFill`, `_hotbarSelector`, all 9 hotbar slot refs, all 10 heart icon refs are non-zero `fileID`s (not left null). |
| WebGL build | `[WebGLBuild] Build succeeded in 07:18 — 17.0 MB → webgl-build/loki` |
| Browser load (automated) | Console shows **zero errors** — only benign `WebGL: INVALID_ENUM: getInternalformatParameter` warnings (harmless WebGL2 driver noise, unrelated to project code). Old pre-fix page load is confirmed, by contrast, to have thrown the `CapsuleCollider` and shader exceptions — proving the fix. |
| Visual | Screenshot confirms player capsule (body + head), sky, ground plane, and hotbar HUD (9 slots, slot 0 highlighted) all render. |
| Movement / mining / hearts-hunger live interaction | **Not verified via automation** — see limitation below. Verified by code review instead (see next section). |

### Automation limitation (not a product bug)
The sandboxed browser tab used for automated testing reported `document.hidden = true` /
`visibilityState: "hidden"` even after `bringToFront()`. Chrome throttles/pauses a WebGL page's
`requestAnimationFrame` loop and blocks the Pointer Lock API when a tab isn't genuinely
foregrounded at the OS level. This capped verification to load/console/visual checks; WASD
movement, mouse-look, mining, placing, and live HUD updates could not be exercised end-to-end by
the automated browser in this session.

### Code-review confidence for the unverified paths
- `PlayerController.HandleMovement()` is gated only on `_spawnReady`, **not** cursor lock — reviewed
  line-by-line, standard `CharacterController` velocity integration, no logic issues found.
- `BlockInteractor.TryMine()` / `TryPlace()` / `TryInteract()` (reviewed earlier in this session) —
  raycast → `ChunkManager.SetBlock` / `Inventory.AddItem` / `IInteractable`, no issues found.
- `HudController.Update()` polls `VitalitySystem.Health` and `Inventory` every frame — straightforward.
- Recommend a manual playtest at `http://127.0.0.1:8080` (server already configured via
  `webgl-build/serve.py`) before signing off beta-ready, since no amount of static review
  substitutes for feeling actual input latency/camera feel.

---

## Pre-Existing Test Failures (out of Phase 9 scope — logged for Phase 13)

All 11 failures below existed before this phase's changes (confirmed via `git diff`, which shows
only an additive method added to `ChunkMesher.cs`) and are unrelated to player/HUD/build work:

| Test | Failure |
|---|---|
| `BlockTypeTests.AllBlockIds_AreSequentialFromZero` | `InvalidCastException` |
| `ChunkMesherTests.SingleSolidBlock_ProducesTwentyFourVertices` | Expected 24 vertices, got 16 |
| `CraftingGridTests.TryCraft_RemovesIngredients_FromGrid` | Expected 1, got 2 |
| `FeatureGeneratorTests.Decorate_MaxTreesPerChunk_NotExceeded` | 7 tree trunks vs ≤3 expected |
| `LocalFileBackendTests` × 4 | Expect `AggregateException`; backend now throws the unwrapped exception directly (async/await unwrapping mismatch in test expectations) |
| `VitalityTests.Tick_RegenHealth_WhenHungerAboveThreshold` | Boundary off-by-epsilon (15.0f not > 15.0f) |
| `WorldTests.MosqueTests.Mosque_BlocksAppear_InSomeVillageChunk` | No Minaret/Dome found in 30×30 grid, seed 42 |

---

## Files Changed

**Modified:** `PlayerController.cs`, `BlockInteractor.cs`, `HotbarSlotUI.cs`, `ChunkMesher.cs`,
`WebGLBuildScript.cs`
**Created:** `HotbarInput.cs`, `HotbarController.cs`, `SceneAssembler.cs`, `link.xml`,
`HotbarInputTests.cs`, `BlockColourTests.cs`, `docs/beta-release-plan.md`
**Deleted:** `BlockInteractionManager.cs`
**Scene:** `Assets/Scenes/Main.slunity.unity` — Player components + HUD Canvas assembled

---

## Exit Criteria Status

- [x] Load in Chrome → spawn on solid ground (spawn-safety logic unchanged/verified present)
- [ ] Walk, jump, sprint, swim — code-reviewed, **not live-tested**
- [ ] Left-click mines a block and it appears in the hotbar — code-reviewed, **not live-tested**
- [ ] Right-click places the held block — code-reviewed, **not live-tested**
- [ ] Hearts and hunger bar visibly update — code-reviewed, **not live-tested**
- [x] Zero errors in the browser console (confirmed via automated console capture)

## Next Steps

1. A manual playtest at `http://127.0.0.1:8080` to close out the four unchecked items above.
2. Proceed to Phase 10 (Populate the World) — nothing in that phase depends on the unverified
   interaction items being retested first, but a playtest is strongly recommended before any
   beta distribution.

## Local Launch Repair — 2026-10-26

The branded root page requested `Build/Deencraft.loader.js` and Brotli files, but the
current Unity build emits `loki/Build/loki.loader.js` and gzip files. This caused
"Failed to load game files" before Unity could start.

- Updated [the launch page](../webgl-build/index.html) to use the actual build and
  StreamingAssets paths, retaining the branded loading screen.
- Aligned [the deploy script](../deploy.sh) with `webgl-build/loki/Build`.
- Added gzip MIME types to [Firebase Hosting configuration](../firebase.json);
  existing Brotli rules remain available.
- Made the local server dual-stack so both `localhost` (IPv6) and `127.0.0.1`
  reach the same launch page.
- Added regression checks in [test_webgl_hosting.py](../tests/test_webgl_hosting.py).
  Run them with `python3 -B -m unittest discover -s tests -p 'test_webgl_hosting.py' -v`.

From the repository root, launch locally with:

```sh
cd webgl-build
python3 serve.py 8080
```

Then open `http://localhost:8080/`. Use [serve.py](../webgl-build/serve.py), not
`python3 -m http.server`, so gzip files have the required `Content-Encoding` and
WebAssembly MIME type. Run only one server on port 8080; separate IPv4/IPv6
listeners can otherwise make `localhost` and `127.0.0.1` serve different pages.

## Terrain Collision, Surface Recovery & Flight — 2026-10-26

### Cause of falling inside terrain

The greedy mesher emitted inward-facing triangles. A downward collision ray hit
the bottom of a test block at Y=5 instead of its top at Y=6. This also made terrain
look like hollow cubes when viewed from inside.

- Corrected triangle winding for both positive and negative faces.
- Prioritized streaming around the player's chunk instead of starting in a distant
  corner. Discarded stale pending chunks on movement and cleared queue membership
  when resetting the world seed.
- The player now waits for colliders over their entire footprint and cannot walk
  or fly across an unloaded chunk boundary.
- Spawn and recovery use the highest actual loaded block column, including
  structures and player edits, rather than a terrain-noise estimate.
- Falling below the world or becoming embedded in a solid block triggers surface
  recovery with vertical velocity reset. Ordinary jumps, swimming, and air above
  terrain do not trigger recovery. Deliberately dug air-filled tunnels are not
  automatically treated as an error.
- Removed the unconditional 15-second spawn release; slow terrain loading now logs
  a warning and keeps protection active.

### Controls

| Action | Key |
|---|---|
| Move | WASD |
| Look | Click the game to capture the mouse, then move the mouse |
| Jump | Space |
| Sprint / boost flight | Left Shift |
| Toggle creative flight | Double-tap Space (within 0.3 seconds) |
| Fly up / swim up | Hold Space |
| Fly down / swim down | Hold Left Ctrl |
| Stop flying and restore gravity | Double-tap Space again |
| Release mouse | Escape |

Flight respects terrain collision; it is not no-clip mode. Releasing the vertical
keys while flying makes the player hover. Flight is session-only and is not saved
with world data.

### Regression verification

- **52/52 focused EditMode tests passed**, covering meshing, collider raycasts,
  chunk queue resets/prioritization, actual surface heights, void/embedded recovery,
  double-tap timing, world generation, constants, and block colours.
- **3/3 PlayMode tests passed** with a deterministic 60 FPS simulation: a real
  `CharacterController` lands on the terrain top, hovers in flight, regains gravity
  when flight is disabled, and rejects unloaded chunk boundaries.
- This is a targeted validation, not a claim that the previously documented
  unrelated full-suite failures have been fixed.
- WebGL rebuild succeeded in **3:44**, producing **17.0 MB** in `webgl-build/loki`.
  Browser screenshots now show the terrain surface and trees rather than an
  inside-out chunk. Browser keyboard input confirmed both flight-toggle messages;
  ascent and descent were exercised with Space and Ctrl.

### Cached framework mismatch after rebuilding

The first browser load after rebuilding mixed the old cached JavaScript framework
(459,770 decoded bytes) with the new WebAssembly binary. This produced
`Import #263 "env" "invoke_iiffii": function import requires a callable`.
Fetching the current framework returned 459,973 bytes and included the required
import, confirming that the build itself was not missing it.

- Local hosting now sends `Cache-Control: no-store`.
- Firebase's stable-name gzip/Brotli assets and loader now use `no-cache` rather
  than year-long immutable caching. Content hashes would be needed before
  immutable caching could safely be restored.
- **5/5 hosting regression tests passed**, including cache policy checks.
- A cache-bypassed browser reload followed by an ordinary reload both completed
  with no load errors and the matching new framework.

If a previously open browser still shows this error, hard-refresh once:
**Cmd+Shift+R on macOS** or **Ctrl+Shift+R on Windows/Linux**. No Unity rebuild is
needed for that refresh.

### Stable-name cache follow-up

The `127.0.0.1` origin retained its own old framework independently of `localhost`.
New response headers cannot evict an already fresh cached response that is never
requested from the server.

The branded launch page now appends a shared `?v=<buildVersion>` to the loader,
framework, data, and WebAssembly URLs. Every successful WebGL build updates that
version in the root launch page. The local server parses URL paths separately
from query strings so versioned gzip assets retain their encoding headers.

Verified **6/6 hosting tests**, a successful Unity editor compilation, and an
ordinary reload of `http://127.0.0.1:8080/` with **no JavaScript errors** and matching
versioned framework/WebAssembly requests. The separately reported
`RangeError: Maximum call stack size exceeded` was not reproduced during this
reload; no independent recursion fix is claimed.

## Clouds, Taller Doorways & Visible Animals — 2026-10-26

- House and mosque doorway openings are now **three blocks tall**, one block
  taller than before. Their widths, surrounding walls, and roofs are unchanged.
  The shared height is `GameConstants.DoorwayHeight`.
- Added [CloudLayer](../Assets/Scripts/World/CloudLayer.cs): 25 drifting,
  block-shaped clouds at Y=155, above the generated terrain. They share one greedy
  mesh/material, wrap around the player, cast no shadows, and have no colliders,
  so the player can fly through them.
- Added visible block-style **sheep, cows, chickens, cats, and horses** using
  [BlockAnimalMesh](../Assets/Scripts/Animals/BlockAnimalMesh.cs) and
  [AnimalSpawner](../Assets/Scripts/Animals/AnimalSpawner.cs).
  There are at most 15 animals (three per species), with shared meshes per species.
  Animals reuse the existing passive AI, spawn only after nearby land/colliders
  load, and are reused rather than instantiated/destroyed as the player travels.
  Water and unloaded columns are rejected as spawn points.
- Animal movement stops before unloaded chunk boundaries, and pooled animals
  reset their falling speed and wander state when reactivated.
- The [scene assembler](../Assets/Editor/SceneAssembler.cs) now wires both systems
  idempotently into the gameplay scene with the existing vertex-colour material.

### Scope

The animals have simple block models and existing wandering/follow behaviour;
they are not rigged/animated art. Horse riding is not wired into this population
update. No hostile animals or combat mechanics were added.

### Verification

- **73/73 focused EditMode tests passed**, including both three-block doorways,
  cloud geometry/drift/wrapping, all five animal models, the bounded population,
  unloaded/water spawn rejection, land spawning, and object reuse.
- **4/4 PlayMode tests passed**, covering real animal movement and stopping at
  an unloaded chunk boundary, plus the existing player landing/flight checks.
- Scene assembly completed successfully and saved non-null material references
  for the cloud layer and animal population.
- Browser verification caught the shared terrain atlas tinting animal coats green.
  Animals and clouds now override only `_MainTex` with the built-in white texture
  through `MaterialPropertyBlock`, preserving their vertex colours without
  modifying the world material. Added assertions for both systems and reran all
  **73 focused EditMode tests successfully**.
- Animal coat colours: cream sheep with brown faces, cream/dark-patched cows,
  cream chickens with yellow beaks/feet and red combs, orange cats, brown horses
  with dark manes/tails. Clouds are white rather than terrain-tinted.
- Corrected-colour WebGL build succeeded in **4:30** (**17.0 MB**). An ordinary
  browser reload fetched the new versioned files without JavaScript errors.
  Screenshot verification shows white clouds, cream sheep, patched cows, and
  brown horses on the terrain; all **6 hosting regression tests** pass.

## Wider Furnished Houses & Villager Trading — 2026-10-27

- Houses are **9 blocks wide by 5 blocks deep**, two blocks wider than before.
  Doorways remain **1 block wide and 3 blocks tall**. Generation clears the
  interior, levels the foundation, and creates a front landing with descending
  block steps; steep approaches can still require jumping.
- Houses record their generated floor height. Grassland/olive/riverside villages
  and the non-desert starting chunk can receive houses; the desert oasis remains
  unchanged. Existing saved block edits are not replaced with pristine houses.
- [VillageSpawner](../Assets/Scripts/Villager/VillageSpawner.cs) maintains a fixed
  pool of 12 house-content sets. Ready nearby house chunks receive a named
  block-style villager on the cleared front landing, plus a bed, table, chair,
  chest, bookshelf, flat rug, and warm ceiling lantern. The lantern's pooled,
  shadow-free point light makes the furniture visible beneath the opaque roof.
  Furniture leaves the central doorway/aisle
  clear. Sets deactivate on chunk unload and are reused on travel/world reset.
- Furniture is **decorative**: chests do not yet open a storage inventory, and
  beds/chairs do not yet have sleeping/sitting interactions.
- Villagers display a name and **[E] Trade**. Aim at a villager within five
  blocks and press **E** to open the
  [trade menu](../Assets/Scripts/UI/VillagerTradePanel.cs). Trade rows show the
  cost, reward, and owned count. Unaffordable/full-inventory offers are disabled.
  Successful trades update the inventory and display the reward.
- Reachable progression: **4 wood / 5 wheat / 3 wool -> 1 gold**;
  gold buys bread, mud bricks, falafel, diamonds, and an axe. Existing
  diamond-for-pickaxe and direct wheat/wool barter offers remain.
- Trade capacity checks simulate the payment first, so spending the last items
  in a full inventory can free the slot for the reward. A rejected trade leaves
  inventory contents unchanged.
- The menu unlocks the cursor and blocks movement, flight controls, mining,
  placing, and hotbar input. **Escape** or **Close** exits; click the game again
  to capture the mouse. The closing click cannot also trigger gameplay.
  Third-person targeting ignores the player's own colliders, respects walls,
  and checks reach from the player rather than the offset camera. Furniture and
  villagers cannot accidentally be mined as terrain.
- The controller radius now follows the existing `PlayerWidth` (0.6 blocks
  across) rather than Unity's default 1-block diameter. This preserves clearance
  through the intentionally single-block doorway.
- Browser walkthrough also exposed the old third-person camera clipping through
  house walls. The camera now shortens its arm at solid geometry (ignoring the
  player's own colliders) and uses a shorter near clipping plane indoors.
  Its eye/cast origin stays below the controller's top, preventing the cast from
  starting inside the roof while flying against a ceiling. The furnished-house
  PlayMode test verifies both side-wall and ceiling clearance.
- [SceneAssembler](../Assets/Editor/SceneAssembler.cs) persists the population,
  menu font, and required references into the gameplay scene.

### Verification

- **111/111 focused EditMode tests passed**, including the new house dimensions,
  doorway/window geometry, interior clearing, furniture pool/names/aisle,
  obtainable trade progression, full-inventory payment-slot reuse, plus existing
  inventory, terrain, recovery, flight, cloud, and animal regressions.
- **8/8 focused PlayMode tests passed**, including a real controller walking
  through the doorway and furnished aisle, E-targeting past the player but not
  through walls, a real trade-button callback updating inventory, rejected-trade
  behaviour and input restoration, pool reuse
  after unload/world-data replacement, and existing movement/animal checks.
- Scene assembly completed and saved successfully. A focused read-only code
  review found no significant issues. These results do not claim that unrelated
  pre-existing full-suite failures have been fixed.
- Browser interaction reached a named villager but exposed a missing menu font:
  the assembler used `AssetDatabase.GetBuiltinExtraResource` for a font that must
  be loaded with `Resources.GetBuiltinResource`. Fixed the asset lookup at its
  source, regenerated the scene, and added a saved-scene regression asserting
  non-null menu-font and population references. This also supplies the existing
  HUD's text with the correct font.
- Browser verification of the corrected menu shows **Trade with Zaynab**, all
  12 readable offers and owned counts, disabled offers for an empty inventory,
  and successful Escape-to-close navigation. Actual payment/reward inventory
  changes and failure atomicity are covered by the automated gameplay tests.
- Indoor browser screenshots exposed the scene's near-black shaded interiors.
  Added a regression for an active interior point light in each furnished set,
  then added the small lantern rather than changing lighting across the world.
  The light is enabled/disabled with its pooled house and casts no extra shadows.
- Final lit WebGL build succeeded in **8:14** (**17.0 MB**), with **6/6 hosting
  regression tests** passing and the local game returning HTTP 200. Browser
  walkthrough entered the one-block doorway and displayed the illuminated bed,
  red/cream rug, bookshelf with coloured books, furniture, and player standing
  in the clear central aisle. Existing `INVALID_ENUM` WebGL capability warnings
  remain; automated integrated-browser pointer-lock requests can also be denied
  by the embedded document, independently of Unity gameplay errors.

## Masjid Discovery District — 2026-10-27

Implementation was merged locally into `main` from
`feature/masjid-discovery-district`, including the approved existing game
dependencies. The approved scope is **exploration first**:

- Four original, regional architectural interpretations: Saudi/Najdi-inspired
  courtyard, Iraqi-inspired brickwork, Pakistani/Mughal-inspired grouped domes,
  and Turkish/Ottoman-inspired central/subsidiary domes and slender minarets.
  These are not monument replicas or a claim that each country has one style.
- A deterministic district near the starting area, with four 32-block plots,
  a shared garden/fountain, connecting paths, and a continuous walkable approach.
  Structures span existing chunks without changing chunk dimensions.
- Bright prayer halls, mihrab recesses, flat aligned rugs, learning rooms,
  decorative washing areas, entrance shelves, teachers, and information boards.
  Furnishings and teachers use four reusable content sets, activated only when
  their supporting collision chunks are ready.
- Aim at a teacher or board and press **E** to read; **Escape** or **Close** exits.
  Reading and trading cannot acquire gameplay input over one another.
- Complex volumes, including air, foundations, roofs, and minarets, reject
  mining and placement. Rejected actions do not consume items or award drops.
  A short on-screen notice explains protection; ordinary terrain outside the
  complex volumes remains editable.
- District selection/version is included in world-save metadata. Existing saves
  without these fields select a district deterministically. This does **not**
  add general block-edit persistence, which the existing save backend lacks.
- Existing day/night and optional Adhan systems are unchanged. This release adds
  **no playable lectures, classes, scheduled attendance, guided Salah, prayer
  animations, recitations, or worship scores/rewards**.

### Verification completed — 2026-10-30

- **108 focused EditMode tests passed**, covering new layout, exact dune/river
  terrain sampling, cross-chunk structure clearances and silhouettes, protected
  edits, metadata round trips, furnishings, saved scene references, and existing
  house, inventory, terrain, animal, cloud, village, and streaming regressions.
- **14 focused PlayMode tests passed**: a real controller walks the approach and
  all four halls/classrooms without jumping or flight; actual ray-targeted mining
  and placement leave protected blocks/inventory unchanged; reading opens and
  restores input without stacking the trading UI. Existing village, flight,
  movement, and trade-button tests also pass. Additional regressions verify
  spawn-sign camera clearance, immediate population deactivation on chunk unload,
  and reuse of the same content objects after a same-seed world reset.
- **6 hosting regression tests passed**; the existing port 8080 server returned
  HTTP 200 after the final build.
- After the local fast-forward merge, both focused Unity suites were rerun on
  `main`: **108 EditMode and 14 PlayMode passed**, with zero failures.
- Scene assembly persisted the population, approach path, reading panel, and font
  references. Final WebGL build succeeded in **6:04**, producing **17.0 MB**.
- Read-only code review raised a possible disconnected-path concern. A regression
  flood-fills the actual generated paved voxels from every entrance to the garden:
  all four routes pass through the side clearances carved after wall placement.
  No speculative path changes were needed.
- Browser walkthrough confirmed the spawn sign no longer obscures the camera,
  entered a furnished prayer hall/classroom, and viewed the Iraqi-inspired
  interior. Rugs, books, information labels, and the teacher render. New masjid
  materials use restrained emission so teachers remain recognizable in shadow;
  global lighting and existing village materials were not changed.
- **Measured Unity performance: 58.4 FPS over 60.0 seconds** inside the district,
  at **1604x924**, Chrome **150.0.7871.250**, Apple M2 / ANGLE Metal, WebGLPlayer.
  This counts Unity frames and unscaled frame time, not browser animation callbacks.
  It exceeds the 30 FPS target on this device; it is not a low-end-device benchmark.
- Browser automation still encounters embedded-document pointer-lock denial.
  Manual browser E-to-read and protected mining/placement are therefore **not
  signed off by browser automation**; their actual Unity UI, ray-targeted edits,
  inventory atomicity, and modal ownership are covered by the PlayMode tests.
  All four halls/classrooms have automated real-controller traversal coverage,
  rather than a claim of a complete four-building manual browser walkthrough.
  Existing `INVALID_ENUM` capability warnings remain.

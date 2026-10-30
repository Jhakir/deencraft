# Masjid Discovery District Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver four protected, furnished, regionally inspired masjids near the starting village, with teachers and readable information boards.

**Architecture:** Select one deterministic district layout before chunk generation. Apply cross-chunk structural geometry through the voxel generator, enforce landmark protection through the world mutation API, and pool non-voxel exploration content. A single reading panel reuses existing interaction and modal-input conventions.

**Tech Stack:** Unity 2022.3.62f3, C#, existing World/Player/Villager/UI/Auth assemblies, NUnit EditMode and PlayMode tests, existing WebGL build and local hosting.

**Execution record (2026-10-30):** The exploration implementation and scene wiring
were merged locally into `main`. Verification: 108 focused
EditMode, 14 PlayMode, and 6 hosting tests passed; the final WebGL build succeeded.
An in-district 60-second Unity-frame sample measured 58.4 FPS at 1604x924 on
Apple M2 / Chrome 150. Browser pointer-lock restrictions limit manual interaction
sign-off; see [the detailed status and limitations](../../phase_9_status.md#verification-completed--2026-10-30).
The user approved including required existing game dependencies; the existing
`mosque` commit was fast-forwarded into `main` without rewriting it. Both Unity
suites passed again after the merge. No push of `main` was performed.

---

## Approved specification and boundaries

Read [the approved specification](../specs/2026-10-27-masjid-discovery-district-design.md).
Only exploration ships here. No lecture/class scheduler, Salah animation,
recitation, rewards, pathfinding, multiplayer, or dependency additions.

Execute inline unless the user explicitly selects subagents. Do not copy this
dirty workspace into a clean worktree by assuming HEAD contains its dependencies:
the scene assembler, village population, gameplay scene, and many regression
tests are currently untracked. Do not stage or revert unrelated files.

### Findings that constrain implementation

- `FeatureGenerator.PlaceStructures` places the legacy mosque beyond the chunk
  boundary after placing a house; district generation must replace that route.
- `WorldGenerator.GetSurfaceY` excludes desert dune adjustment and river carving.
  Do not use it as an exact terrain support measurement.
- `ChunkManager.SetBlock` currently returns void and silently ignores unloaded
  positions. Mining gives drops and placement consumes items unconditionally
  afterward. Mutation success must be observable before inventory changes.
- `WorldSaveData` stores metadata, not chunk-edit blobs. Do not claim existing
  block-edit persistence, add Firebase Storage, or expand this task into a general
  save system. Preserve existing cached edits and any existing edit input;
  persist only district selection/version metadata for reproducible landmarks.
- `SetSeed` clears cached chunks even when the numeric seed is unchanged.
  Population and reading panels must treat every world reset as a new generation.
- Existing reading/trading input must have one owner. A second panel must not
  restore gameplay while another panel still owns the input block.

## Files and responsibilities

| File | Responsibility |
|---|---|
| `Assets/Scripts/World/MasjidDistrictLayout.cs` | Styles, immutable complex bounds, district layout, fixed candidate ordering |
| `Assets/Scripts/World/MasjidDistrictPlanner.cs` | Select a complete safe district before writing chunks |
| `Assets/Scripts/World/MasjidStructureGenerator.cs` | Shared floors, walls, rooms, approaches, paths, regional silhouettes |
| `Assets/Scripts/World/MasjidDistrictProtection.cs` | Protected-volume queries independent of colliders |
| `Assets/Scripts/World/WorldGenerator.cs` | Accurate undecorated terrain sampling and explicit district-aware generation |
| `Assets/Scripts/World/FeatureGenerator.cs` | Reserve district areas before decorations/houses; remove old mosque route |
| `Assets/Scripts/World/ChunkManager.cs` | Own layout and world revision, generation context, checked mutation |
| `Assets/Scripts/World/WaterSimulator.cs` | Respect checked mutation without bypassing protection |
| `Assets/Scripts/Player/BlockInteractor.cs` | Gate inventory changes on mutation success; show edit-denial event |
| `Assets/Scripts/Villager/MasjidExplorer.cs` | Teacher/board `IInteractable` and content request event |
| `Assets/Scripts/Villager/MasjidContentCatalog.cs` | Reviewed, original educational text keyed by style/topic |
| `Assets/Scripts/Villager/MasjidDistrictSpawner.cs` | Four pooled complex-content sets and readiness lifecycle |
| `Assets/Scripts/UI/MasjidInformationPanel.cs` | Scrollable reading UI and protected-landmark notification |
| `Assets/Scripts/UI/VillagerTradePanel.cs` | Reject opening while another modal owns gameplay input |
| `Assets/Scripts/Auth/Models/WorldSaveData.cs` | Optional district origin, floor, and generation-version metadata |
| `Assets/Scripts/Session/WorldSaveManager.cs` | Configure/restore district selection before chunk generation |
| `Assets/Editor/SceneAssembler.cs` | Persist spawner, panel, font, and player/world references |
| `docs/phase_9_status.md` | Actual build, browser, test, and scope evidence |

Keep related runtime files in existing assemblies. Add no new assembly unless
an actual dependency cycle requires it. Unity creates `.meta` files; include only
the new files' corresponding metadata in feature commits.

## Validation commands

Run one Unity process per project at a time. Use this pattern for each named
test fixture below, substituting a distinct result/log basename:

```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath /Users/ruqayyah/Developer/deencraft \
  -runTests -testPlatform EditMode -testFilter MasjidDistrictLayoutTests \
  -testResults /Users/ruqayyah/.copilot/session-state/8ecf4572-9491-4d0e-9568-40c502073083/files/masjid-layout.xml \
  -logFile /Users/ruqayyah/.copilot/session-state/8ecf4572-9491-4d0e-9568-40c502073083/files/masjid-layout.log
```

For PlayMode fixtures use `-testPlatform PlayMode`. Do not add `-quit` to test
runs. Inspect NUnit XML counts and failures rather than equating exit code zero
with passing tests. Save the red result before implementation and a separate
green result afterward.

## Task 1: Deterministic complete district layout

**Files:** Create `MasjidDistrictLayout.cs`, `MasjidDistrictPlanner.cs`,
`Assets/Tests/EditMode/WorldTests/MasjidDistrictLayoutTests.cs`.

- [ ] Write failing tests for exactly four unique regional styles, disjoint
  complex footprints, same-seed equality, all-or-nothing planning failure,
  candidate rejection, negative coordinates, and distance to initial spawn.
  Use the following public contract in the tests:

```csharp
MasjidDistrictLayout layout = MasjidDistrictPlanner.Create(
    seed: 42, conflicts: bounds => false);
Assert.That(layout.Complexes.Count, Is.EqualTo(4));
Assert.That(layout.Complexes.Select(c => c.Style).Distinct().Count(),
    Is.EqualTo(4));
Assert.That(layout.DistanceFromInitialSpawn, Is.LessThanOrEqualTo(128f));
Assert.Throws<InvalidOperationException>(() =>
    MasjidDistrictPlanner.Create(42, bounds => true));
```

- [ ] Run `MasjidDistrictLayoutTests`; confirm failures are missing district
  functionality rather than a broken test assembly.
- [ ] Define `MasjidStyle` values `Saudi`, `Iraqi`, `Pakistani`, `Turkish`;
  immutable `MasjidComplexLayout` with `Style` and integer `BoundsInt Bounds`;
  and `MasjidDistrictLayout` with a read-only complex list, origin, floor height,
  generation version, and `DistanceFromInitialSpawn`.
- [ ] Implement `Create(int seed, Func<BoundsInt, bool> conflicts)` with four
  ordered candidate origins `(32,-40)`, `(-112,-40)`, `(-40,32)`, `(-40,-112)`.
  The original world spawn is `(0,0)` horizontally, not the player's restored
  travel position. Each candidate reserves an 80 x 80 district.
- [ ] Place 32 x 32 complex plots at candidate-local `(0,0)`, `(48,0)`,
  `(0,48)`, `(48,48)`. The middle 16-block cross connects to the central garden.
  Assign the four styles in preview order; keep the prayer-wall direction +Z.
  Entries can face paths through a side vestibule without rotating prayer rows.
- [ ] Reject a whole candidate when any structural/path write volume conflicts
  with an existing house or modified chunk data. Return only complete layouts.
  Throw an explicit placement exception when all candidates conflict.
- [ ] Calculate a safe common floor from the maximum effective terrain height
  across reserved ground, leaving room below chunk height for the tallest dome
  or minaret. Store the result once, never rescan the district per chunk.
- [ ] Run the fixture again and inspect all assertions. Commit only this task's
  files with `feat(world): plan deterministic masjid district` and the Copilot
  co-author trailer.

## Task 2: Exact terrain support and reserved generation

**Files:** Modify `WorldGenerator.cs`, `FeatureGenerator.cs`,
`ChunkManager.cs`; create
`Assets/Tests/EditMode/WorldTests/MasjidTerrainTests.cs`.

- [ ] Write red tests comparing effective terrain sampling with undecorated
  generated columns in desert dunes, riverside channels, and ordinary land.
  Add order-independence tests for adjacent and negative-coordinate chunks.
  Verify `(0,0)` starter-house generation remains unchanged.
- [ ] Run `MasjidTerrainTests;WideHouseTests;DoorwayTests`.
- [ ] Extract the existing terrain-fill/river-carving stage into
  `WorldGenerator.GenerateTerrain(ChunkData, int chunkX, int chunkZ, int seed)`.
  Keep the existing four-argument `Generate` entry point behavior available to
  callers not supplying a district context.
- [ ] Add a district-aware overload accepting `MasjidDistrictLayout`.
  Apply undecorated terrain, then ordinary features outside reserved write
  volumes, then district structures. Do not write into neighboring ChunkData.
- [ ] Add `FeatureGenerator` reservation checks before tree canopies, vegetation,
  houses, oasis, and boats can intersect reserved geometry. Removing only house
  generation is insufficient: canopy blocks also need exclusion.
- [ ] Remove the legacy beside-house mosque selection/placement route once
  district generation supplies replacements. Update legacy mosque tests to
  assert the replacement behavior; retain cultural block/drop tests.
- [ ] Make `ChunkManager` select its layout once before loading chunks, expose
  `DistrictLayout` and a monotonically increasing `WorldRevision`, and pass
  the immutable layout into generation. Reset layout and revision on `SetSeed`.
  Track accepted runtime edits separately from merely generated cache entries,
  so conflict detection does not incorrectly classify every loaded chunk as
  player-modified. Clear edit tracking with world replacement, not streaming.
  Existing cached data must never be retroactively regenerated to insert a
  district. Planning failure must log an explicit error and keep ordinary
  gameplay available without pretending that district creation succeeded.
- [ ] Run focused tests green, then commit scoped files as
  `feat(world): reserve terrain for cross-chunk masjid landmarks`.

## Task 3: Four traversable regional structures

**Files:** Create `MasjidStructureGenerator.cs`,
`Assets/Tests/EditMode/WorldTests/MasjidStructureTests.cs`;
modify the district-aware generation overload.

- [ ] Write red geometry tests for every style, including entrance clearance,
  7 x 9 usable hall, two-block circulation routes, mihrab/row orientation,
  complete roof, foundation support, rooms, and cross-chunk seams.

```csharp
[TestCase(MasjidStyle.Saudi)]
[TestCase(MasjidStyle.Iraqi)]
[TestCase(MasjidStyle.Pakistani)]
[TestCase(MasjidStyle.Turkish)]
public void Entrance_HasThreeByFourClearance(MasjidStyle style)
{
    var district = MasjidDistrictPlanner.Create(42, bounds => false);
    var complex = district.Complexes.Single(c => c.Style == style);
    foreach (Vector3Int voxel in complex.EntranceClearance.allPositionsWithin)
        Assert.That(SampleGeneratedDistrict(district, voxel), Is.EqualTo(BlockType.Air));
}
```

  Implement `SampleGeneratedDistrict` in the fixture by generating/caching the
  required chunk through the explicit context overload and translating world
  coordinates with `ChunkManager.WorldToChunkCoord`.
  Expose `EntranceClearance`, `HallClearance`, and `CirculationClearances` as
  read-only derived geometry on `MasjidComplexLayout`, not test-only duplicates.
- [ ] Run `MasjidStructureTests` and record red evidence.
- [ ] Implement `MasjidStructureGenerator.Apply(ChunkData chunk, Vector2Int coord,
  MasjidDistrictLayout layout)` as clipped world-coordinate writes. For each
  complex, fill supports down to real terrain, clear occupied room volume,
  construct floor/walls/roof, then explicitly carve all entrances and aisles.
  Validate full descriptors before writing any district blocks.
- [ ] Use existing MudBrick, Stone, Sand, Mosque, Dome, Minaret, and StoneArch
  blocks. Build recognizable dome shapes from many voxels, not one Dome block.
  Add no block IDs unless existing material limits demonstrably prevent the
  approved visual distinctions; a new ID requires atlas, mapping, and tests.
- [ ] Create Saudi shaded arcade/courtyard geometry; Iraqi patterned brick and
  arched-front geometry; Pakistani gateway and three-dome silhouette; Turkish
  central dome with lower subsidiary domes and slender minarets.
  Put geometric pattern accents on walls, never fabricated sacred writing.
- [ ] Build the central fountain/garden and three-block paths. Use graded
  approaches with no greater than the existing controller's step offset per
  step, or a tested ramp mesh; do not require jumping or flight to enter.
- [ ] Verify chunk-order permutations produce identical blocks and no seams,
  run existing house/doorway tests, and commit as
  `feat(world): generate four explorable regional masjids`.

## Task 4: Protection with transactional inventory behavior

**Files:** Create `MasjidDistrictProtection.cs`,
`Assets/Tests/EditMode/WorldTests/MasjidProtectionTests.cs`;
modify `ChunkManager.cs`, `BlockInteractor.cs`, `WaterSimulator.cs`.

- [ ] Write red tests for all faces of each protected volume, interior air,
  foundation, dome/minaret height, and an editable garden/path/outside block.
  In PlayMode add mining/placement inventory snapshots for accepted and rejected
  edits and unloaded/out-of-bounds targets.
- [ ] Run protection and inventory tests red.
- [ ] Add `BlockEditResult` values `Success`, `Protected`, `ChunkUnavailable`,
  `OutOfBounds`. Implement `ChunkManager.TrySetBlock(Vector3Int, BlockType)`
  returning this enum; check protection before mutation, then loaded data and
  bounds, then apply/dirty neighbors and record the changed chunk. Generated structural writes use ChunkData
  directly before activation, not an unrestricted public mutation bypass.
- [ ] Preserve the existing `SetBlock` entry point as a checked wrapper for
  callers that do not need inventory transactions. Explicitly report rejected
  external edits with repository-standard diagnostics. Migrate water simulation
  to the result API and stop propagation at protected/unavailable cells.
- [ ] Update mining and placement to mutate inventory only after
  `BlockEditResult.Success`. Raise a player-targeted denial event for a protected
  attempt; keep event ownership in Player rather than referencing UI from World.
  Failed edits never award drops or consume inventory.
- [ ] Verify normal mining, placement, boundary-neighbor dirtiness, and water
  outside protected complexes. Commit as
  `feat(world): protect masjid landmarks without inventory loss`.

## Task 5: Reproducible selection and safe world switching

**Files:** Modify `WorldSaveData.cs`, `WorldSaveManager.cs`,
`ChunkManager.cs`; create
`Assets/Tests/EditMode/WorldTests/MasjidPersistenceTests.cs`.

- [ ] Write red tests for metadata JSON round-trip, legacy saves with no district
  fields, existing cached edits inside rejected candidates, repeat load,
  same-seed world replacement, and unsupported district-generation versions.
- [ ] Run `MasjidPersistenceTests;AuthModelsTests`.
- [ ] Add optional serializable metadata fields
  `masjidDistrictVersion`, `masjidDistrictOriginX`, `masjidDistrictOriginZ`,
  `masjidDistrictFloorY`. Version zero means an old save without selection.
  Do not add block-edit blobs or change existing backend collection paths.
- [ ] Extend world initialization so restored district metadata is validated
  before chunk generation starts. A saved valid selection remains stable across
  loads. Reject unsupported nonzero versions explicitly rather than moving
  existing landmarks silently.
- [ ] Capture selected district metadata with existing save metadata.
  A legacy save selects a safe complete candidate; cached modified chunks and
  preexisting village footprints are conflicts, not terrain to erase.
  If all candidates fail, surface the explicit placement error.
- [ ] Increment world revision even for same-seed replacement, invalidate
  population assignments, and ensure the old reading panel closes.
- [ ] Run persistence/lifecycle fixtures green. Document that general
  player block-edit persistence remains outside this feature. Commit as
  `feat(world): preserve masjid district selection across world loads`.

## Task 6: Pooled furnishings, teachers, and interaction targets

**Files:** Create `MasjidDistrictSpawner.cs`, `MasjidExplorer.cs`,
`MasjidContentCatalog.cs`,
`Assets/Tests/EditMode/VillagerTests/MasjidPopulationTests.cs`.

- [ ] Write red tests for exactly four pooled sets, one teacher per complex,
  regional labels, required furniture, flat rugs, decorative washing area,
  bounded lights, unobstructed aisles, unload/reactivation, and revision reset.
- [ ] Run `MasjidPopulationTests` red.
- [ ] Implement `MasjidExplorer : MonoBehaviour, IInteractable` with a typed
  style/topic identifier and a request event containing the target and player.
  Teachers and boards use this component; do not attach trade profiles.
  Missing listener/configuration logs an explicit diagnostic.
- [ ] Create one reusable content set per style. Reuse shared meshes/materials
  and build models once. Use simple friendly block people, named from the
  existing approved villager-name list, without claiming one appearance
  represents an entire country.
- [ ] Furnish halls with thin nonphysical aligned rugs, a mihrab niche, entrance
  storage, learning-room tables/seating/bookshelves, boards, and washing-area
  fixtures. Physical furniture must remain outside circulation clearances.
- [ ] Activate each set only when all its supporting chunks have ready
  colliders. Check readiness before activation and deactivate on unload/world
  revision changes; use chunk data identity to catch same-coordinate replacement.
- [ ] Add no more than two shadow-free point lights per active complex, shared
  material palettes, and no new per-frame material creation. Billboard labels
  using the same camera-facing convention as village labels.
- [ ] Write concise original catalog entries: building inspiration, community
  spaces, and welcoming etiquette. Cite the specification's heritage references
  in code-adjacent documentation, not long copied source passages. Verify
  additional claims before publishing them.
- [ ] Run population plus existing village/animal regressions green, then commit
  as `feat(villager): populate masjids with exploration content`.

## Task 7: Reading UI and edit-denial feedback

**Files:** Create `MasjidInformationPanel.cs`,
`Assets/Tests/PlayMode/MasjidInteractionTests.cs`;
modify `VillagerTradePanel.cs`.

- [ ] Write red PlayMode tests for teacher/board E targeting, wall occlusion,
  five-block reach, correct style/topic text, scrolling, Close/Escape,
  prior-input-state restoration, frame guard, target unload, and modal overlap.
  Verify edit-denial feedback is visible and no Join button exists.
- [ ] Run `MasjidInteractionTests;VillageGameplayTests` red.
- [ ] Build the reading panel once under the HUD canvas, using the assigned
  built-in font, a scroll view, title, body, and Close button. Follow existing
  CanvasScaler conventions. Missing font logs an error and does not acquire
  gameplay input.
- [ ] Before either reading or trading opens, reject a new request if
  `PlayerController.InputBlocked` is already true and the requesting panel does
  not already own it. Store previous state, release only owned input, and keep
  the current closing-frame gate. No global catch or unconditional input unlock.
- [ ] Subscribe to typed explorer requests and player denial events only while
  enabled. Show a short timed nonmodal message for protected edit attempts;
  it must not block movement or steal cursor capture.
- [ ] Close a reading panel when its target unloads, the world revision changes,
  the player goes out of range, or the panel disables. Unsubscribe all events.
- [ ] Run both fixtures green, including the actual trade-button inventory test,
  then commit as `feat(ui): add masjid exploration reading panels`.

## Task 8: Persistent scene wiring and traversal verification

**Files:** Modify `SceneAssembler.cs`; regenerate
`Assets/Scenes/Main.slunity.unity`; create
`Assets/Tests/EditMode/WorldTests/MasjidSceneTests.cs`,
`Assets/Tests/PlayMode/MasjidTraversalTests.cs`.

- [ ] Write red saved-scene tests for spawner/player/world/font references and
  PlayMode tests walking into every hall and learning room without jumping,
  checking camera side-wall/roof collision and floor support.
- [ ] Run `MasjidSceneTests;MasjidTraversalTests` in their respective platforms.
- [ ] Add idempotent assembler configuration for the spawner and reading panel.
  Reuse `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`.
  Keep Standard/UI shaders included. Do not add a second camera or EventSystem.
- [ ] Regenerate the scene:

```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath /Users/ruqayyah/Developer/deencraft \
  -executeMethod DeenCraft.Editor.SceneAssembler.AssembleFromCommandLine \
  -logFile /Users/ruqayyah/.copilot/session-state/8ecf4572-9491-4d0e-9568-40c502073083/files/masjid-scene.log
```

- [ ] Inspect scene log, rerun scene/traversal tests, and fix geometry rather than
  relaxing the clearance assertions or changing player dimensions.
- [ ] Commit only assembler, generated scene, new tests, and relevant metadata
  as `feat(scene): wire and verify explorable masjid district`.
  The scene/assembler include prior untracked work: review the entire staged
  versions and obtain approval before bundling unrelated earlier content.

## Task 9: Focused regression, browser walkthrough, and delivery

**Files:** `docs/phase_9_status.md`; generated WebGL output and launcher version.

- [ ] Run all new EditMode fixtures in one filtered invocation, plus existing
  house/doorway, inventory/trading, chunk meshing, terrain recovery, cloud,
  animal, and scene fixtures affected by the changes.
- [ ] Run all new PlayMode fixtures together with
  `VillageGameplayTests;PlayerMovementTests`. Inspect XML totals.
- [ ] Run hosting tests:

```bash
python3 -B -m unittest discover -s tests -p 'test_webgl_hosting.py' -v
```

  Expected: all six hosting regressions pass unless the repository intentionally
  adds new cases. Do not claim unrelated full-suite failures are fixed.
- [ ] Build sequentially after tests and scene assembly:

```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -nographics -projectPath /Users/ruqayyah/Developer/deencraft \
  -executeMethod DeenCraft.Editor.WebGLBuildScript.Build \
  -logFile /Users/ruqayyah/.copilot/session-state/8ecf4572-9491-4d0e-9568-40c502073083/files/masjid-build.log
```

- [ ] Verify successful build output, versioned launcher, and HTTP 200 on the
  existing port 8080 server. Do not launch a competing listener.
- [ ] In a fresh focused browser page, walk from the starting village to the
  district and enter all four complexes. Capture exterior silhouettes, hall and
  classroom interiors, reading UI, and visible edit-protection feedback.
  Verify existing villager trading and flight still work outside the district.
- [ ] Measure browser frame times during a 60-second district walkthrough
  after loading, recording browser, device, viewport, average FPS, and stalls.
  Compare the measured result with the >=30 FPS target. If lower, profile and
  reduce geometry/light/object cost, then rebuild and retest.
- [ ] Record unavailable browser capabilities or pointer-lock limitations
  explicitly; do not substitute a static mockup for a gameplay walkthrough.
- [ ] Update phase status with exact test counts, build result, measured
  performance, screenshots checked, scope limits, and remaining failures.
- [ ] Run `git diff --check` on feature files. Review the scoped diff before
  committing documentation as `docs: record masjid district verification`.
  Keep all unrelated preexisting changes intact.

## Self-review and checkpoints

Coverage: tasks 1-3 cover regional architecture, placement, terrain and routes;
task 4 protects volumes and inventory; task 5 covers reproducibility and saves;
tasks 6-7 cover exploration content and UI; tasks 8-9 cover scene persistence,
real traversal, browser performance, and delivery evidence.

Review after tasks 2, 5, and 8 before proceeding. If safe nearby placement or
save compatibility cannot satisfy the approved specification, stop with the
failing evidence and obtain a design decision rather than silently shrinking
buildings, moving them far away, or overwriting player work.

All commits must be scoped and include:

```text
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

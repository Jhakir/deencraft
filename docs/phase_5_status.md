# Phase 5 Status — Animals & Entities

## Completion Status: ✅ DONE

Completed on: Phase 5 full pass.
Cumulative test count: **169 EditMode tests** (148 Phase 1–4 + 21 Phase 5).

---

## What Was Built

### Bug Fixes
- **`ChunkManager.GetBlock(int x, int y, int z)`** — Added missing method referenced by `PlayerController.IsInWater()`. Previously caused a compile error.
- **`BlockType ↔ ItemId` mismatch corrected** — Phase 4 code had an incorrect comment claiming the two enums shared integer values (they do NOT after index 4). Created `BlockItemMap.cs` with explicit bidirectional lookup. Removed the misleading comment from `ItemId.cs`.

### Core
- **`IInteractable.cs`** — Interface in `DeenCraft` namespace (Core assembly). `void Interact(GameObject interactor)`. Used by horses, boats, villagers, sheep — enables entity interaction without circular assembly dependencies.

### Player
- **`ItemId.cs`** — Added: Diamond=110, Gold=111, Wool=120, Meat=121, Egg=122, Milk=123, Feather=124, Fish=304.
- **`BlockItemMap.cs`** — Static class with `BlockTypeToItemId(BlockType)` and `ItemIdToBlockType(ItemId)`. Explicit switch-based lookup (no int casting).
- **`BlockInteractor.cs`** — Replaces Phase-2 stub `BlockInteractionManager`. Left-click mines block and gives item via inventory. Right-click places active hotbar block. E key calls `IInteractable.Interact()` on any entity in range.

### Animals (DeenCraft.Animals assembly)
- **`AnimalType.cs`** — enum: Horse, Cat, Chicken, Cow, Sheep.
- **`AnimalState.cs`** — enum: Idle, Wander, Flee, Follow, Graze.
- **`AnimalBrainData.cs`** — Pure C# state machine. Injectable `System.Random` for testability. States: Idle→Wander cycle, Follow when player has food in range, Flee on demand. `ForceWanderTarget()` for testing.
- **`AnimalController.cs`** — MonoBehaviour + IInteractable. CharacterController locomotion driven by brain. Sheep shearing on Interact() with 60s regrow cooldown.
- **`HorseController.cs`** — MonoBehaviour + IInteractable. Mount/dismount logic. Disables PlayerController while riding. WASD to steer at `HorseRideSpeed`. E to dismount.
- **`BoatController.cs`** — MonoBehaviour + IInteractable. Floats at configurable `_waterSurfaceY` (default 63.5f, matches world gen). Board/disembark via E. WASD steering on water.
- **`Animals.asmdef`** — Updated: added DeenCraft.World and DeenCraft.Player references.

### Villager (DeenCraft.Villager assembly)
- **`TradeOffer.cs`** — Pure C# data class. RequestedItem/Count and OfferedItem/Count.
- **`VillagerProfile.cs`** — Pure C#. Hardcoded Arabic name whitelist (24 names). `GetRandomName()` and `CreateDefault()` factory methods. Default trades include Diamond/Gold/Wheat exchanges.
- **`TradeSession.cs`** — Pure C# static class. Atomic trade execution: validates inputs, checks inventory space BEFORE mutating, rollback on unexpected overflow. Rejects zero/negative counts (security).
- **`VillagerAI.cs`** — MonoBehaviour + IInteractable. Rotates to face player within 10 units. `ExecuteTrade(tradeIndex, inventory)` for Phase 8 UI. Logs interaction for Phase 8 wiring.
- **`Villager.asmdef`** — Updated: added DeenCraft.Player reference.

### Tests
- **`AnimalBrainTests.cs`** — 10 tests: initial state, TriggerFlee, flee expiry, Follow transitions, ForceWander, wander-target-reached, idle timer.
- **`TradeSessionTests.cs`** — 8 tests: null checks, insufficient items, successful execution, item removal/addition, full-inventory, zero/negative-count security.
- **`VillagerProfileTests.cs`** — 3 tests: name non-empty, trades present, positive counts.
- **`BlockItemMapTests.cs`** — 6 tests: Grass identity mapping, Wood non-trivial mapping, Water→None, Mosque→None, reverse Wood mapping, Diamond non-placeable.
- **`DeenCraft.Tests.EditMode.asmdef`** — Updated: added DeenCraft.Animals and DeenCraft.Villager.

---

## Security Review

| Concern | Mitigation |
|---|---|
| Zero/negative trade counts → item duplication | `TradeSession.CanExecute` rejects `<= 0` counts |
| Partial trade state (items removed but not given) | `HasRoomForItem` checked before any mutation |
| Out-of-bounds block operations | `ChunkManager.GetBlock` returns `Air` safely; `ChunkData.IsInBounds` checked |
| Player placing block inside themselves | `BlockInteractor.IsInsidePlayer` Bounds check |
| Excessive interaction range | All raycasts bounded by `GameConstants.PlayerReach` (5 units) |
| Villager name injection | Names are hardcoded whitelist, never from user input |
| Horse/boat dual-mount exploit | `IsRidden`/`HasPilot` checked before mount |

---

## Assembly Dependency Graph (no cycles)

```
Core ← World ← Player ← Animals
                       ← Villager
                       ← Crafting
Tests → Core, Auth, World, Player, Crafting, Animals, Villager
```

---

## Phase 6 Notes

- Trade UI panels need to be wired to `VillagerAI.ExecuteTrade()` (Phase 8 scope).
- Procedural villager spawning near village structures (FeatureGenerator integration).
- Animal prefabs with actual art need to be created (Phase 8 scope).
- `BlockInteractionManager.cs` (Phase 2 stub) can be removed from the scene — it should be disabled/removed since `BlockInteractor` supersedes it.
- Mining progress bar / multi-hit breaking (currently instant) is a Phase 6 enhancement.

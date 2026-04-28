# Phase 4: Character System — Design Spec
**Date:** 2026-04-28  
**Status:** Approved (Option A — full scope, placeholder art)

---

## Goals

Add a fully playable character with third-person locomotion, a customisable appearance, item inventory, crafting, and health/hunger — all backed by data models that are independently testable without Unity runtime.

---

## Architecture Decisions

### Placeholder Art
- Player body = Unity **Capsule** primitive. No rigged FBX in Phase 4.
- `PlayerAnimator` drives an Animator Controller but no-ops gracefully if Animator is null.
- Real art wired in Phase 8 (or when asset is ready).

### Input System
- Unity **legacy Input** (`Input.GetAxis`, `Input.GetKey`). No new Input System package dependency.

### Physics
- Player uses Unity **CharacterController** (not Rigidbody). Better step-detection for voxel terrain.
- Gravity constant = −20 (already set in Physics settings from Phase 1).

### Testability via Data/Logic Split
MonoBehaviours that contain non-trivial game logic each have a pure-C# "data" companion that unit tests can target directly:

| MonoBehaviour | Pure C# companion | Responsibility |
|---|---|---|
| `VitalitySystem` | `VitalityData` | Health + hunger maths |
| `CraftingSystem` | `CraftingGrid` | Recipe matching, crafting |
| `PlayerController` | — (movement is physics, not logic) | — |

`Inventory` is pure C# throughout (no MonoBehaviour needed).

---

## File Map

### DeenCraft.Player assembly

| File | Type | Responsibility |
|------|------|----------------|
| `ItemId.cs` | enum | All item identifiers |
| `ItemStack.cs` | struct | Item + count pair |
| `Inventory.cs` | class | Hotbar[9] + backpack[27], add/remove logic |
| `PlayerAnimationState.cs` | enum | Idle, Walk, Run, Jump, Swim, Mine, Place |
| `CharacterAppearance.cs` | data classes | SkinTone, HeadwearType, ClothingStyle enums + CharacterAppearance class |
| `CharacterCustomizer.cs` | MonoBehaviour | Applies appearance to SkinnedMeshRenderer; saves/loads via PlayerPrefs |
| `VitalityData.cs` | class | Health/hunger maths, `Tick(float dt)` |
| `VitalitySystem.cs` | MonoBehaviour | Calls `VitalityData.Tick` in Update, exposes public API |
| `PlayerController.cs` | MonoBehaviour | CharacterController movement, mouse look, swim detection |
| `PlayerAnimator.cs` | MonoBehaviour | Reads PlayerController state → drives Animator params |

### DeenCraft.Crafting assembly

| File | Type | Responsibility |
|------|------|----------------|
| `CraftingRecipe.cs` | class | RecipeId, Ingredients[9], Result, Shapeless flag |
| `RecipeDatabase.cs` | static class | Hardcoded recipe registry, `FindMatch(ItemStack[])` |
| `CraftingGrid.cs` | class | 3×3 grid state, `TryCraft(Inventory)` |
| `CraftingSystem.cs` | MonoBehaviour | Wraps CraftingGrid, hooks to UI |

---

## ItemId Enum (Phase 4 scope)

Block-items share their byte value with BlockType where possible:

```
None=0, Grass=1..Thatch=19  (block items)
WoodPickaxe=100, StonePickaxe=101
WoodAxe=102, StoneAxe=103
WoodShovel=104, StoneShovel=105
Stick=200, String=201
Bread=300, Date=301, Fig=302, Falafel=303
```

---

## Inventory

- `_hotbar[9]` and `_backpack[27]` of `ItemStack`.
- `AddItem(stack)` fills hotbar slots first, then backpack, respecting `MaxStackSize=64`.
- `RemoveItem(itemId, count)` scans hotbar first, then backpack.
- `GetAllSlots()` returns combined read-only view for UI.
- `SelectedSlot` (0–8), `ActiveItem` shortcut.

---

## Crafting Recipes (Phase 4)

| Recipe | Ingredients | Result |
|--------|-------------|--------|
| Stick | 2× Wood (shapeless) | 4× Stick |
| Wood Pickaxe | 3× Wood (top row) + 2× Stick (middle col) | 1× WoodPickaxe |
| Wood Axe | 3× Wood (top-right L) + 2× Stick | 1× WoodAxe |
| Wood Shovel | 1× Wood (top-centre) + 2× Stick | 1× WoodShovel |
| Stone Pickaxe | 3× Stone (top row) + 2× Stick | 1× StonePickaxe |
| Bread | 3× Wheat (shapeless) | 1× Bread |

All Phase 4 recipes are **shapeless** for simplicity. Shaped recipes can be added in Phase 6.

---

## Vitality

- `Health`: float 0–20. Falls to 0 → character collapses (no respawn in Phase 4, just frozen).
- `Hunger`: float 0–20. Drains at `HungerDrainRate = 0.05f`/s while moving.
- If `Hunger <= 0`: `TakeDamage(0.5f * dt)` per second.
- If `Hunger >= HungerRegenThresh (18)`: `Heal(HealthRegenRate * dt)`.
- `Eat(float restore)`: adds to Hunger, clamps to MaxHunger.

---

## Player Controller

- `CharacterController` required component.
- Third-person camera: child GameObject offset `(0, 1.6, -4)`, looks at player head.
- Mouse X → rotate player body (`_yaw`).
- Mouse Y → tilt camera (`_pitch`, clamped −80° to +60°).
- WASD → move in `transform.forward/right` * speed * dt, applied via `CharacterController.Move`.
- Space → jump: sets `_verticalVelocity = PlayerJumpForce` if grounded.
- Left Shift → sprint (use `PlayerSprintSpeed`).
- Gravity: accumulate `_verticalVelocity -= 20 * dt` each frame; reset to −2 when grounded.
- Swim: if block at (player position + (0, 0.5, 0)) == Water → swim mode: reduced gravity (−2), WASD + Space (up) / Left Ctrl (down).

---

## PlayerAnimator

Drives these Animator parameter hashes (set via `Animator.SetFloat/SetBool/SetInteger`):

| Param | Type | Description |
|-------|------|-------------|
| `Speed` | float | 0=idle, 1=walk, 2=run |
| `IsJumping` | bool | in air |
| `IsSwimming` | bool | swim mode |
| `ActionState` | int | PlayerAnimationState cast to int |

No-ops if `Animator` component is null (placeholder capsule case).

---

## Character Customizer

`CharacterAppearance` fields:
- `SkinTone` enum: Light, MediumLight, Medium, MediumDark, Dark
- `HeadwearType` enum: None, Hijab, Kufi
- `ClothingStyle` enum: Casual, Traditional, Winter
- `ClothingColor`: UnityEngine.Color (serializable)

`CharacterCustomizer.Apply()` sets `SkinnedMeshRenderer` material color. No-ops if renderer null.  
`CharacterCustomizer.Save(string key)` / `Load(string key)` via `PlayerPrefs` + `JsonUtility`.

---

## Tests (EditMode)

### PlayerTests
- `InventoryTests.cs` (12 tests): AddItem fills hotbar first, stack merging, max stack, RemoveItem, empty checks
- `VitalityTests.cs` (8 tests): damage, heal, hunger drain, starvation damage, regen threshold, eat clamp

### CraftingTests
- `RecipeDatabaseTests.cs` (6 tests): each recipe resolves, unknown combo returns null, case sensitivity
- `CraftingGridTests.cs` (6 tests): TryCraft produces result, removes ingredients, full inventory rejection, clear grid

# Phase 4 Status — Character System

**Completed:** 2026-04-28  
**Branch:** main  
**Commits:** see git log from "feat(player): add ItemId enum and ItemStack struct"

---

## What Was Built

### DeenCraft.Player assembly (`Assets/Scripts/Player/`)

| File | Description |
|------|-------------|
| `ItemId.cs` | Enum: None=0, block items 1–19, tools 100–105, ingredients 200–201, food 300–303 |
| `ItemStack.cs` | Struct: ItemId + Count, IsEmpty, Empty sentinel, ToString |
| `Inventory.cs` | Hotbar[9] + backpack[27]; AddItem (merge + fill), RemoveItem (atomic), CountItem, IsEmpty, SetSelectedSlot |
| `InventoryHolder.cs` | MonoBehaviour bridge holding Inventory instance on Player GameObject |
| `VitalityData.cs` | Pure-C# health/hunger: Tick(dt, isMoving), TakeDamage, Heal, Eat, SetHealth/SetHunger |
| `VitalitySystem.cs` | MonoBehaviour wrapper; Update → Tick; food restore map (Bread=5, Date=3, Fig=2, Falafel=6) |
| `CharacterAppearance.cs` | Enums SkinTone/HeadwearType/ClothingStyle + [Serializable] CharacterAppearance with GetSkinColor() |
| `CharacterCustomizer.cs` | MonoBehaviour; Apply() to renderers; Save/Load via PlayerPrefs + JsonUtility |
| `PlayerAnimationState.cs` | Enum: Idle=0, Walk=1, Run=2, Jump=3, Swim=4, Mine=5, Place=6 |
| `PlayerAnimator.cs` | MonoBehaviour; drives Animator Speed/IsJumping/IsSwimming/ActionState; null-safe |
| `PlayerController.cs` | MonoBehaviour; CharacterController, WASD+mouse look, jump, sprint, swim (Water block detect), third-person camera arm |

### DeenCraft.Crafting assembly (`Assets/Scripts/Crafting/`)

| File | Description |
|------|-------------|
| `CraftingRecipe.cs` | RecipeId, Ingredients[9], Result, IsShapeless |
| `RecipeDatabase.cs` | Static registry; 6 shapeless recipes: Stick, WoodPickaxe, WoodAxe, WoodShovel, StonePickaxe, Bread |
| `CraftingGrid.cs` | Pure-C#; 3×3 grid; TryCraft (consumes + adds to inventory), Preview, Clear |
| `CraftingSystem.cs` | MonoBehaviour wrapper; SetSlot/TryCraft/GetPreview/SetInventory |

### Assembly definitions updated

- `DeenCraft.Tests.EditMode.asmdef` — added `DeenCraft.Player` and `DeenCraft.Crafting` references
- `Crafting.asmdef` — added `DeenCraft.World` and `DeenCraft.Player` references

---

## Tests Added (32 new)

| Suite | Count | Location |
|-------|-------|----------|
| InventoryTests | 12 | `Assets/Tests/EditMode/PlayerTests/InventoryTests.cs` |
| VitalityTests | 8 | `Assets/Tests/EditMode/PlayerTests/VitalityTests.cs` |
| RecipeDatabaseTests | 6 | `Assets/Tests/EditMode/CraftingTests/RecipeDatabaseTests.cs` |
| CraftingGridTests | 6 | `Assets/Tests/EditMode/CraftingTests/CraftingGridTests.cs` |

**Cumulative total: 148 EditMode tests**

---

## Art

- **Placeholder only**: Unity Capsule for player body. No rigged FBX, no animation clips.
- `PlayerAnimator` gracefully no-ops if Animator component is absent.
- Real character art to be wired in Phase 8.

---

## Design Notes / Bugs Fixed

- Camera: Initial spec had `LookAt` overwriting `localRotation`. Fixed to use a pitch-rotated arm (camera positioned at angle on sphere around player, then `LookAt` player head).

---

## Phase 5 Notes

- **UI required**: HUD (health bar, hunger bar, hotbar), inventory screen (hotbar + backpack grid), crafting screen (3×3 grid + output slot), character creation screen
- **Character creation flow**: `CharacterCustomizer.HasSavedAppearance()` → show creation screen on first login
- **Block interaction**: Left-click mine / right-click place deferred to Phase 5 (block breaking/placing + item drops)
- **Item drops**: Mined blocks should `AddItem` to `InventoryHolder.Inventory` — wire in Phase 5
- **VitalitySystem food**: `Eat(ItemId)` is implemented — Phase 5 UI needs a "use item" action to call it
- **CraftingSystem**: Pure logic done; Phase 5 UI connects slots to `Grid.SetSlot()` and calls `TryCraft()`
- **New block types**: Next IDs start at 20 (IDs 0–19 used by existing BlockType enum)
- **PlayerController**: Mouse look sensitivity is hardcoded at 2f — expose as `[SerializeField]` in Phase 5 settings panel

# Phase 4: Character System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fully playable third-person character with item inventory, crafting, and health/hunger — all backed by independently unit-testable pure-C# logic.

**Architecture:** `CharacterController`-based third-person locomotion with a capsule placeholder; pure-C# data classes (`Inventory`, `VitalityData`, `CraftingGrid`, `RecipeDatabase`) tested via Unity EditMode; MonoBehaviours (`PlayerController`, `VitalitySystem`, `CharacterCustomizer`, `CraftingSystem`, `PlayerAnimator`) wire the data to the scene.

**Tech Stack:** Unity 2022.3 LTS, C#, CharacterController, Animator (stub controller), PlayerPrefs, Unity Test Framework (EditMode + PlayMode)

---

## File Map

### DeenCraft.Player assembly — `Assets/Scripts/Player/`

| File | Create/Modify |
|------|--------------|
| `ItemId.cs` | Create |
| `ItemStack.cs` | Create |
| `Inventory.cs` | Create |
| `PlayerAnimationState.cs` | Create |
| `CharacterAppearance.cs` | Create |
| `CharacterCustomizer.cs` | Create |
| `VitalityData.cs` | Create |
| `VitalitySystem.cs` | Create |
| `PlayerController.cs` | Create |
| `PlayerAnimator.cs` | Create |

### DeenCraft.Crafting assembly — `Assets/Scripts/Crafting/`

| File | Create/Modify |
|------|--------------|
| `CraftingRecipe.cs` | Create |
| `RecipeDatabase.cs` | Create |
| `CraftingGrid.cs` | Create |
| `CraftingSystem.cs` | Create |

### Assembly definitions — modify existing

| File | Change |
|------|--------|
| `Assets/Scripts/Crafting/Crafting.asmdef` | Add `DeenCraft.World` reference |
| `Assets/Tests/EditMode/DeenCraft.Tests.EditMode.asmdef` | Add `DeenCraft.Player`, `DeenCraft.Crafting` references |

### Tests — `Assets/Tests/EditMode/`

| File | Create |
|------|--------|
| `PlayerTests/InventoryTests.cs` | Create |
| `PlayerTests/VitalityTests.cs` | Create |
| `CraftingTests/RecipeDatabaseTests.cs` | Create |
| `CraftingTests/CraftingGridTests.cs` | Create |

---

## Task 1: ItemId enum + ItemStack struct

**Files:**
- Create: `Assets/Scripts/Player/ItemId.cs`
- Create: `Assets/Scripts/Player/ItemStack.cs`

- [ ] **Step 1.1: Create ItemId.cs**

```csharp
// Assets/Scripts/Player/ItemId.cs
using DeenCraft.World;

namespace DeenCraft.Player
{
    public enum ItemId
    {
        None = 0,

        // Block items (match BlockType byte values 1-19)
        Grass     = 1,
        Dirt      = 2,
        Stone     = 3,
        Sand      = 4,
        Wood      = 5,
        Leaves    = 6,
        Water     = 7,
        OliveWood = 8,
        OliveLeaf = 9,
        PalmWood  = 10,
        PalmLeaf  = 11,
        MudBrick  = 12,
        SnowBlock = 13,
        IceBlock  = 14,
        Cobblestone = 15,
        Gravel    = 16,
        Flower    = 17,
        Wheat     = 18,
        Thatch    = 19,

        // Tool items
        WoodPickaxe  = 100,
        StonePickaxe = 101,
        WoodAxe      = 102,
        StoneAxe     = 103,
        WoodShovel   = 104,
        StoneShovel  = 105,

        // Crafting ingredients
        Stick  = 200,
        String = 201,

        // Food items
        Bread   = 300,
        Date    = 301,
        Fig     = 302,
        Falafel = 303,
    }
}
```

- [ ] **Step 1.2: Create ItemStack.cs**

```csharp
// Assets/Scripts/Player/ItemStack.cs
namespace DeenCraft.Player
{
    [System.Serializable]
    public struct ItemStack
    {
        public ItemId ItemId;
        public int Count;

        public bool IsEmpty => ItemId == ItemId.None || Count <= 0;

        public static readonly ItemStack Empty = new ItemStack { ItemId = ItemId.None, Count = 0 };

        public ItemStack(ItemId itemId, int count)
        {
            ItemId = itemId;
            Count  = count;
        }

        public override string ToString() => IsEmpty ? "Empty" : $"{ItemId}x{Count}";
    }
}
```

- [ ] **Step 1.3: Commit**

```bash
git add Assets/Scripts/Player/ItemId.cs Assets/Scripts/Player/ItemStack.cs
git commit -m "feat(player): add ItemId enum and ItemStack struct"
```

---

## Task 2: Inventory class

**Files:**
- Create: `Assets/Scripts/Player/Inventory.cs`

- [ ] **Step 2.1: Create Inventory.cs**

```csharp
// Assets/Scripts/Player/Inventory.cs
using System.Collections.Generic;
using DeenCraft;

namespace DeenCraft.Player
{
    public class Inventory
    {
        private const int HotbarSize  = GameConstants.HotbarSlots;   // 9
        private const int BackpackSize = GameConstants.BackpackSlots; // 27

        private readonly ItemStack[] _hotbar   = new ItemStack[HotbarSize];
        private readonly ItemStack[] _backpack = new ItemStack[BackpackSize];

        public int SelectedSlot { get; private set; } = 0;

        public ItemStack ActiveItem => _hotbar[SelectedSlot];

        public Inventory()
        {
            for (int i = 0; i < HotbarSize;  i++) _hotbar[i]   = ItemStack.Empty;
            for (int i = 0; i < BackpackSize; i++) _backpack[i] = ItemStack.Empty;
        }

        /// <summary>Returns hotbar slot contents (index 0-8).</summary>
        public ItemStack GetHotbarSlot(int index) => _hotbar[index];

        /// <summary>Returns backpack slot contents (index 0-26).</summary>
        public ItemStack GetBackpackSlot(int index) => _backpack[index];

        /// <summary>Sets selected hotbar slot (0-8).</summary>
        public void SetSelectedSlot(int index)
        {
            if (index >= 0 && index < HotbarSize)
                SelectedSlot = index;
        }

        /// <summary>
        /// Adds items to inventory. Fills hotbar first, then backpack.
        /// Merges into existing stacks before using new slots.
        /// Returns the number of items that could NOT be added (overflow).
        /// </summary>
        public int AddItem(ItemStack stack)
        {
            int remaining = stack.Count;
            remaining = FillSlots(_hotbar,   stack.ItemId, remaining);
            remaining = FillSlots(_backpack, stack.ItemId, remaining);
            return remaining;
        }

        private int FillSlots(ItemStack[] slots, ItemId itemId, int remaining)
        {
            // Pass 1: merge into existing stacks
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].ItemId == itemId && slots[i].Count < GameConstants.MaxStackSize)
                {
                    int space = GameConstants.MaxStackSize - slots[i].Count;
                    int toAdd = System.Math.Min(space, remaining);
                    slots[i] = new ItemStack(itemId, slots[i].Count + toAdd);
                    remaining -= toAdd;
                }
            }
            // Pass 2: fill empty slots
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int toAdd = System.Math.Min(GameConstants.MaxStackSize, remaining);
                    slots[i] = new ItemStack(itemId, toAdd);
                    remaining -= toAdd;
                }
            }
            return remaining;
        }

        /// <summary>
        /// Removes count items of itemId. Scans hotbar first, then backpack.
        /// Returns true if all items were removed, false if not enough items.
        /// Does NOT partially remove on failure.
        /// </summary>
        public bool RemoveItem(ItemId itemId, int count)
        {
            if (CountItem(itemId) < count) return false;

            int remaining = count;
            remaining = DrainSlots(_hotbar,   itemId, remaining);
            remaining = DrainSlots(_backpack, itemId, remaining);
            return remaining == 0;
        }

        private int DrainSlots(ItemStack[] slots, ItemId itemId, int remaining)
        {
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].ItemId == itemId)
                {
                    int take = System.Math.Min(slots[i].Count, remaining);
                    int left = slots[i].Count - take;
                    slots[i] = left > 0 ? new ItemStack(itemId, left) : ItemStack.Empty;
                    remaining -= take;
                }
            }
            return remaining;
        }

        /// <summary>Returns total count of itemId across all slots.</summary>
        public int CountItem(ItemId itemId)
        {
            int total = 0;
            foreach (var s in _hotbar)   if (s.ItemId == itemId) total += s.Count;
            foreach (var s in _backpack) if (s.ItemId == itemId) total += s.Count;
            return total;
        }

        /// <summary>Returns true if all slots are empty.</summary>
        public bool IsEmpty()
        {
            foreach (var s in _hotbar)   if (!s.IsEmpty) return false;
            foreach (var s in _backpack) if (!s.IsEmpty) return false;
            return true;
        }

        /// <summary>Sets a hotbar slot directly (used by CraftingGrid output).</summary>
        internal void SetHotbarSlot(int index, ItemStack stack) => _hotbar[index] = stack;

        /// <summary>Sets a backpack slot directly (used by CraftingGrid output).</summary>
        internal void SetBackpackSlot(int index, ItemStack stack) => _backpack[index] = stack;
    }
}
```

- [ ] **Step 2.2: Commit**

```bash
git add Assets/Scripts/Player/Inventory.cs
git commit -m "feat(player): add Inventory class (hotbar + backpack)"
```

---

## Task 3: Inventory unit tests

**Files:**
- Create: `Assets/Tests/EditMode/PlayerTests/InventoryTests.cs`
- Modify: `Assets/Tests/EditMode/DeenCraft.Tests.EditMode.asmdef` (add DeenCraft.Player + DeenCraft.Crafting references)

- [ ] **Step 3.1: Update EditMode asmdef**

Replace the content of `Assets/Tests/EditMode/DeenCraft.Tests.EditMode.asmdef` with:

```json
{
    "name": "DeenCraft.Tests.EditMode",
    "rootNamespace": "DeenCraft.Tests.EditMode",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "DeenCraft.Core",
        "DeenCraft.Auth",
        "DeenCraft.World",
        "DeenCraft.Player",
        "DeenCraft.Crafting"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3.2: Create InventoryTests.cs**

```csharp
// Assets/Tests/EditMode/PlayerTests/InventoryTests.cs
using NUnit.Framework;
using DeenCraft.Player;

namespace DeenCraft.Tests.EditMode
{
    public class InventoryTests
    {
        [Test]
        public void AddItem_FillsHotbarFirst()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 1));
            Assert.AreEqual(ItemId.Stone, inv.GetHotbarSlot(0).ItemId);
            Assert.AreEqual(ItemId.None,  inv.GetBackpackSlot(0).ItemId);
        }

        [Test]
        public void AddItem_MergesIntoExistingStack()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 30));
            inv.AddItem(new ItemStack(ItemId.Stone, 20));
            Assert.AreEqual(50, inv.GetHotbarSlot(0).Count);
        }

        [Test]
        public void AddItem_RespectsMaxStackSize()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 64));
            int overflow = inv.AddItem(new ItemStack(ItemId.Stone, 1));
            Assert.AreEqual(64, inv.GetHotbarSlot(0).Count);
            Assert.AreEqual(ItemId.Stone, inv.GetHotbarSlot(1).ItemId);
            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void AddItem_ReturnsOverflow_WhenInventoryFull()
        {
            var inv = new Inventory();
            // Fill all 36 slots with max stacks of Stone (36 * 64 = 2304 items)
            inv.AddItem(new ItemStack(ItemId.Stone, 36 * 64));
            int overflow = inv.AddItem(new ItemStack(ItemId.Stone, 5));
            Assert.AreEqual(5, overflow);
        }

        [Test]
        public void RemoveItem_ReturnsFalse_WhenNotEnoughItems()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Wood, 3));
            bool result = inv.RemoveItem(ItemId.Wood, 5);
            Assert.IsFalse(result);
            Assert.AreEqual(3, inv.CountItem(ItemId.Wood)); // unchanged
        }

        [Test]
        public void RemoveItem_RemovesCorrectCount()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Wood, 10));
            bool result = inv.RemoveItem(ItemId.Wood, 4);
            Assert.IsTrue(result);
            Assert.AreEqual(6, inv.CountItem(ItemId.Wood));
        }

        [Test]
        public void RemoveItem_ClearsSlotWhenExhausted()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Wood, 5));
            inv.RemoveItem(ItemId.Wood, 5);
            Assert.IsTrue(inv.GetHotbarSlot(0).IsEmpty);
        }

        [Test]
        public void CountItem_SumsAcrossHotbarAndBackpack()
        {
            var inv = new Inventory();
            // Fill 9 hotbar slots + 1 backpack slot with 10 items each
            for (int i = 0; i < 10; i++)
                inv.AddItem(new ItemStack(ItemId.Dirt, 64)); // fills 10 slots
            Assert.AreEqual(640, inv.CountItem(ItemId.Dirt));
        }

        [Test]
        public void IsEmpty_ReturnsTrueOnNewInventory()
        {
            var inv = new Inventory();
            Assert.IsTrue(inv.IsEmpty());
        }

        [Test]
        public void IsEmpty_ReturnsFalseAfterAddItem()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Grass, 1));
            Assert.IsFalse(inv.IsEmpty());
        }

        [Test]
        public void SetSelectedSlot_IgnoresOutOfRange()
        {
            var inv = new Inventory();
            inv.SetSelectedSlot(3);
            Assert.AreEqual(3, inv.SelectedSlot);
            inv.SetSelectedSlot(9);  // out of range
            Assert.AreEqual(3, inv.SelectedSlot); // unchanged
        }

        [Test]
        public void ActiveItem_ReflectsSelectedSlot()
        {
            var inv = new Inventory();
            inv.AddItem(new ItemStack(ItemId.Stone, 1));
            inv.AddItem(new ItemStack(ItemId.Wood,  1));
            inv.SetSelectedSlot(1);
            Assert.AreEqual(ItemId.Wood, inv.ActiveItem.ItemId);
        }
    }
}
```

- [ ] **Step 3.3: Commit**

```bash
git add Assets/Tests/EditMode/DeenCraft.Tests.EditMode.asmdef \
        Assets/Tests/EditMode/PlayerTests/InventoryTests.cs
git commit -m "test(player): inventory unit tests (12 cases)"
```

---

## Task 4: VitalityData + VitalitySystem

**Files:**
- Create: `Assets/Scripts/Player/VitalityData.cs`
- Create: `Assets/Scripts/Player/VitalitySystem.cs`

- [ ] **Step 4.1: Create VitalityData.cs**

```csharp
// Assets/Scripts/Player/VitalityData.cs
using DeenCraft;

namespace DeenCraft.Player
{
    /// <summary>
    /// Pure-C# health/hunger model. No Unity dependency — fully unit-testable.
    /// Call Tick(dt) every frame to advance hunger drain and health regen.
    /// </summary>
    public class VitalityData
    {
        public float Health  { get; private set; } = GameConstants.MaxHealth;
        public float Hunger  { get; private set; } = GameConstants.MaxHunger;
        public bool  IsAlive { get; private set; } = true;

        /// <summary>
        /// Advance vitality by deltaTime seconds.
        /// isMoving: true if player moved this frame (hunger drains faster while moving).
        /// </summary>
        public void Tick(float deltaTime, bool isMoving)
        {
            if (!IsAlive) return;

            // Drain hunger
            float drainMultiplier = isMoving ? 2f : 1f;
            Hunger -= GameConstants.HungerDrainRate * drainMultiplier * deltaTime;
            if (Hunger < 0f) Hunger = 0f;

            // Starvation: take damage when hunger is 0
            if (Hunger <= 0f)
            {
                Health -= 0.5f * deltaTime;
                if (Health <= 0f)
                {
                    Health  = 0f;
                    IsAlive = false;
                }
            }
            // Regen: heal when hunger is high
            else if (Hunger >= GameConstants.HungerRegenThresh)
            {
                Health += GameConstants.HealthRegenRate * deltaTime;
                if (Health > GameConstants.MaxHealth)
                    Health = GameConstants.MaxHealth;
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            Health -= amount;
            if (Health <= 0f)
            {
                Health  = 0f;
                IsAlive = false;
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health += amount;
            if (Health > GameConstants.MaxHealth)
                Health = GameConstants.MaxHealth;
        }

        /// <summary>Eat food, restoring hunger. Returns actual hunger restored.</summary>
        public float Eat(float restoreAmount)
        {
            float before = Hunger;
            Hunger += restoreAmount;
            if (Hunger > GameConstants.MaxHunger)
                Hunger = GameConstants.MaxHunger;
            return Hunger - before;
        }

        // For testing and save/load
        public void SetHealth(float value) => Health = System.Math.Max(0, System.Math.Min(GameConstants.MaxHealth, value));
        public void SetHunger(float value) => Hunger = System.Math.Max(0, System.Math.Min(GameConstants.MaxHunger, value));
    }
}
```

- [ ] **Step 4.2: Create VitalitySystem.cs**

```csharp
// Assets/Scripts/Player/VitalitySystem.cs
using UnityEngine;
using DeenCraft;

namespace DeenCraft.Player
{
    /// <summary>
    /// MonoBehaviour wrapper for VitalityData.
    /// Attach to the Player GameObject.
    /// </summary>
    public class VitalitySystem : MonoBehaviour
    {
        public VitalityData Data { get; } = new VitalityData();

        private PlayerController _controller;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            bool isMoving = _controller != null && _controller.IsMoving;
            Data.Tick(Time.deltaTime, isMoving);
        }

        public void TakeDamage(float amount) => Data.TakeDamage(amount);
        public void Heal(float amount)        => Data.Heal(amount);

        /// <summary>Call when player eats a food item.</summary>
        public void Eat(ItemId foodItem)
        {
            float restore = GetFoodRestoreAmount(foodItem);
            Data.Eat(restore);
        }

        private float GetFoodRestoreAmount(ItemId foodItem)
        {
            switch (foodItem)
            {
                case ItemId.Bread:   return 5f;
                case ItemId.Date:    return 3f;
                case ItemId.Fig:     return 2f;
                case ItemId.Falafel: return 6f;
                default:             return 0f;
            }
        }

        public float Health => Data.Health;
        public float Hunger => Data.Hunger;
        public bool  IsAlive => Data.IsAlive;
    }
}
```

- [ ] **Step 4.3: Commit**

```bash
git add Assets/Scripts/Player/VitalityData.cs Assets/Scripts/Player/VitalitySystem.cs
git commit -m "feat(player): add VitalityData and VitalitySystem"
```

---

## Task 5: Vitality unit tests

**Files:**
- Create: `Assets/Tests/EditMode/PlayerTests/VitalityTests.cs`

- [ ] **Step 5.1: Create VitalityTests.cs**

```csharp
// Assets/Tests/EditMode/PlayerTests/VitalityTests.cs
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft;

namespace DeenCraft.Tests.EditMode
{
    public class VitalityTests
    {
        [Test]
        public void TakeDamage_ReducesHealth()
        {
            var v = new VitalityData();
            v.TakeDamage(5f);
            Assert.AreEqual(GameConstants.MaxHealth - 5f, v.Health, 0.001f);
        }

        [Test]
        public void TakeDamage_KillsPlayerWhenHealthReachesZero()
        {
            var v = new VitalityData();
            v.TakeDamage(GameConstants.MaxHealth + 1f);
            Assert.IsFalse(v.IsAlive);
            Assert.AreEqual(0f, v.Health, 0.001f);
        }

        [Test]
        public void Heal_IncreasesHealth_ClampedToMax()
        {
            var v = new VitalityData();
            v.TakeDamage(5f);
            v.Heal(10f);
            Assert.AreEqual(GameConstants.MaxHealth, v.Health, 0.001f);
        }

        [Test]
        public void Tick_DrainsHunger_WhenMoving()
        {
            var v = new VitalityData();
            float before = v.Hunger;
            v.Tick(1f, isMoving: true);
            // Moving doubles drain rate: 0.05 * 2 * 1 = 0.1
            Assert.AreEqual(before - GameConstants.HungerDrainRate * 2f, v.Hunger, 0.001f);
        }

        [Test]
        public void Tick_DrainsHunger_WhenNotMoving()
        {
            var v = new VitalityData();
            float before = v.Hunger;
            v.Tick(1f, isMoving: false);
            Assert.AreEqual(before - GameConstants.HungerDrainRate, v.Hunger, 0.001f);
        }

        [Test]
        public void Tick_CausesStarvationDamage_WhenHungerIsZero()
        {
            var v = new VitalityData();
            v.SetHunger(0f);
            v.Tick(2f, isMoving: false);
            // 0.5 damage/s * 2s = 1 damage
            Assert.AreEqual(GameConstants.MaxHealth - 1f, v.Health, 0.001f);
        }

        [Test]
        public void Tick_RegenHealth_WhenHungerAboveThreshold()
        {
            var v = new VitalityData();
            v.TakeDamage(5f);
            v.SetHunger(GameConstants.HungerRegenThresh); // exactly at threshold
            float healthBefore = v.Health;
            v.Tick(1f, isMoving: false);
            Assert.Greater(v.Health, healthBefore);
        }

        [Test]
        public void Eat_RestoresHunger_ClampedToMax()
        {
            var v = new VitalityData();
            v.SetHunger(18f);
            v.Eat(10f); // would be 28 but max is 20
            Assert.AreEqual(GameConstants.MaxHunger, v.Hunger, 0.001f);
        }
    }
}
```

- [ ] **Step 5.2: Commit**

```bash
git add Assets/Tests/EditMode/PlayerTests/VitalityTests.cs
git commit -m "test(player): vitality unit tests (8 cases)"
```

---

## Task 6: CharacterAppearance data + CharacterCustomizer

**Files:**
- Create: `Assets/Scripts/Player/CharacterAppearance.cs`
- Create: `Assets/Scripts/Player/CharacterCustomizer.cs`

- [ ] **Step 6.1: Create CharacterAppearance.cs**

```csharp
// Assets/Scripts/Player/CharacterAppearance.cs
using UnityEngine;

namespace DeenCraft.Player
{
    public enum SkinTone
    {
        Light,
        MediumLight,
        Medium,
        MediumDark,
        Dark
    }

    public enum HeadwearType
    {
        None,
        Hijab,
        Kufi
    }

    public enum ClothingStyle
    {
        Casual,
        Traditional,
        Winter
    }

    [System.Serializable]
    public class CharacterAppearance
    {
        public SkinTone      SkinTone      = SkinTone.Medium;
        public HeadwearType  HeadwearType  = HeadwearType.None;
        public ClothingStyle ClothingStyle = ClothingStyle.Casual;
        public Color         ClothingColor = Color.white;

        /// <summary>Returns the Unity Color matching this skin tone.</summary>
        public Color GetSkinColor()
        {
            switch (SkinTone)
            {
                case SkinTone.Light:       return new Color(1.00f, 0.88f, 0.77f);
                case SkinTone.MediumLight: return new Color(0.93f, 0.76f, 0.62f);
                case SkinTone.Medium:      return new Color(0.82f, 0.62f, 0.45f);
                case SkinTone.MediumDark:  return new Color(0.60f, 0.40f, 0.25f);
                case SkinTone.Dark:        return new Color(0.35f, 0.22f, 0.13f);
                default:                   return Color.white;
            }
        }
    }
}
```

- [ ] **Step 6.2: Create CharacterCustomizer.cs**

```csharp
// Assets/Scripts/Player/CharacterCustomizer.cs
using UnityEngine;

namespace DeenCraft.Player
{
    /// <summary>
    /// Applies CharacterAppearance to the player's SkinnedMeshRenderer.
    /// Saves/loads via PlayerPrefs using JsonUtility.
    /// Attach to the Player root GameObject.
    /// </summary>
    public class CharacterCustomizer : MonoBehaviour
    {
        private const string PrefsKey = "PlayerAppearance";

        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _headwearRenderer;

        public CharacterAppearance Appearance { get; private set; } = new CharacterAppearance();

        private void Awake()
        {
            LoadFromPrefs();
        }

        /// <summary>Apply current Appearance to renderers.</summary>
        public void Apply()
        {
            if (_bodyRenderer != null)
                _bodyRenderer.material.color = Appearance.GetSkinColor();

            if (_headwearRenderer != null)
            {
                _headwearRenderer.enabled = Appearance.HeadwearType != HeadwearType.None;
                _headwearRenderer.material.color = Appearance.ClothingColor;
            }
        }

        public void SetAppearance(CharacterAppearance appearance)
        {
            Appearance = appearance;
            Apply();
        }

        public void SaveToPrefs()
        {
            string json = JsonUtility.ToJson(Appearance);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        public void LoadFromPrefs()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                string json = PlayerPrefs.GetString(PrefsKey);
                Appearance = JsonUtility.FromJson<CharacterAppearance>(json) ?? new CharacterAppearance();
            }
            Apply();
        }

        public bool HasSavedAppearance() => PlayerPrefs.HasKey(PrefsKey);
    }
}
```

- [ ] **Step 6.3: Commit**

```bash
git add Assets/Scripts/Player/CharacterAppearance.cs \
        Assets/Scripts/Player/CharacterCustomizer.cs
git commit -m "feat(player): add CharacterAppearance and CharacterCustomizer"
```

---

## Task 7: PlayerAnimationState enum + PlayerAnimator

**Files:**
- Create: `Assets/Scripts/Player/PlayerAnimationState.cs`
- Create: `Assets/Scripts/Player/PlayerAnimator.cs`

- [ ] **Step 7.1: Create PlayerAnimationState.cs**

```csharp
// Assets/Scripts/Player/PlayerAnimationState.cs
namespace DeenCraft.Player
{
    public enum PlayerAnimationState
    {
        Idle  = 0,
        Walk  = 1,
        Run   = 2,
        Jump  = 3,
        Swim  = 4,
        Mine  = 5,
        Place = 6,
    }
}
```

- [ ] **Step 7.2: Create PlayerAnimator.cs**

```csharp
// Assets/Scripts/Player/PlayerAnimator.cs
using UnityEngine;

namespace DeenCraft.Player
{
    /// <summary>
    /// Reads PlayerController state and drives Animator parameters.
    /// No-ops gracefully if Animator component is absent (capsule placeholder case).
    /// 
    /// Animator parameters expected:
    ///   float  Speed       (0=idle, 1=walk, 2=run)
    ///   bool   IsJumping
    ///   bool   IsSwimming
    ///   int    ActionState (PlayerAnimationState cast to int)
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash       = Animator.StringToHash("Speed");
        private static readonly int IsJumpingHash   = Animator.StringToHash("IsJumping");
        private static readonly int IsSwimmingHash  = Animator.StringToHash("IsSwimming");
        private static readonly int ActionStateHash = Animator.StringToHash("ActionState");

        private Animator         _animator;
        private PlayerController _controller;

        private void Awake()
        {
            _animator   = GetComponent<Animator>(); // null if no Animator — that's fine
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_animator == null) return;

            float speed = 0f;
            if      (_controller.IsSprinting) speed = 2f;
            else if (_controller.IsMoving)    speed = 1f;

            _animator.SetFloat(SpeedHash,       speed);
            _animator.SetBool(IsJumpingHash,    _controller.IsJumping);
            _animator.SetBool(IsSwimmingHash,   _controller.IsSwimming);
            _animator.SetInteger(ActionStateHash, (int)_controller.CurrentAnimationState);
        }
    }
}
```

- [ ] **Step 7.3: Commit**

```bash
git add Assets/Scripts/Player/PlayerAnimationState.cs \
        Assets/Scripts/Player/PlayerAnimator.cs
git commit -m "feat(player): add PlayerAnimationState enum and PlayerAnimator"
```

---

## Task 8: PlayerController

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`

> **Note:** PlayerController requires access to WorldGenerator to detect Water blocks. The Player asmdef already references DeenCraft.World, so `BlockType` and world query APIs are available.

- [ ] **Step 8.1: Create PlayerController.cs**

```csharp
// Assets/Scripts/Player/PlayerController.cs
using UnityEngine;
using DeenCraft;
using DeenCraft.World;

namespace DeenCraft.Player
{
    /// <summary>
    /// Third-person CharacterController locomotion.
    /// - WASD: move in yaw-relative direction
    /// - Mouse X: rotate player body (yaw)
    /// - Mouse Y: tilt third-person camera (pitch)
    /// - Space: jump (when grounded or swimming up)
    /// - Left Shift: sprint
    /// - Left Ctrl while swimming: swim down
    /// - Water block at chest height triggers swim mode
    /// 
    /// Camera child object named "PlayerCamera" is positioned at (0, 1.6, -4).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private Transform _cameraTarget; // "PlayerCamera" child

        // ── Constants ────────────────────────────────────────────────────────
        private const float Gravity        = -20f;
        private const float SwimGravity    = -2f;
        private const float GroundedBump   = -2f;
        private const float MinPitch       = -80f;
        private const float MaxPitch       =  60f;

        // ── State ─────────────────────────────────────────────────────────────
        private CharacterController _cc;
        private ChunkManager        _chunkManager;

        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;

        public bool IsMoving   { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsJumping  { get; private set; }
        public bool IsSwimming { get; private set; }
        public PlayerAnimationState CurrentAnimationState { get; private set; } = PlayerAnimationState.Idle;

        // ── Unity ────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cc           = GetComponent<CharacterController>();
            _chunkManager = FindObjectOfType<ChunkManager>();

            // Create camera if not assigned
            if (_cameraTarget == null)
            {
                var camGo = new GameObject("PlayerCamera");
                camGo.transform.SetParent(transform);
                camGo.transform.localPosition = new Vector3(0f, 1.6f, -4f);
                var camComp = camGo.AddComponent<Camera>();
                camComp.tag = "MainCamera";
                _cameraTarget = camGo.transform;
            }
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
            UpdateAnimationState();
        }

        // ── Mouse Look ───────────────────────────────────────────────────────
        private void HandleMouseLook()
        {
            _yaw   += Input.GetAxis("Mouse X") * 2f;
            _pitch -= Input.GetAxis("Mouse Y") * 2f;
            _pitch  = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            if (_cameraTarget != null)
            {
                _cameraTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                _cameraTarget.localPosition = new Vector3(0f, 1.6f, -4f);
                // Make camera look at player head
                _cameraTarget.LookAt(transform.position + Vector3.up * 1.4f);
            }
        }

        // ── Movement ─────────────────────────────────────────────────────────
        private void HandleMovement()
        {
            bool grounded  = _cc.isGrounded;
            IsSwimming = IsInWater();

            float moveSpeed;
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && !IsSwimming;
            moveSpeed   = IsSprinting
                ? GameConstants.PlayerSprintSpeed
                : IsSwimming
                    ? GameConstants.PlayerSwimSpeed
                    : GameConstants.PlayerMoveSpeed;

            // Horizontal movement
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = transform.forward * v + transform.right * h;
            IsMoving = move.sqrMagnitude > 0.01f;

            // Vertical
            if (IsSwimming)
            {
                _verticalVelocity = 0f;
                if (Input.GetKey(KeyCode.Space))    _verticalVelocity =  GameConstants.PlayerSwimSpeed;
                if (Input.GetKey(KeyCode.LeftControl)) _verticalVelocity = -GameConstants.PlayerSwimSpeed;
            }
            else
            {
                if (grounded)
                {
                    IsJumping = false;
                    if (_verticalVelocity < 0f) _verticalVelocity = GroundedBump;
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        _verticalVelocity = GameConstants.PlayerJumpForce;
                        IsJumping = true;
                    }
                }
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 velocity = move * moveSpeed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
        }

        private bool IsInWater()
        {
            if (_chunkManager == null) return false;
            Vector3 checkPos = transform.position + Vector3.up * 0.5f;
            BlockType block = _chunkManager.GetBlock(
                Mathf.FloorToInt(checkPos.x),
                Mathf.FloorToInt(checkPos.y),
                Mathf.FloorToInt(checkPos.z));
            return block == BlockType.Water;
        }

        // ── Animation State ──────────────────────────────────────────────────
        private void UpdateAnimationState()
        {
            if      (IsSwimming)  CurrentAnimationState = PlayerAnimationState.Swim;
            else if (IsJumping)   CurrentAnimationState = PlayerAnimationState.Jump;
            else if (IsSprinting) CurrentAnimationState = PlayerAnimationState.Run;
            else if (IsMoving)    CurrentAnimationState = PlayerAnimationState.Walk;
            else                  CurrentAnimationState = PlayerAnimationState.Idle;
        }
    }
}
```

- [ ] **Step 8.2: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs
git commit -m "feat(player): add PlayerController (CharacterController, third-person)"
```

---

## Task 9: Crafting assembly — asmdef update + CraftingRecipe + RecipeDatabase

**Files:**
- Modify: `Assets/Scripts/Crafting/Crafting.asmdef` (add DeenCraft.World reference so recipes can reference BlockType-sourced ItemIds)
- Create: `Assets/Scripts/Crafting/CraftingRecipe.cs`
- Create: `Assets/Scripts/Crafting/RecipeDatabase.cs`

- [ ] **Step 9.1: Update Crafting.asmdef**

Replace `Assets/Scripts/Crafting/Crafting.asmdef` content with:

```json
{
    "name": "DeenCraft.Crafting",
    "rootNamespace": "DeenCraft.Crafting",
    "references": [
        "DeenCraft.Core",
        "DeenCraft.World",
        "DeenCraft.Player"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 9.2: Create CraftingRecipe.cs**

```csharp
// Assets/Scripts/Crafting/CraftingRecipe.cs
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// Describes one crafting recipe.
    /// Ingredients is a flat 9-element array representing the 3x3 grid (row-major, left-to-right, top-to-bottom).
    /// ItemId.None means "any / empty" in a shapeless recipe.
    /// </summary>
    public class CraftingRecipe
    {
        public string    RecipeId     { get; }
        public ItemId[]  Ingredients  { get; } // length 9
        public ItemStack Result       { get; }
        public bool      IsShapeless  { get; }

        public CraftingRecipe(string recipeId, ItemId[] ingredients, ItemStack result, bool shapeless = true)
        {
            RecipeId    = recipeId;
            Ingredients = ingredients;
            Result      = result;
            IsShapeless = shapeless;
        }
    }
}
```

- [ ] **Step 9.3: Create RecipeDatabase.cs**

```csharp
// Assets/Scripts/Crafting/RecipeDatabase.cs
using System.Collections.Generic;
using System.Linq;
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// Hardcoded recipe registry for Phase 4.
    /// All recipes are shapeless: match by ingredient counts regardless of slot position.
    /// FindMatch returns the first matching recipe or null.
    /// </summary>
    public static class RecipeDatabase
    {
        private static readonly List<CraftingRecipe> _recipes = new List<CraftingRecipe>();

        static RecipeDatabase()
        {
            RegisterRecipes();
        }

        private static void RegisterRecipes()
        {
            // Stick: 2x Wood → 4x Stick
            _recipes.Add(new CraftingRecipe(
                "stick",
                new ItemId[] { ItemId.Wood, ItemId.Wood, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.Stick, 4),
                shapeless: true));

            // Wood Pickaxe: 3x Wood + 2x Stick → 1x WoodPickaxe
            _recipes.Add(new CraftingRecipe(
                "wood_pickaxe",
                new ItemId[] { ItemId.Wood, ItemId.Wood, ItemId.Wood, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.WoodPickaxe, 1),
                shapeless: true));

            // Wood Axe: 2x Wood + 2x Stick → 1x WoodAxe
            _recipes.Add(new CraftingRecipe(
                "wood_axe",
                new ItemId[] { ItemId.Wood, ItemId.Wood, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.WoodAxe, 1),
                shapeless: true));

            // Wood Shovel: 1x Wood + 2x Stick → 1x WoodShovel
            _recipes.Add(new CraftingRecipe(
                "wood_shovel",
                new ItemId[] { ItemId.Wood, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.WoodShovel, 1),
                shapeless: true));

            // Stone Pickaxe: 3x Stone + 2x Stick → 1x StonePickaxe
            _recipes.Add(new CraftingRecipe(
                "stone_pickaxe",
                new ItemId[] { ItemId.Stone, ItemId.Stone, ItemId.Stone, ItemId.Stick, ItemId.Stick, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.StonePickaxe, 1),
                shapeless: true));

            // Bread: 3x Wheat → 1x Bread
            _recipes.Add(new CraftingRecipe(
                "bread",
                new ItemId[] { ItemId.Wheat, ItemId.Wheat, ItemId.Wheat, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None, ItemId.None },
                new ItemStack(ItemId.Bread, 1),
                shapeless: true));
        }

        /// <summary>
        /// Find a recipe matching the given 9-slot grid (shapeless matching).
        /// Returns null if no match found.
        /// </summary>
        public static CraftingRecipe FindMatch(ItemStack[] grid)
        {
            if (grid == null || grid.Length != 9) return null;

            // Build a count dictionary of non-empty slots in the grid
            var gridCounts = new Dictionary<ItemId, int>();
            foreach (var slot in grid)
            {
                if (!slot.IsEmpty)
                {
                    if (!gridCounts.ContainsKey(slot.ItemId)) gridCounts[slot.ItemId] = 0;
                    gridCounts[slot.ItemId] += slot.Count;
                }
            }

            foreach (var recipe in _recipes)
            {
                if (recipe.IsShapeless && MatchesShapeless(gridCounts, recipe.Ingredients))
                    return recipe;
            }
            return null;
        }

        private static bool MatchesShapeless(Dictionary<ItemId, int> gridCounts, ItemId[] recipeIngredients)
        {
            // Build required ingredient counts from recipe
            var required = new Dictionary<ItemId, int>();
            foreach (var id in recipeIngredients)
            {
                if (id == ItemId.None) continue;
                if (!required.ContainsKey(id)) required[id] = 0;
                required[id]++;
            }

            if (required.Count != gridCounts.Count) return false;
            foreach (var kv in required)
            {
                if (!gridCounts.ContainsKey(kv.Key)) return false;
                if (gridCounts[kv.Key] != kv.Value)  return false;
            }
            return true;
        }

        public static IReadOnlyList<CraftingRecipe> AllRecipes => _recipes.AsReadOnly();
    }
}
```

- [ ] **Step 9.4: Commit**

```bash
git add Assets/Scripts/Crafting/Crafting.asmdef \
        Assets/Scripts/Crafting/CraftingRecipe.cs \
        Assets/Scripts/Crafting/RecipeDatabase.cs
git commit -m "feat(crafting): add CraftingRecipe and RecipeDatabase (6 recipes)"
```

---

## Task 10: CraftingGrid + CraftingSystem

**Files:**
- Create: `Assets/Scripts/Crafting/CraftingGrid.cs`
- Create: `Assets/Scripts/Crafting/CraftingSystem.cs`

- [ ] **Step 10.1: Create CraftingGrid.cs**

```csharp
// Assets/Scripts/Crafting/CraftingGrid.cs
using DeenCraft;
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// Pure-C# 3x3 crafting grid model.
    /// Call TryCraft(inventory) to consume ingredients and return the crafted item.
    /// </summary>
    public class CraftingGrid
    {
        private const int GridSize = GameConstants.CraftingGridSize; // 3

        private readonly ItemStack[] _slots = new ItemStack[GridSize * GridSize];

        public CraftingGrid()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = ItemStack.Empty;
        }

        /// <summary>Set a grid slot (row 0-2, col 0-2).</summary>
        public void SetSlot(int row, int col, ItemStack stack)
        {
            _slots[row * GridSize + col] = stack;
        }

        /// <summary>Get a grid slot (row 0-2, col 0-2).</summary>
        public ItemStack GetSlot(int row, int col) => _slots[row * GridSize + col];

        /// <summary>Returns the result preview without consuming anything. Null if no match.</summary>
        public ItemStack? Preview()
        {
            var recipe = RecipeDatabase.FindMatch(_slots);
            return recipe?.Result;
        }

        /// <summary>
        /// If a matching recipe exists and the inventory has room for the result,
        /// consume the ingredients from the grid and add the result to inventory.
        /// Returns the crafted ItemStack, or ItemStack.Empty if crafting failed.
        /// </summary>
        public ItemStack TryCraft(Inventory inventory)
        {
            if (inventory == null) return ItemStack.Empty;

            var recipe = RecipeDatabase.FindMatch(_slots);
            if (recipe == null) return ItemStack.Empty;

            // Check inventory has room
            // (AddItem returns overflow; if overflow > 0 don't craft)
            // We check by simulating — instead just attempt; AddItem returns overflow
            int overflow = SimulateAdd(inventory, recipe.Result);
            if (overflow > 0) return ItemStack.Empty;

            // Consume ingredients from grid
            ConsumeIngredients(recipe.Ingredients);

            // Add result to inventory
            inventory.AddItem(recipe.Result);
            return recipe.Result;
        }

        private int SimulateAdd(Inventory inventory, ItemStack result)
        {
            // Count available space: existing stack space + empty slots
            int existingSpace = 0;
            int emptySlots    = 0;

            for (int i = 0; i < GameConstants.HotbarSlots; i++)
            {
                var slot = inventory.GetHotbarSlot(i);
                if (slot.IsEmpty) emptySlots++;
                else if (slot.ItemId == result.ItemId)
                    existingSpace += GameConstants.MaxStackSize - slot.Count;
            }
            for (int i = 0; i < GameConstants.BackpackSlots; i++)
            {
                var slot = inventory.GetBackpackSlot(i);
                if (slot.IsEmpty) emptySlots++;
                else if (slot.ItemId == result.ItemId)
                    existingSpace += GameConstants.MaxStackSize - slot.Count;
            }
            int totalSpace = existingSpace + emptySlots * GameConstants.MaxStackSize;
            return System.Math.Max(0, result.Count - totalSpace);
        }

        private void ConsumeIngredients(ItemId[] recipeIngredients)
        {
            // Build required counts
            var required = new System.Collections.Generic.Dictionary<ItemId, int>();
            foreach (var id in recipeIngredients)
            {
                if (id == ItemId.None) continue;
                if (!required.ContainsKey(id)) required[id] = 0;
                required[id]++;
            }

            // Consume from grid slots
            foreach (var kv in required)
            {
                int remaining = kv.Value;
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    if (_slots[i].ItemId == kv.Key)
                    {
                        int take = System.Math.Min(_slots[i].Count, remaining);
                        int left = _slots[i].Count - take;
                        _slots[i] = left > 0 ? new ItemStack(kv.Key, left) : ItemStack.Empty;
                        remaining -= take;
                    }
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = ItemStack.Empty;
        }
    }
}
```

- [ ] **Step 10.2: Create CraftingSystem.cs**

```csharp
// Assets/Scripts/Crafting/CraftingSystem.cs
using UnityEngine;
using DeenCraft.Player;

namespace DeenCraft.Crafting
{
    /// <summary>
    /// MonoBehaviour wrapper for CraftingGrid.
    /// Attach to the Player or a UI Canvas root.
    /// UI scripts call SetSlot/TryCraft/GetPreview.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public CraftingGrid Grid { get; } = new CraftingGrid();

        private Inventory _inventory;

        private void Awake()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                _inventory = player.GetComponent<InventoryHolder>()?.Inventory;
        }

        public ItemStack GetPreview() => Grid.Preview() ?? ItemStack.Empty;

        public ItemStack TryCraft()
        {
            if (_inventory == null) return ItemStack.Empty;
            return Grid.TryCraft(_inventory);
        }

        public void SetSlot(int row, int col, ItemStack stack) => Grid.SetSlot(row, col, stack);
        public ItemStack GetSlot(int row, int col)              => Grid.GetSlot(row, col);
        public void Clear()                                     => Grid.Clear();

        public void SetInventory(Inventory inventory) => _inventory = inventory;
    }
}
```

- [ ] **Step 10.3: Commit**

```bash
git add Assets/Scripts/Crafting/CraftingGrid.cs \
        Assets/Scripts/Crafting/CraftingSystem.cs
git commit -m "feat(crafting): add CraftingGrid and CraftingSystem"
```

---

## Task 11: InventoryHolder (bridge MonoBehaviour)

The `CraftingSystem` and `VitalitySystem` both need a reference to the player's `Inventory`. A thin MonoBehaviour bridges the pure-C# `Inventory` to the scene.

**Files:**
- Create: `Assets/Scripts/Player/InventoryHolder.cs`

- [ ] **Step 11.1: Create InventoryHolder.cs**

```csharp
// Assets/Scripts/Player/InventoryHolder.cs
using UnityEngine;

namespace DeenCraft.Player
{
    /// <summary>
    /// Holds the player's Inventory instance on the scene GameObject.
    /// Other MonoBehaviours (CraftingSystem, VitalitySystem) fetch this via GetComponent.
    /// </summary>
    public class InventoryHolder : MonoBehaviour
    {
        public Inventory Inventory { get; } = new Inventory();
    }
}
```

- [ ] **Step 11.2: Commit**

```bash
git add Assets/Scripts/Player/InventoryHolder.cs
git commit -m "feat(player): add InventoryHolder MonoBehaviour bridge"
```

---

## Task 12: Crafting unit tests

**Files:**
- Create: `Assets/Tests/EditMode/CraftingTests/RecipeDatabaseTests.cs`
- Create: `Assets/Tests/EditMode/CraftingTests/CraftingGridTests.cs`

- [ ] **Step 12.1: Create RecipeDatabaseTests.cs**

```csharp
// Assets/Tests/EditMode/CraftingTests/RecipeDatabaseTests.cs
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft.Crafting;

namespace DeenCraft.Tests.EditMode
{
    public class RecipeDatabaseTests
    {
        private static ItemStack[] MakeGrid(params (ItemId id, int count)[] items)
        {
            var grid = new ItemStack[9];
            for (int i = 0; i < 9; i++) grid[i] = ItemStack.Empty;
            for (int i = 0; i < items.Length && i < 9; i++)
                grid[i] = new ItemStack(items[i].id, items[i].count);
            return grid;
        }

        [Test]
        public void FindMatch_Stick_ReturnsStickRecipe()
        {
            var grid = MakeGrid((ItemId.Wood, 1), (ItemId.Wood, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("stick", result.RecipeId);
            Assert.AreEqual(4, result.Result.Count);
        }

        [Test]
        public void FindMatch_WoodPickaxe_ReturnsCorrectRecipe()
        {
            var grid = MakeGrid(
                (ItemId.Wood, 1), (ItemId.Wood, 1), (ItemId.Wood, 1),
                (ItemId.Stick, 1), (ItemId.Stick, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("wood_pickaxe", result.RecipeId);
        }

        [Test]
        public void FindMatch_StonePickaxe_ReturnsCorrectRecipe()
        {
            var grid = MakeGrid(
                (ItemId.Stone, 1), (ItemId.Stone, 1), (ItemId.Stone, 1),
                (ItemId.Stick, 1), (ItemId.Stick, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("stone_pickaxe", result.RecipeId);
        }

        [Test]
        public void FindMatch_Bread_ReturnsCorrectRecipe()
        {
            var grid = MakeGrid((ItemId.Wheat, 1), (ItemId.Wheat, 1), (ItemId.Wheat, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNotNull(result);
            Assert.AreEqual("bread", result.RecipeId);
        }

        [Test]
        public void FindMatch_UnknownCombo_ReturnsNull()
        {
            var grid = MakeGrid((ItemId.Dirt, 1), (ItemId.Sand, 1));
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNull(result);
        }

        [Test]
        public void FindMatch_EmptyGrid_ReturnsNull()
        {
            var grid = new ItemStack[9];
            for (int i = 0; i < 9; i++) grid[i] = ItemStack.Empty;
            var result = RecipeDatabase.FindMatch(grid);
            Assert.IsNull(result);
        }
    }
}
```

- [ ] **Step 12.2: Create CraftingGridTests.cs**

```csharp
// Assets/Tests/EditMode/CraftingTests/CraftingGridTests.cs
using NUnit.Framework;
using DeenCraft.Player;
using DeenCraft.Crafting;

namespace DeenCraft.Tests.EditMode
{
    public class CraftingGridTests
    {
        [Test]
        public void TryCraft_ProducesResult_WhenRecipeMatches()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var inv = new Inventory();
            var result = grid.TryCraft(inv);

            Assert.AreEqual(ItemId.Stick, result.ItemId);
            Assert.AreEqual(4, result.Count);
        }

        [Test]
        public void TryCraft_RemovesIngredients_FromGrid()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 2));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var inv = new Inventory();
            grid.TryCraft(inv);

            // One Wood was in slot (0,1) (count 1), consumed. Slot (0,0) had 2, needs 1 more → 1 left.
            Assert.AreEqual(1, grid.GetSlot(0, 0).Count);
            Assert.IsTrue(grid.GetSlot(0, 1).IsEmpty);
        }

        [Test]
        public void TryCraft_AddsResultToInventory()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var inv = new Inventory();
            grid.TryCraft(inv);

            Assert.AreEqual(4, inv.CountItem(ItemId.Stick));
        }

        [Test]
        public void TryCraft_ReturnsEmpty_WhenNoRecipeMatches()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Sand, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Gravel, 1));

            var inv = new Inventory();
            var result = grid.TryCraft(inv);
            Assert.IsTrue(result.IsEmpty);
        }

        [Test]
        public void Preview_ReturnsResult_WithoutCrafting()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Wood, 1));
            grid.SetSlot(0, 1, new ItemStack(ItemId.Wood, 1));

            var preview = grid.Preview();
            Assert.IsNotNull(preview);
            Assert.AreEqual(ItemId.Stick, preview.Value.ItemId);
            // Grid unchanged
            Assert.IsFalse(grid.GetSlot(0, 0).IsEmpty);
        }

        [Test]
        public void Clear_EmptiesAllSlots()
        {
            var grid = new CraftingGrid();
            grid.SetSlot(0, 0, new ItemStack(ItemId.Stone, 5));
            grid.Clear();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    Assert.IsTrue(grid.GetSlot(r, c).IsEmpty);
        }
    }
}
```

- [ ] **Step 12.3: Commit**

```bash
git add Assets/Tests/EditMode/CraftingTests/RecipeDatabaseTests.cs \
        Assets/Tests/EditMode/CraftingTests/CraftingGridTests.cs
git commit -m "test(crafting): recipe database and crafting grid tests (12 cases)"
```

---

## Task 13: Final push

- [ ] **Step 13.1: Run all tests**

In Unity Test Runner (Window → General → Test Runner → EditMode), run all tests.  
Expected: All 32 new tests pass (12 inventory + 8 vitality + 6 recipe + 6 grid).

- [ ] **Step 13.2: Push to remote**

```bash
git push
echo "Phase 4 complete"
```

- [ ] **Step 13.3: Write phase_4_status.md**

Create `docs/phase_4_status.md`:

```markdown
# Phase 4 Status — Character System

**Completed:** 2026-04-28  
**Commits:** see git log from "feat(player): add ItemId enum and ItemStack struct"

## What Was Built

### DeenCraft.Player
- `ItemId` enum (block items 1-19, tools 100-105, ingredients 200-201, food 300-303)
- `ItemStack` struct (ItemId + count, IsEmpty, Empty sentinel)
- `Inventory` (hotbar[9] + backpack[27], AddItem with stack merging, RemoveItem, CountItem)
- `InventoryHolder` MonoBehaviour bridge
- `VitalityData` pure-C# class (health, hunger, Tick, TakeDamage, Heal, Eat)
- `VitalitySystem` MonoBehaviour wrapper (Update → Tick, food restore amounts)
- `CharacterAppearance` data class + SkinTone/HeadwearType/ClothingStyle enums
- `CharacterCustomizer` MonoBehaviour (Apply, Save/Load via PlayerPrefs)
- `PlayerAnimationState` enum (Idle, Walk, Run, Jump, Swim, Mine, Place)
- `PlayerAnimator` MonoBehaviour (drives Animator params, null-safe)
- `PlayerController` MonoBehaviour (CharacterController, WASD, mouse look, swim)

### DeenCraft.Crafting
- `CraftingRecipe` class (RecipeId, Ingredients[9], Result, IsShapeless)
- `RecipeDatabase` static class (6 recipes: Stick, WoodPickaxe, WoodAxe, WoodShovel, StonePickaxe, Bread)
- `CraftingGrid` pure-C# 3×3 grid (TryCraft, Preview, Clear)
- `CraftingSystem` MonoBehaviour wrapper

### Tests
- 32 new EditMode tests (12 inventory, 8 vitality, 6 recipe, 6 crafting grid)
- Cumulative total: 148 tests

## Art
- Placeholder only: Unity Capsule for player body. No rigged mesh, no animation clips.
- Real art wired in Phase 8.

## Phase 5 Notes
- UI panels needed: HUD (health/hunger bars, hotbar), inventory screen, crafting screen, character creation screen
- `CharacterCustomizer.HasSavedAppearance()` → show character creation on first login
- `VitalitySystem` food items need matching block-pick-up detection in PlayerController (block mining not in Phase 4)
- Block placement/breaking (left-click mine, right-click place) deferred to Phase 5
```

- [ ] **Step 13.4: Commit status doc**

```bash
git add docs/phase_4_status.md
git commit -m "docs: Phase 4 status document"
git push
echo "All done"
```

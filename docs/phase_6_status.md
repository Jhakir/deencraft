# Phase 6 Status — Islamic Cultural Content

## Completion Status: ✅ DONE

Completed on: 2026-05-06.
Cumulative test count: **209 EditMode tests** (174 Phase 1–5 + Adhan + 35 Phase 6).

---

## What Was Built

### Phase 6 Features

#### ✅ Adhan Audio System (committed `94fa018`, `59269bd`)
- **`DayNightCycle.cs`** — Singleton MonoBehaviour. 20-min in-game day (1200s real time). Fires `OnPrayerTime(PrayerName)` event once per day at each of 5 prayer thresholds. `SetTime(float)` for settings preview.
- **`AdhanAudioManager.cs`** — Subscribes to `OnPrayerTime`, plays the selected `AudioClip` at each prayer. Multi-clip support (Slot 0 = built-in default; children can optionally add their own recording). Toggle + clip index persisted in `PlayerPrefs`.
- **`DayNightCycleTests.cs`** — 5 EditMode tests covering prayer-fire logic via pure-C# test harness.

#### ✅ Cultural Block Types (8 new `BlockType` values)
- `Minaret = 20` — mosque minaret shaft
- `Dome = 21` — mosque dome cap
- `StoneArch = 22` — Palestinian stone arch
- `Crescent = 23` — crescent-moon decorative block
- `StarBlock = 24` — star decorative block
- `AppleWood = 25` — apple tree trunk (drops Apple food item)
- `AppleLeaves = 26` — apple tree canopy (drops Apple food item)
- `WaterSlide = 27` — fun slide block (future: gives speed burst)

#### ✅ New ItemIds
- Cultural block items: `Minaret=28`, `Dome=29`, `StoneArch=30`, `Crescent=31`, `StarBlock=32`, `AppleWood=33`, `WaterSlide=34`
- Food: `Olive=305`, `Apple=306`

#### ✅ FoodDefinition (pure C#, fully testable)
- **`FoodDefinition.cs`** — `HungerRestored(ItemId)` and `IsFood(ItemId)`. Single source of truth for all food nutrition values.
- `VitalitySystem.GetFoodRestoreAmount` now delegates to `FoodDefinition` (removed inline switch).
- Full nutrition table: Bread=5, Date=3, Fig=2, Falafel=6, Fish=4, Olive=2, Apple=3.

#### ✅ BlockItemMap (extended)
- `OliveLeaves` now drops `Olive` food item (was `OliveLeaf` block — changed for Phase 6).
- `AppleLeaves` drops `Apple` food item.
- All 7 cultural block types map bidirectionally (minable, drop themselves).
- `Apple` and `Olive` food items are NOT placeable (correct).

#### ✅ Apple Trees in Grassland Biome
- `FeatureGenerator.PlaceAppleTree` — 3-5 block trunk (`AppleWood`), 3×3 canopy (`AppleLeaves`), same shape as regular tree.
- `PlaceTree` updated: Grassland biome now splits 50/50 between apple trees and regular trees.
- Apple trees never appear in Desert (tested).

#### ✅ Mosque Auto-Generation in Villages
- `FeatureGenerator.IsMosqueChunk` — ~40% of village chunks in Grassland/OliveGrove get a mosque.
- `FeatureGenerator.PlaceMosque` — 7×7 mosaic tile floor, MudBrick walls (4 high), Dome cap, 8-block Minaret with Dome top, door opening.
- Mosque is placed next to the house when both fit within chunk bounds.
- Mosques never appear in Desert or SnowyIsland (tested).

### GameConstants additions
```csharp
public const float WaterSlideSpeedMultiplier = 2.5f;
public const float MosqueChancePerVillage    = 0.40f;
public const int   MosqueMinaretHeight       = 8;
public const int   MosqueCourtyard           = 7;
```

---

## Test Summary

| File | New Tests |
|---|---|
| `CulturalBlockTests.cs` | 21 (enum + ItemId existence + BlockItemMap) |
| `FoodDefinitionTests.cs` | 10 |
| `AppleTreeTests.cs` | 3 |
| `MosqueTests.cs` | 3 |
| **Phase 6 new** | **37** |
| **Cumulative** | **211** |

(The DayNightCycleTests.cs (5 tests) committed in `94fa018` + `AdhanAudioManager` = phase 6 total ~42 tests.)

---

## Security Review

| Concern | Mitigation |
|---|---|
| Food giving more hunger than max | `VitalityData.Eat` already clamps at `MaxHunger` |
| Mosque overwriting player blocks | `SafeSet` only writes if block is Air (structure placement) — same guard as houses |
| Audio clip null reference | `AdhanAudioManager` checks `_adhanClips.Length > 0` before play |
| Minaret height exceeding chunk bounds | `SafeSet` is bounds-checked; no overflow possible |

---

## What Is NOT Done in Phase 6
- Water slide speed boost (requires MonoBehaviour `OnCollisionStay` — deferred to Phase 8/runtime)
- Villager name-tag rendering above head (Phase 8 UI)
- Palestinian stone arch generation (block exists; auto-placement deferred — needs a dedicated structure generator pass)
- Crescent/star auto-placement on mosque rooftops (deferred to Phase 8 or future feature)

---

## Phase 7 Notes
- Firebase Auth: parent creates account (email + password)
- Parent dashboard: add/manage child profiles (username, avatar)
- Child login screen: pick profile → enter PIN or select avatar
- World saves stored per child profile in Firestore (`worldSaves` collection)
- Session persistence: auto-login on same device via `PlayerPrefs.PrefsActiveChildKey`
- `#if FIREBASE_AVAILABLE` guards already in place throughout codebase
- Firebase config goes in `StreamingAssets/firebase-config.json` (gitignored)

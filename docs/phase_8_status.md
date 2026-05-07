# Phase 8 Status — UI/UX

**Status:** ✅ Complete  
**Commit:** `50a3107`  
**Date:** 2025-05-07

---

## What Was Built

### Assembly definition
- `UI.asmdef` — added `DeenCraft.Player` and `DeenCraft.World` references so UI scripts can
  reach `VitalitySystem`, `Inventory`, `CharacterCustomizer`, `AdhanAudioManager`,
  `DayNightCycle`, and `WorldSaveManager`.

### Pure-C# testable layer (EditMode-testable)
| File | Purpose |
|---|---|
| `SettingsModel.cs` | PlayerPrefs-backed settings (MusicVolume, SfxVolume, GraphicsQuality). Clamped 0–1 volume setters. Keys prefixed `dc_`. |
| `HudViewModel.cs` | Static helpers: `HealthToHearts`, `HungerFraction`, `TimeLabel`. Pure functions, no state. |

### MonoBehaviour UI scripts
| File | Purpose |
|---|---|
| `HotbarSlotUI.cs` | Per-slot component: icon enable/disable + stack count label |
| `HudController.cs` | Polls VitalitySystem + Inventory every frame; drives hearts, hunger fill, hotbar, day label |
| `MainMenuController.cs` | Top-level menu panel; routes to child-profile, settings, credits panels |
| `ChildProfilePickerPanel.cs` | Lists child profiles; handles optional PIN entry; activates child via FirebaseAuthManager |
| `WorldSelectPanel.cs` | Lists + creates world saves via WorldSaveManager |
| `CharacterCustomizerPanel.cs` | Skin tone (5), headwear (3), clothing style (3), colour swatches; live preview; saves to PlayerPrefs + SessionManager.ActiveChild |
| `PauseMenuController.cs` | Escape key toggle; Time.timeScale pause; Save / SaveAndQuit / Settings buttons |
| `SettingsPanel.cs` | AudioMixer dB volume sliders; Adhan toggle → AdhanAudioManager; graphics → QualitySettings |

### Backend change
- `FirebaseAuthManager.cs` — added `ListWorldSavesAsync()` delegating to `_backend.ListWorldSavesAsync(parentUid, childId)`

### Tests added (EditMode)
| File | Count |
|---|---|
| `SettingsModelTests.cs` | 6 (defaults, save/load round-trips × 3, volume clamp × 2) |
| `HudViewModelTests.cs` | 7 (hearts × 4, hunger × 3, time label × 3) |

**Total EditMode tests: ~239**

---

## Known Limitations / Phase 9 Notes
- All UI scripts require manual Inspector wiring in the Unity editor — no prefabs created yet (prefab authoring is editor work outside code generation scope).
- `AudioMixer` asset ("MasterMixer") with exposed `MusicVolume` and `SFXVolume` parameters must be created in the editor and wired to `SettingsPanel`.
- `PauseMenuController.OnSaveAndQuitPressed` uses `SceneManager.LoadScene(0)` — the main-menu scene must be index 0 in Build Settings.
- No mobile-specific touch controls yet (planned for Phase 9 / post-launch iteration).

---

## Phase 9 — WebGL Build & Deployment

**Scope:**
1. Configure Unity WebGL build settings (IL2CPP, gzip compression, memory cap)
2. Wire Firebase Hosting + set up CI/CD deploy script
3. Verify < 10 s initial load on simulated throttled connection
4. Mobile browser testing (touch controls, viewport scaling)
5. Final QA pass: all 239+ tests green, no console errors in browser

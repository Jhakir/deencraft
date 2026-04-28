# Plan: Deencraft — Islamic Minecraft-Style Game

## Overview
Unity (C#) WebGL voxel sandbox game, Islamic/Palestine-inspired theme, single-player first.  
Parent-managed accounts, child logs in with username/avatar. Solo developer.

## Tech Stack
- **Engine:** Unity 2022 LTS (WebGL build target)
- **Backend:** Firebase (Auth + Firestore for world saves & accounts)
- **Hosting:** Firebase Hosting (serves WebGL build)
- **Packages:** Unity Terrain Tools, ProBuilder, Cinemachine, Unity Input System

---

## Phase 1: Project Setup
1. Create Unity 2022 LTS project, configure WebGL build settings
2. Set up Firebase project (Auth, Firestore, Hosting)
3. Integrate Firebase Unity SDK
4. Set up Git repo with `.gitignore` for Unity
5. Folder structure: `Assets/Scripts`, `Assets/Prefabs`, `Assets/Art`, `Assets/UI`, `Assets/Audio`

---

## Phase 2: Voxel Engine (Core)
1. Build chunk-based voxel world system (16×16×256 chunks, similar to Minecraft)
2. Block types: stone, dirt, grass, sand, snow, water, wood, leaves, mosaic/tile (mosque)
3. Mesh generation with greedy meshing for performance
4. Infinite world generation with Perlin noise
5. Biomes: desert, grassland, snowy island, olive grove, riverside/waterway
6. Block placement and destruction system
7. Physics: gravity for player, water flow simulation

---

## Phase 3: World Content
1. Terrain features: mountains, hills, flat plains, rivers, ocean/lake areas
2. Trees: olive trees, apple trees, palm trees, regular trees
3. Plants: flowers, moss, grass tufts, wheat
4. Structures: auto-generated villages with mud-brick houses
5. Village interiors: basic furniture blocks
6. Water bodies: oceans, rivers, lakes — with boat traversal
7. Snow island biome: ice blocks, snow-covered trees
8. Desert biome: sand dunes, cacti, oasis with palm trees

---

## Phase 4: Character System
1. Third-person player controller (WASD + mouse look)
2. Customizable character: skin tone, hijab/kufi options, clothing colors, outfit styles
3. Character creation screen at first login
4. Animation states: walk, run, swim, jump, mine, place, idle
5. Inventory system: hotbar (9 slots) + 27-slot backpack
6. Crafting system: crafting table, basic recipes (tools, blocks, food)
7. Health/hunger system (no death from mobs — hunger from not eating, restore by eating)

---

## Phase 5: Animals & Entities
1. Passive animals: horses (rideable), cats, chickens, cows, sheep (shearable for wool)
2. Animal AI: wander, flee when hurt, follow with food
3. Villagers: stand near village, open trade UI (offer diamonds/gold for items)
4. Trade UI: simple barter interface
5. Boats: craftable, float on water, player can board/steer

---

## Phase 6: Islamic Cultural Content
1. Mosques: auto-generated in some villages, buildable block set (minaret, dome blocks)
2. Olive and apple trees with harvestable fruit items
3. Water slides: special block type (fun for kids)
4. Islamic/Arabic name tags on villagers
5. Crescent moon and star decorative blocks
6. Palestinian-inspired architecture blocks: stone arches, tile patterns
7. In-game Adhan ambient sound (optional, toggleable by parent in settings)
8. Food items: dates, figs, olives, bread, falafel — restores hunger

---

## Phase 7: Auth & Account System
1. Firebase Auth: parent creates account with email + password
2. Parent dashboard (web page, separate from game): add/manage child profiles
3. Child profile: username, chosen avatar/character save, world save data
4. Child login screen in-game: pick profile → enter PIN or just select avatar
5. World saves stored in Firestore per child profile
6. Session persistence: auto-login on same device

---

## Phase 8: UI/UX
1. Main menu: logo, Play, Settings, Credits
2. Character customizer UI
3. HUD: hotbar, health hearts, hunger bar, compass
4. Pause menu: save & quit, settings
5. Settings: music volume, sound effects, toggle Adhan, graphics quality
6. Mobile-friendly UI scaling (for browser on tablets)
7. Kid-friendly fonts and color palette

---

## Phase 9: WebGL Build & Deployment
1. Configure Unity WebGL compression (Brotli)
2. Firebase Hosting setup, deploy WebGL build
3. Custom domain setup (deencraft.com or similar)
4. Loading screen with Islamic art/pattern
5. Test cross-browser: Chrome, Safari, Firefox, Edge

---

## Key Files/Folders (to be created)
| File | Purpose |
|---|---|
| `Assets/Scripts/World/ChunkManager.cs` | Chunk generation and pooling |
| `Assets/Scripts/World/WorldGenerator.cs` | Perlin noise biome generation |
| `Assets/Scripts/Player/PlayerController.cs` | Movement, mining, placing |
| `Assets/Scripts/Player/CharacterCustomizer.cs` | Character save/load |
| `Assets/Scripts/Inventory/InventorySystem.cs` | Inventory & hotbar |
| `Assets/Scripts/Animals/AnimalAI.cs` | Passive animal behaviour |
| `Assets/Scripts/Villager/TradeSystem.cs` | Villager trade UI logic |
| `Assets/Scripts/Auth/FirebaseAuthManager.cs` | Auth & session management |
| `Assets/Scripts/UI/` | All UI controllers |

---

## Verification Checklist
- [ ] WebGL build loads in Chrome under 10s on average connection
- [ ] World generates without framerate drops below 30fps in browser
- [ ] Parent can create account, add child profile, child can log in
- [ ] Child can mine, place blocks, craft, eat food, ride a horse, use a boat
- [ ] Character customization saves and persists across sessions
- [ ] Village with tradeable villager spawns correctly in the world
- [ ] All biomes generate: desert, snowy island, grassland, riverside, olive grove

---

## Design Decisions
- **No monsters/hostile mobs** — peaceful-only world
- **Unity WebGL** for browser-first (no install required)
- **Firebase** for auth + saves (scalable, generous free tier)
- **Multiplayer deferred to v2** (add Photon or Firebase Realtime DB later)
- Game is for **Muslim and non-Muslim kids** — culturally immersive but inclusive
- **Palestine-inspired aesthetic** throughout (olive trees, stone arches, tile patterns)

---

## Out of Scope (v1)
- Multiplayer
- Mobile native apps (iOS/Android)
- In-app purchases
- Modding support

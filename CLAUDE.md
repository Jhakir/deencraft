# Deencraft — Claude Instructions

## Project Overview
Deencraft is a Minecraft-style 3D voxel sandbox game with an Islamic/Palestine-inspired theme, built for Muslim and non-Muslim children. It is peaceful (no hostile mobs), culturally immersive, and browser-playable via Unity WebGL.

## Tech Stack
- **Engine:** Unity 2022 LTS (C#), WebGL build target
- **Backend:** Firebase (Authentication, Firestore, Hosting)
- **Auth model:** Parent creates account → manages child profiles → child logs in with username/avatar
- **Networking:** Single-player only (v1). Multiplayer via Photon or Firebase Realtime DB is planned for v2.

## Project Structure
```
Assets/
  Scripts/
    World/        # ChunkManager, WorldGenerator, BiomeSystem
    Player/       # PlayerController, CharacterCustomizer, Inventory
    Animals/      # AnimalAI, HorseController
    Villager/     # VillagerAI, TradeSystem
    Auth/         # FirebaseAuthManager, SessionManager
    UI/           # All UI panel controllers
    Crafting/     # CraftingSystem, RecipeDatabase
  Prefabs/
  Art/
  Audio/
docs/             # Project docs and planning
.github/          # Copilot instructions and prompts
```

## Core Game Design Rules
- **No hostile mobs** — world is peaceful; only passive animals exist
- **Passive animals:** horses (rideable), cats, chickens, cows, sheep (shearable)
- **Biomes:** desert, grassland, snowy island, olive grove, riverside
- **Cultural content:** mosques, olive/apple trees, Palestinian stone arches, crescent/star decor, date/fig/falafel foods
- **Character customizer:** skin tone, hijab/kufi, clothing color and style
- **Villagers:** trade with diamonds and gold, have Arabic/Islamic names

## Code Conventions
- **C# style:** PascalCase for classes and methods, camelCase for private fields with underscore prefix (`_chunkSize`)
- **Unity patterns:** Use `[SerializeField]` instead of `public` for inspector-exposed fields
- **No magic numbers:** Define constants at the top of each class or in a shared `GameConstants.cs`
- **Chunk size:** 16×16×256 voxels per chunk (do not change without updating all systems)
- **Coordinate system:** World space is Unity default (Y-up). Chunk coordinates are in chunk units, not world units.
- **Async:** Use Unity Coroutines for game-side async. Use `async/await` only for Firebase calls.

## Firebase / Auth Rules
- Never expose Firebase config secrets in source code — use environment config or Unity's `StreamingAssets` approach with `.gitignore`
- Parent account = Firebase Auth user. Child profiles = Firestore subcollection under parent document.
- World save data is stored per child profile in Firestore.

## Performance Targets (WebGL)
- Initial load: < 10s on average connection
- In-game framerate: ≥ 30 FPS in browser
- Use greedy meshing for all chunk geometry
- Pool chunks — do not instantiate/destroy, reuse from a pool

## What NOT to Do
- Do not add hostile mobs, combat systems, or violence of any kind
- Do not add in-app purchases (v1)
- Do not add multiplayer networking (v1)
- Do not use `public` fields where `[SerializeField]` private fields will do

## Reference Docs
- Full plan: `docs/deencraft-plan.md`

## Development Methodology (Superpowers Skills)
This project uses the [Superpowers](https://github.com/obra/superpowers) agentic development framework. Skills are in `.github/skills/`. Key workflows:

| Skill | When to use |
|---|---|
| `brainstorming` | Before any new feature — refine idea into a spec first |
| `writing-plans` | Turn an approved spec into a bite-sized task plan |
| `executing-plans` | Execute a written plan with review checkpoints |
| `subagent-driven-development` | Execute a plan using fresh subagents per task |
| `test-driven-development` | Always — write failing test before any implementation code |
| `systematic-debugging` | Before proposing any bug fix — find root cause first |
| `verification-before-completion` | Before claiming any task is done — run and show evidence |
| `using-git-worktrees` | When starting feature work that needs branch isolation |
| `finishing-a-development-branch` | When implementation is complete — merge, PR, or discard |

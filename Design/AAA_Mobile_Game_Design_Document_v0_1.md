# WarlineCapture: AAA Mobile Game Design Document
v0.1 - Prepared from GAME_DESIGN_REFERENCE.md | 2026-05-01
2026-05-21 amendment: aligned to the active 3D single-map direction.

## Executive Summary
WarlineCapture should become a mobile-first 3D RTS with three player-facing modes built on one shared 3D operation-map simulation: Campaign, Operations, and Skirmish. The existing code already supports base building, production, mixed units, transport, base breach combat, radar/satellite warnings, economy, configurable enemy AI, and Auto Mode. The production direction is now full 3D single-map mobile RTS as defined in `Design/3D_SingleMap_Gameplay_Direction.md`. The next work should focus on reusable mode infrastructure, objectives, progression, persistence, polished command-base mobile UX, and controlled 3D large-map validation.

## Active Visual Production Direction

WarlineCapture should use the current `Design` source-of-truth hierarchy and pursue full 3D single-map RTS visuals: Middle Eastern-inspired towns, forward command bases, civilians, hostile cells, vehicles, aircraft, buildings, and metadata-backed command overlays in one playable world. Planning, briefing, minimap, deployment, and battle view are camera/UI states over that same map.

Reference paths:

- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/UIUX_MainMenu_Visual_Contract.md`
- `Design/VisualLockLayered/README.md`
- `Assets/Game/Configs/Prefabs`

Legacy visual-lock packs were archived under `Design/Archive/LegacyVisualLock_2026-05-22/` and are comparison/history only. New production visual targets must be recreated under `Design/VisualLockLayered/<SurfaceId>/` using the active 3D single-map and command-base direction.

## Recommended Modes
### 1. Campaign
- Curated mission nodes and operation selections that launch 3D operation maps.
- Each level has unique setup, objectives, constraints, star scoring, and rewards.
- Teaches existing systems gradually: infantry, APCs, walls/gates, radar, helicopters, economy, air, and breach assault.
### 2. Operations
- Multi-week saved campaign across city districts.
- The player protects civilians, stabilizes districts, and uncovers a hidden hostile network through abstract intel and trust systems.
- Uses district values: Security, Trust, Infrastructure, Enemy Influence, Intel Confidence, Civilian Density, and Heat.
### 3. Skirmish
- Fast configurable skirmishes using existing AI knobs.
- Enemy options: Hidden Cell Network, AI Military, Mixed, Random, AI-vs-AI.
- Options: enemy count, difficulty, resources, map seed, victory condition, tech level, match length, Auto Mode.

## Highest Priority Build Items
- GameModeDefinition and ScenarioSetup loader
- Objective Manager with win/loss/star conditions
- Skirmish setup screen
- Result/debrief screen
- 3D operation-map validation and command-base menu target updates
- Campaign UI and Chapter 1
- Civilian Safety and Public Trust scoring
- OperationState save/persistence
- District state model and hidden network abstraction
- AI profile definitions and 1-3 enemy support

See the DOCX for the full AAA mobile GDD, technical roadmap, mode designs, AI roadmap, and example first chapter/week.

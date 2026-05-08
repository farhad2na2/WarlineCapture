# WarlineCapture: AAA Mobile Game Design Document
v0.1 - Prepared from GAME_DESIGN_REFERENCE.md | 2026-05-01

## Executive Summary
WarlineCapture should become a mobile-first RTS with three modes built on one shared simulation: Saga Map Campaign, Persistent City Operation, and Quick Custom Games. The existing code already supports base building, production, mixed units, transport, base breach combat, radar/satellite warnings, economy, configurable enemy AI, and Auto Mode. The production visual direction is now premium 2D isometric mobile RTS, validated through the ISO-01 golden asset Tilemap spike under `Design/VisualReferences/2DIsometricProduction`. The next work should focus on reusable mode infrastructure, objectives, progression, persistence, polished mobile UX, and controlled 2D isometric asset production.

## Active Visual Production Direction

WarlineCapture should use the original design documents as the source of truth and pursue premium 2D isometric RTS visuals rather than constraining gameplay around the current desert/Synty 3D asset set.

Reference paths:

- `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
- `Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`
- `Design/VisualReferences/2DIsometricProduction/GoldenAssets/README.md`
- `Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Report.md`

## Recommended Modes
### 1. Saga Map Campaign
- Curated level nodes on a district map.
- Each level has unique setup, objectives, constraints, star scoring, and rewards.
- Teaches existing systems gradually: infantry, APCs, walls/gates, radar, helicopters, economy, air, and breach assault.
### 2. Persistent City Operation
- Multi-week saved campaign across city districts.
- The player protects civilians, stabilizes districts, and uncovers a hidden hostile network through abstract intel and trust systems.
- Uses district values: Security, Trust, Infrastructure, Enemy Influence, Intel Confidence, Civilian Density, and Heat.
### 3. Quick Custom Games
- Fast configurable skirmishes using existing AI knobs.
- Enemy options: Hidden Cell Network, AI Military, Mixed, Random, AI-vs-AI.
- Options: enemy count, difficulty, resources, map seed, victory condition, tech level, match length, Auto Mode.

## Highest Priority Build Items
- GameModeDefinition and ScenarioSetup loader
- Objective Manager with win/loss/star conditions
- Custom Game setup screen
- Result/debrief screen
- 2D isometric art bible and modular terrain golden asset set
- Saga Map UI and Chapter 1
- Civilian Safety and Public Trust scoring
- OperationState save/persistence
- District state model and hidden network abstraction
- AI profile definitions and 1-3 enemy support

See the DOCX for the full AAA mobile GDD, technical roadmap, mode designs, AI roadmap, and example first chapter/week.

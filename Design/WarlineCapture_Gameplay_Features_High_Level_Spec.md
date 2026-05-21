# WarlineCapture Gameplay Features High-Level Spec

Date: 2026-05-02

## Source Material

- `Design/GAME_DESIGN_REFERENCE.md`
- `Design/WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
- `Design/BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
- `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`
- `Design/AI_CONTROLLER_DESIGN.md`
- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `Design/WarlineCapture_Level_And_Mission_Content_Plan.md`
- Current gameplay systems under `Assets/Game/Scripts`
- Current validation tests under `Assets/Tests`

## Purpose

This document defines the gameplay feature roadmap that should follow the UI/UX shell work. The UI/UX plan defines how players navigate and see the game. This gameplay plan defines what the modes actually do: campaign structure, objectives, rewards, progression, persistent operation state, custom game rules, AI profiles, and match result logic.

Before creating level-by-level or mission-by-mission content, read `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`, `Design/WarlineCapture_Level_And_Mission_Content_Plan.md`, and the relevant chapter doc under `Design/SagaChapters`. The north-star doc is the content-authoring gate for WarlineCapture's core fantasy and grammar; the level-and-mission plan owns shared structure; chapter docs own chapter-specific mission matrices and specs.

Before adding a unit, building, support ability, upgrade, reward target, or store item, read `Design/WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`. Gameplay configs should use ids from `Design/BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`, while art and UI references should use `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`.

Terminology: a Campaign node launches a player-facing Mission; the Mission uses a `ScenarioSetup`; the ScenarioSetup references one reusable 3D `OperationMap` through IDs such as `OperationMapId`, `PlanningCameraId`, and `MinimapProjectionId`.

Map-view contract: use `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md` for all mission/map work. Planning, briefing, minimap, deployment, threat, and battle views are UI/camera states over the same 3D operation map.

## Current Gameplay Foundation

WarlineCapture already has a strong real-time tactical foundation:

- Grid-based RTS movement and pathing.
- Player unit selection and attack commands.
- Building placement and base construction.
- Road building.
- Unit production from buildings.
- Faction resources: tactical money, oil, and fuel. Player-facing UI maps tactical money to Credits and keeps Fuel as the shared mobility resource.
- Civilian population and housing/resource stats.
- Combat against units and buildings.
- Base breach through walls/gates.
- Transport boarding for APCs and aircraft.
- Helicopter landing and rope disembark.
- Air movement and aircraft support.
- Radar/satellite threat warning.
- Runtime match stats through `GameRuntimeStats`.
- Configurable enemy AI and Player Auto Mode.
- AI economy, building, production, squad, targeting, and combat systems.
- Android build and Unity test gates.

What is missing is the product/gameplay layer around the tactical simulation:

- Mode definitions.
- Scenario setup.
- Mission objectives.
- Win/loss conditions.
- Star scoring.
- Rewards and unlocks.
- Campaign progression.
- Operations save state.
- District state and strategic actions.
- Match result/debrief routing.
- Player profile and long-term progression.
- Combat catalog loader, visual catalog loader, upgrade service, and catalog sanity tests.

UI elements for these systems are governed by `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`. Any new visible UI element, including labels, images, icons, cards, tabs, rows, dropdowns, toggles, sliders, maps, meters, and buttons must map to a gameplay purpose, route/effect or read-only/decorative role, data source, state rule, and feedback state before it is implemented.

## Target Gameplay Product

WarlineCapture should become a mobile-first RTS with three major modes built on the same simulation.

Gameplay north star:

- Win battles while keeping the city alive.
- Make civilian safety, district recovery, and tactical command pressure the main differentiators.
- Every authored mission should connect briefing, intel/scout, loadout, tactical mission, result/stars, rewards, district consequence, and next decision.

Presentation target:

- Full 3D single-map mobile RTS.
- Keep gameplay readable under a mobile landscape tactical HUD and command-base shell.
- Validate prefab-catalog units/buildings, metadata overlays, camera states, and performance in Unity before expanding the full asset library.
- Active art-direction details live in `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`.

## 3D Single-Map Gameplay Alignment

Gameplay implementation should remain simulation-first and art-direction aware:

- Do not build new gameplay features around the archived 2D isometric or strategic/tactical-map presentation.
- Mode, mission, objective, reward, and encounter systems should reference `ScenarioSetup` and `OperationMap` identifiers, not hard-coded visual prefabs.
- Scenario and mission configs should bind to `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, operation-map metadata, and runtime catalog ids from `Assets/Game/Configs/Prefabs`.
- Planning, briefing, minimap, deployment, threat, and battle views are camera/UI states over the same 3D operation map. They are not separate map products.
- Tactical readability is a gameplay requirement: objectives, wave timing, spawn locations, selection sizes, command ranges, camera assumptions, civilians, vehicles, and markers must remain readable at mobile landscape scale.
- The UI track owns Canvas HUD/screens; the 3D gameplay/art track owns operation-map assets, entity presentation, camera states, metadata overlays, and gameplay capture behind HUD overlays.

## 3D Operation-Map Alignment

The active battlefield production path is large authored 3D operation maps with separate gameplay metadata.

Gameplay rules stay simulation-first:

- Existing ECS/grid/pathfinding remains authoritative.
- Terrain pixels are presentation, not gameplay truth.
- Roads, alleys, sidewalks, gates, walls, rooftops, courtyards, pads, and vehicle lanes must be represented logically by operation-map metadata.
- Walkability, blockers, spawns, objectives, minimap projection, deployment zones, civilian-risk zones, and camera bounds come from operation-map metadata.
- Buildings, units, resources, destructible cover, objectives, VFX, health bars, selection rings, and UI markers remain runtime objects.

Building placement should move toward approved sockets/pads:

- production buildings use building sockets
- defenses use defense sockets
- resources use resource sockets
- temporary deployables can use constrained valid zones later

Do not bake destructible gameplay buildings into map art. Baked buildings are decorative only.

### 1. Campaign

Purpose:

- Curated mission-based campaign.
- Teaches the game gradually.
- Provides clear objectives, stars, unlocks, and rewards.

Core gameplay:

- Select a mission node.
- Read briefing.
- Choose loadout when allowed.
- Complete tactical mission.
- Earn stars and rewards.
- Unlock next missions, units, buildings, or support abilities.

### 2. Operations

Purpose:

- Long-running district operation layer.
- The player stabilizes districts, protects civilians, and uncovers hidden hostile activity.

Core gameplay:

- Inspect district state and operation-map options.
- Manage security, trust, infrastructure, enemy influence, intel confidence, civilian density, and heat.
- Take strategic actions: patrol, scan, aid, repair, evacuate, build outpost, raid.
- Generate 3D operation missions from district events.
- End days, persist state, and react to evolving threats.

### 3. Skirmish

Purpose:

- Fast replayable skirmishes.
- Reuse existing AI knobs.
- Let the player experiment with difficulty, economy, enemy count, map seed, and win conditions.

Core gameplay:

- Choose enemy type/count/difficulty.
- Choose resources, speed, aggression, and match rules.
- Launch a 3D operation match.
- Show result/debrief, but do not require campaign progression.

## Core Architecture

Introduce a mode and mission layer above existing systems.

```text
GameModeDefinition
  Campaign
  Operations
  Skirmish

ScenarioSetup
  operation map / grid / city setup
  faction setup
  resource setup
  AI setup
  player loadout
  objective list
  reward table

ObjectiveManager
  evaluates mission objectives
  evaluates win/loss
  evaluates star goals
  reports progress to UI

MissionResultBuilder
  collects stats
  computes stars
  computes rewards
  routes back to source mode
```

Existing `GameBootstrap.BeginGameplay()` should eventually accept a launch payload instead of always starting the same default scenario.

## Gameplay Feature Pillars

### Mission Structure

Every 3D operation-map match should know:

- Which mode launched it.
- Which scenario config it uses.
- Which factions exist.
- What the player starts with.
- Which AI profiles are active.
- Which objectives are required.
- Which optional star goals exist.
- Which rewards can be granted.
- Where to route after the match ends.

### Objectives

Initial objective types:

- Destroy all enemies.
- Destroy target building.
- Survive duration.
- Protect civilians.
- Keep unit losses below threshold.
- Build a required structure.
- Produce a required unit type.
- Gather/earn resource amount.
- Reach/extract with transport.
- Defend base until timer ends.

### Star Scoring

Stars should be bonus goals, not hidden scores.

Examples:

- Win the mission.
- Finish under a time limit.
- Lose fewer than N soldiers.
- Keep at least N civilians alive.
- Destroy target without losing a vehicle.
- Build radar before first attack.
- Complete with no enemy breach.

### Rewards

Reward types:

- Commander XP.
- Credits, Materials, Fuel, Intel, Command Authority, and Rush Tickets.
- Unit unlock.
- Building unlock.
- Support ability unlock.
- Blueprint parts.
- Gear/module item.
- Cosmetic.
- Operation supply.
- Campaign chapter stars.
- Operation trust/security/intel changes.

Rewards must be data-driven so UI result screens can preview them before mission launch and grant them after mission completion. Resource and reward lifecycle rules are locked in `WarlineCapture_Economy_Reward_Design.md`.

### Progression

Progression layers:

- Player profile level and XP.
- Campaign mission completion and star count. Existing `SagaProgress` may remain as a runtime compatibility backing store until renamed.
- Unit/building/support unlocks.
- Operation district state and day count.
- Skirmish presets and last-used setup.

### Difficulty and AI Profiles

The existing `AISettingsRuntimeState` is a good start for Skirmish. Campaign and Operations need named AI profiles:

- Tutorial Cell
- Hidden Cell Network
- AI Military
- Defensive Garrison
- Air Assault
- Swarm Militia
- Armored Column
- Mixed Force
- Random

Profiles should map to AI controller configs, preferred buildings/units/vehicles, economy policy, aggression, attack cadence, and allowed tech.

### Balance and Gameplay Probes

WarlineCapture needs repeatable gameplay probes for tuning economy, AI pressure, mission length, objective pacing, and reward balance. These probes are not build-validation tests and must not cause normal EditMode, PlayMode, Android, or CI validation failures.

The concrete implementation plan for extending these probes and their automated harness tests is `WarlineCapture_Balancing_Automated_Test_Plan.md`.

Balance probes should be opt-in and report-oriented:

- Put first balance probes under `Assets/Tests/Editor/Balance` so they compile with the current EditMode test layout. If the project later adopts game/test asmdefs, move them into a separate balance test assembly such as `Assets/Tests/Balance/WarlineCapture.BalanceTests.asmdef`.
- Mark balance tests with `[Category("Balance")]` and use `[Explicit]` for long-running or experimental probes.
- Exclude the `Balance` category from normal build-validation and CI test filters.
- Assert only on harness correctness: scenario loaded, simulation completed, no exception, metrics written.
- Do not fail tests because a balance value is too high or too low. Classify balance outcomes in reports instead.
- Write generated reports outside `Assets`, such as `Temp/BalanceReports` or `Library/WarlineCaptureBalanceReports`.

Initial balance metrics:

- Winner and result reason.
- Match duration.
- Time to first attack.
- Time to first production.
- Time to first base breach.
- Resource income, spend, and float over time.
- Army value over time.
- Unit losses and kill/death ratios.
- Civilian losses and collateral damage.
- Objective completion timing.
- Threat warning lead time.

Initial named probe scenarios:

- `QuickCustom_Default_Medium`
- `QuickCustom_Hard_Swarm`
- `Campaign_Chapter1_Mission1`
- `Campaign_Chapter1_Mission2`
- `Campaign_Chapter1_Mission3`
- `Campaign_Chapter1_Mission4`
- `Campaign_Chapter1_Mission5`
- `Operation_Raid_MediumIntel`
- `BaseDefense_HeavyAir`
- `EconomyRush_FastBuild`

Reports should classify results as `Good`, `Watch`, `Problem`, or `InvalidRun`. For example, a target match length can be 8 to 14 minutes, `Watch` outside that range, and `Problem` only as a report label. The Unity test itself should still pass unless the simulation or report harness failed.

### Civilian Safety and Public Trust

This should become a major differentiator for WarlineCapture.

Track:

- Civilians alive.
- Civilians housed.
- Civilian deaths.
- Refugees supported.
- Collateral damage.
- District trust.
- District stability/security.

Use these values in objectives, stars, rewards, and operation consequences.

### Persistence

Use local JSON persistence first.

Save:

- Player profile.
- Campaign progress, using `SagaProgress` only as a compatibility wrapper where needed.
- Operation state.
- Settings.
- Skirmish last setup.

Do not persist raw ECS world state initially. Persist abstract mode state and regenerate tactical missions from configs.

## High-Level Implementation Phases

### Phase 1 - Mode and Scenario Foundations

Goal: make the game launch through explicit mode/scenario data.

Build:

- `GameModeDefinition`
- `ScenarioSetup`
- `GameLaunchPayload`
- `GameModeRuntimeState`
- `ScenarioSetupLoader`

Outcome:

- Skirmish, Campaign, and Operations can all launch the existing tactical scene with different setup data later.

### Phase 2 - Skirmish Rules

Goal: connect the first new UI mode to real gameplay.

Build:

- `QuickGameConfig`
- `QuickGamePreset`
- AI setting mapping.
- Match rules: enemy count, difficulty, starting resources, win condition, match length.

Outcome:

- The player can configure and launch a match using systems that already exist.

### Phase 3 - Objective Manager

Goal: formalize mission progress and win/loss.

Build:

- `ObjectiveConfig`
- `ObjectiveRuntimeState`
- `ObjectiveManager`
- objective evaluation against current systems and `GameRuntimeStats`.

Outcome:

- Tactical matches can end based on objectives instead of manual/implicit play.

### Phase 4 - Result, Stars, Rewards

Goal: turn match outcomes into meaningful progression events.

Build:

- `MissionResultData`
- `MissionResultBuilder`
- `StarGoalConfig`
- `RewardConfig`
- `RewardGrantResult`
- `RewardService`

Outcome:

- UI result/debrief screens can display actual gameplay outcomes and grants.

### Phase 5 - Player Profile and Unlocks

Goal: add long-term progression.

Build:

- `PlayerProfileState`
- `CommanderProgression`
- `UnlockState`
- `AccountStats`

Outcome:

- Rewards persist and can unlock units/buildings/modes.

### Phase 6 - Campaign

Goal: build the first structured campaign loop.

Build:

- `CampaignProgress` or existing `SagaProgress` runtime compatibility wrapper
- `ChapterConfig`
- `CampaignMissionNodeConfig` or the existing `SagaMissionNodeConfig` as a compatibility wrapper
- Chapter 1 mission configs.
- Campaign unlock/star logic.

Outcome:

- Main Menu -> Campaign Map -> Briefing -> Loadout -> Match -> Result -> Campaign Map.

### Phase 7 - Operations

Goal: build the city operation strategic layer.

Build:

- `OperationState`
- `DistrictState`
- `OperationEvent`
- `OperationAction`
- `IntelEvidence`
- operation save/load.

Outcome:

- Main Menu -> Operations Dashboard -> District -> Action/Raid -> Match/Report -> Operations Dashboard.

### Phase 8 - Advanced AI and Encounter Variety

Goal: make modes replayable and distinct.

Build:

- Named AI profiles.
- Encounter templates.
- Spawn waves.
- Convoys and timed attacks.
- Defensive garrisons.
- Air assault events.
- Hidden cell/intel reveal events.

Outcome:

- Campaign and Operations missions feel authored, not just default skirmishes.

### Phase 9 - Balance and Content Pass

Goal: tune progression and mission pacing.

Build:

- Cost/reward tables.
- Difficulty curves.
- Chapter pacing.
- Operation day pacing.
- Unit/building unlock pacing.

Outcome:

- The game has a coherent early game and replayable skirmish baseline.

## Recommended Build Order After UI/UX Foundation

1. Skirmish gameplay config, launch payload, and 3D operation-map ID contract.
2. Objective Manager with a small set of objective types.
3. Mission result and reward data.
4. Campaign Chapter 1 with 3 playable missions.
5. Player profile and unlock persistence.
6. Operations state and district actions.
7. Advanced AI profiles and encounter templates.

This order gives immediate value from the UI work and creates the technical foundation that Campaign and Operations both need.

# WarlineCapture Gameplay Features Detailed Implementation Spec

Date: 2026-05-02

## Purpose

This document describes the concrete gameplay systems to implement after the UI/UX shell. It is code-oriented and grounded in the current Unity project.

The goal is to avoid building screens that only look complete. Every new screen should eventually bind to real gameplay data: modes, scenarios, objectives, results, rewards, progression, and persistence.

Before adding mission configs, level-by-level content, reward tables, validation rules, or balance target bands, read `Design/Gameplay_North_Star_And_Content_Grammar.md`, `Design/Level_And_Mission_Content_Plan.md`, and the relevant dedicated chapter doc under `Design/SagaChapters`. Mission content must select a defined archetype, threat family, objective/star pattern, consequence model, target balance band, and validation plan from those documents or update them first.

Before adding units, buildings, skills, abilities, upgrades, reward targets, or store item target ids, read `Design/Combat_Catalog_And_Upgrade_Design.md`. Gameplay numbers and upgrade tiers must come from `Design/BalanceConfigs/Combat_Balance_Config_v0_1.json`; art, icon, portrait, VFX, and audio references must come from `Design/VisualConfigs/Combat_Visual_Config_v0_1.json`.

Before wiring player-facing command, reward, warning, damage, popup, or invalid-action feedback, read `Design/Visual_Feedback_VFX_Recommendations.md` and `Design/Audio_Design_Guidelines.md`. Gameplay systems should emit clear accepted/rejected/result events that the UI, VFX, and audio layers can present consistently.

Before adding FTUE, contextual help, assistant recommendations, or any assistant-controlled player action, read `Design/FTUE_And_Command_Assistant_Design.md`. Tutorial and assistant behavior must be data-driven, interruptible, and routed through typed command intents rather than arbitrary screen-coordinate automation.

Before adding map selection, operation-map loading, minimap behavior, camera jumps, mission previews, or level metadata, read `Design/3D_SingleMap_Gameplay_Direction.md`. Planning, briefing, minimap, deployment, threat, and battle views are UI/camera states over the same 3D operation map.

Terminology for implementation:

- `MissionConfig` is the player-facing authored content unit.
- `ScenarioSetup` is the tactical configuration used by the mission.
- `OperationMap` is the reusable 3D battlefield layout referenced by `ScenarioSetup`.
- Do not use `Level` as a synonym for `Mission` in ids, UI, tests, or data paths.

The active visual production direction is full 3D single-map mobile RTS. Gameplay implementation should preserve tactical readability across planning, minimap, deployment, and battle camera states validated against the selected 3D operation map.

For Chapter 1 operation-map production, use `Design/Level_And_Mission_Content_Plan.md`, `Design/LargeScale_Grid_Movement_Design.md`, and the dedicated chapter docs as the bridge between ScenarioSetup IDs, OperationMap IDs, map metadata, current grid/pathfinding buffers, selection, movement, attack, and validation scenes.

Every implemented visible UI element must also satisfy `Design/UIUX_Gameplay_Element_Alignment.md`. A gameplay surface without a live runtime system must expose a clear `Locked`, `DesignedUnavailable`, `DevOnly`, or `ReadOnly` state instead of behaving like a silent inert element.

## 3D Operation-Map Implementation Rules

- Keep gameplay systems data-driven and visual-presentation agnostic. Do not add new mode/objective/encounter logic that depends on archived 2D map packages, desert/Synty prefabs, or camera-specific assumptions.
- Scenario and mission configs should carry art/runtime references by ID, such as `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, and optional `DesignTargetSceneId`.
- Planning, briefing, minimap, deployment, threat, and battle views are camera/UI states over one `OperationMapDefinition`; they are not separate map products.
- Runtime gameplay should consume the existing simulation systems first, then let the active battlefield presentation resolve those IDs to 3D operation-map scenes, metadata, entity presentation, markers, and camera states.
- Spawn positions, objective target areas, command ranges, camera defaults, minimap scale, civilian-risk areas, and enemy wave paths must be validated for 3D mobile readability.
- Manual art validation belongs to the 3D operation-map validation path and prefab-catalog presentation work. Normal gameplay tests should validate data contracts and repeatable behavior, not final art quality.

## 3D Operation-Map Metadata Rules

The active terrain direction is large authored 3D operation maps with metadata. Gameplay code should not depend on visual mesh/material details.

Add or extend map data around these concepts:

- `OperationMapDefinition`
- `OperationMapId`
- `PlanningCameraId`
- `MinimapProjectionId`
- `OperationMapRouteAnchor`
- `OperationMapWalkableZone`
- `OperationMapBlockerVolume`
- `OperationMapRoadGraphNode`
- `OperationMapRoadGraphEdge`
- `OperationMapBuildingZone`
- `OperationMapDeploymentZone`
- `OperationMapSpawnAnchor`
- `OperationMapObjectiveAnchor`
- `OperationMapCivilianRiskZone`
- `OperationMapCameraBounds`

Runtime behavior:

- Build the pathfinding/grid input from metadata.
- Build minimap data from metadata.
- Clamp minimap, threat, objective, and tutorial camera jumps to operation-map camera bounds.
- Restrict production building placement to sockets/pads.
- Keep gameplay buildings as runtime entities placed on sockets.
- Swap runtime building models/states for damage/destruction.
- Use scorch/rubble decals and VFX above terrain after destruction.
- Never require the background art to change for building destruction.

## Current Code Anchors

Use these current systems instead of duplicating behavior:

- `GameBootstrap`
  - Currently starts the tactical simulation through `BeginGameplay()`.
  - Seeds AI economies, control, build plans, production plans, squad plans, targeting, and runtime systems.

- `AISettingsRuntimeState`
  - Current global Skirmish/QuickCustom-style AI tuning state.
  - Supports difficulty, starting money, income, build speed, production speed, attack group size/frequency, aggression, expansion, target priority, player auto mode, and enemy count.

- `GameRuntimeStats`
  - Tracks oil extracted, fuel produced, units ordered, buildings built, own soldier deaths, enemy soldier deaths.
  - Should become a source for objectives, stars, account stats, and mission results.

- `ThreatWarningRuntimeState`
  - Tracks pending ground/air warnings.
  - Should feed threat objective events and operation warnings.

- `BuildingPlacementSystem`
  - Owns building placement, production, faction resource snapshots, runtime building data, and camp request flows.
  - Should be adapted to validate operation-map building zones/sockets/pads instead of relying on arbitrary visual placement.

- `RTSSelectionSystem`
  - Owns tactical unit selection, focused unit data, attack/move/transport command paths.

- ECS systems:
  - `AIEconomySystem`
  - `AIBuildPlannerSystem`
  - `AIProductionSystem`
  - `AISquadSystem`
  - `AITargetingSystem`
  - `AICombatOrderSystem`
  - `ThreatDetectionWarningSystem`
  - movement/combat/transport/base breach systems.

## Folder Layout

Add gameplay systems here:

```text
Assets/Game/Scripts/Modes
Assets/Game/Scripts/Scenarios
Assets/Game/Scripts/Objectives
Assets/Game/Scripts/Results
Assets/Game/Scripts/Rewards
Assets/Game/Scripts/Progression
Assets/Game/Scripts/Persistence
Assets/Game/Scripts/Operation
Assets/Game/Scripts/Saga
Assets/Game/Scripts/QuickGame
Assets/Game/Scripts/Encounters
Assets/Game/Scripts/CombatCatalog
Assets/Game/Scripts/Upgrades
Assets/Game/Scripts/Tutorial
Assets/Game/Scripts/Tutorial/Assistant
Assets/Game/Scripts/Tutorial/Recommendations
Assets/Game/Scripts/Tutorial/Control
Assets/Game/Scripts/Tutorial/Data
Assets/Game/Configs/Modes
Assets/Game/Configs/Scenarios
Assets/Game/Configs/Missions
Assets/Game/Configs/Objectives
Assets/Game/Configs/Rewards
Assets/Game/Configs/Saga
Assets/Game/Configs/Operation
Assets/Game/Configs/QuickGame
Assets/Game/Configs/Encounters
Assets/Game/Configs/CombatCatalog
Assets/Game/Configs/VisualCatalog
Assets/Game/Configs/Tutorial
```

Keep existing tactical scripts in place. Add mode systems around them.

## Combat Catalog And Upgrade Foundation

Create these types before mission reward/store target ids are wired into runtime:

- `CombatCatalogConfig`
- `CombatUnitConfig`
- `CombatBuildingConfig`
- `CombatAbilityConfig`
- `CombatUpgradeTrackConfig`
- `CombatVisualCatalogConfig`
- `CombatCatalogLoader`
- `UpgradeService`
- `UpgradeInventoryState`

Responsibilities:

- Load gameplay values from the combat balance config.
- Load art/icon/portrait/VFX/audio references from the visual config.
- Validate that every `visualCatalogId`, ability reference, producer relationship, and upgrade-track reference resolves.
- Apply upgrade tiers outside active combat launch, then pass resolved stats into `ScenarioSetup` or unit/building authoring overrides.
- Keep existing prefab ScriptableObject values as current runtime anchors until data migration is complete.

Tests:

- `CombatCatalogLoader_LoadsBalanceAndVisualConfigs`
- `CombatCatalogLoader_RejectsMissingVisualRefs`
- `CombatCatalogLoader_RequiresAbilityAvailabilityAndImplementationSpecs`
- `CombatCatalogLoader_RequiresUpgradeAvailabilityAndResolvedItems`
- `UpgradeService_AppliesTierModifiersWithoutMutatingBaseConfig`
- `UpgradeService_RejectsNegativeCostsAndUnknownTrackIds`
- `UpgradeService_BlocksPlayerTierMutationDuringActiveCombat`

## Phase 1 - Mode and Scenario Foundations

### New Types

Create:

- `GameModeId`
- `GameModeDefinition`
- `GameLaunchPayload`
- `GameModeRuntimeState`
- `ScenarioSetup`
- `ScenarioSetupLoader`
- `FactionScenarioSetup`
- `PlayerLoadoutSetup`
- `ScenarioResourceSetup`
- `ScenarioAISetup`

### Suggested Enums

```csharp
public enum GameModeId
{
    QuickCustom,
    SagaCampaign,
    PersistentOperation
}

public enum ScenarioWinCondition
{
    DestroyAllEnemies,
    CompleteObjectives,
    SurviveDuration,
    DefendBase,
    Sandbox
}
```

### GameLaunchPayload

Fields:

- `GameModeId Mode`
- `string ScenarioId`
- `string MissionId`
- `string SourceRoute`
- `QuickGameConfig QuickGame`
- `CampaignMissionLaunchData Campaign`
- `OperationMissionLaunchData Operation`
- `PlayerLoadoutSetup PlayerLoadout`
- `ScenarioSetup Scenario`

Responsibility:

- Be the one object passed from UI/mode screens into `GameBootstrap`.
- Avoid global special cases for Campaign vs Skirmish vs Operations.

### ScenarioSetup

Fields:

- Scenario id/name.
- Operation-map seed or authored operation-map id.
- Grid metadata profile, route graph id, or authored map preset.
- Start time/day.
- Player faction setup.
- Enemy faction setup list.
- Starting resources per faction.
- Initial unit/building overrides.
- Allowed unit/building catalog.
- Allowed skill/ability and upgrade catalog ids.
- Objective configs.
- Reward configs.
- Encounter configs.
- 3D operation-map/runtime ids:
  - `OperationMapId`
  - `TerrainSetId`
  - `PlanningCameraId`
  - `MinimapProjectionId`
  - `DesignTargetSceneId`

### Integration With GameBootstrap

Add a new entry point:

```csharp
public void BeginGameplay(GameLaunchPayload payload)
```

Migration path:

1. Keep current `BeginGameplay()` and have it build a default payload.
2. Add `BeginGameplay(GameLaunchPayload payload)`.
3. Move hard-coded startup values behind payload/configs gradually.

### Tests

Add:

- `GameLaunchPayload_DefaultQuickGameBuildsValidScenario`
- `ScenarioSetupLoader_LoadsDefaultScenario`
- `GameBootstrap_BeginGameplayWithoutPayloadUsesDefaultQuickGame`

## Phase 2 - Skirmish Gameplay

Use `Design/Skirmish_Mode_Implementation_Spec.md` as the active implementation contract for Skirmish. This section remains the code-oriented summary; the dedicated spec owns player flow, presets, UI/control requirements, result routing, prefab-catalog roster usage, and QuickCustom compatibility rules.

### New Types

Create:

- `QuickGameConfig`
- `QuickGamePreset`
- `QuickGameConfigMapper`
- `QuickGameRuntimeState`
- `QuickGameResultPolicy`

### QuickGameConfig Fields

- Enemy type.
- Enemy count.
- Difficulty.
- Starting Credits.
- Income multiplier.
- Build speed.
- Unit production speed.
- Attack group size.
- Attack frequency.
- Aggression.
- Expansion.
- Target priority.
- Player auto mode.
- Map seed.
- Operation-map preset or `OperationMapId`.
- Planning camera id.
- Minimap projection id.
- Starting resources.
- Win condition.
- Match length minutes.
- Fog of war enabled.
- Intel reveal enabled.
- Tech level.

### Mapping to Existing Systems

On launch:

- Set `AISettingsRuntimeState.Difficulty`.
- Set `AISettingsRuntimeState.StartingMoney` from the player-facing Starting Credits field.
- Set `AISettingsRuntimeState.IncomeMultiplier`.
- Set `AISettingsRuntimeState.BuildSpeed`.
- Set `AISettingsRuntimeState.UnitProductionSpeed`.
- Set `AISettingsRuntimeState.AttackGroupSize`.
- Set `AISettingsRuntimeState.AttackFrequency`.
- Set `AISettingsRuntimeState.Aggression`.
- Set `AISettingsRuntimeState.Expansion`.
- Set `AISettingsRuntimeState.TargetPriority`.
- Set `AISettingsRuntimeState.PlayerAutoAIEnabled`.
- Set `AISettingsRuntimeState.EnemyAICount`.

Then create `GameLaunchPayload` with player-facing mode `Skirmish`. Runtime internals may continue to map this through existing `QuickCustom` enum or route names until the code migration is complete.

### First Supported Win Conditions

- Sandbox: no automatic win/loss.
- Destroy All Enemies.
- Survive Duration.

### Tests

Add:

- `QuickGameConfigMapper_AppliesAISettings`
- `QuickGameConfig_DefaultPresetIsValid`
- `QuickGameLaunchPayload_ContainsScenarioAndMode`

## Phase 3 - Objective System

### New Types

Create:

- `ObjectiveType`
- `ObjectiveConfig`
- `ObjectiveProgress`
- `ObjectiveRuntimeState`
- `ObjectiveManager`
- `ObjectiveEvaluationContext`
- `ObjectiveEventBus`

### ObjectiveType

```csharp
public enum ObjectiveType
{
    DestroyAllEnemies,
    DestroyTargetBuilding,
    SurviveDuration,
    DefendBase,
    ProtectCivilians,
    BuildStructure,
    ProduceUnit,
    EarnResource,
    ExtractUnit,
    ReachLocation,
    PreventBaseBreach
}
```

### ObjectiveConfig Fields

- Objective id.
- Localized title key.
- Localized description key.
- Type.
- Required/optional flag.
- Target faction id.
- Target building id.
- Target unit id.
- Target count.
- Time limit seconds.
- Resource type.
- Threshold value.
- Failure threshold.
- Visible in HUD.

### ObjectiveEvaluationContext

Collect data from:

- `GameRuntimeStats.GetSnapshot()`
- ECS entity queries for alive units by faction.
- `BuildingPlacementSystem` runtime building state.
- `CitizenPopulationSystem` if available.
- `ThreatWarningRuntimeState`
- match elapsed time.
- launch payload/scenario.

### ObjectiveManager Responsibilities

- Initialize from objective configs.
- Evaluate progress every interval, not every frame unless cheap.
- Mark objectives complete/failed.
- Compute mission win/loss:
  - win when all required win objectives complete.
  - loss when any required fail objective fails.
- Publish progress changes for UI.
- Build objective result data for `MissionResultBuilder`.

### Initial Implementation Scope

Start with these objective types:

1. DestroyAllEnemies.
2. SurviveDuration.
3. BuildStructure.
4. ProduceUnit.
5. ProtectCivilians.
6. KeepUnitLossesBelow as a star goal, not required objective.

### Tests

Add:

- `ObjectiveManager_DestroyAllEnemiesCompletesWhenNoEnemyCombatUnitsRemain`
- `ObjectiveManager_SurviveDurationCompletesAfterTime`
- `ObjectiveManager_BuildStructureReadsGameRuntimeStats`
- `ObjectiveManager_ProtectCiviliansFailsBelowThreshold`
- `ObjectiveManager_RequiredCompletionProducesVictory`

## Phase 4 - Star Goals

### New Types

Create:

- `StarGoalConfig`
- `StarGoalType`
- `StarGoalResult`
- `StarScoringService`

### StarGoalType

```csharp
public enum StarGoalType
{
    CompleteMission,
    FinishUnderTime,
    KeepOwnDeathsBelow,
    KeepCiviliansAliveAbove,
    BuildAtLeast,
    AvoidBaseBreach,
    DestroyTarget,
    EarnResourceAtLeast
}
```

### Rules

- First star should usually be mission completion.
- Optional stars should be visible before launch.
- Star goals should not silently fail from hidden data unless the UI can explain them.

### Tests

Add:

- `StarScoringService_GrantsCompletionStarOnVictory`
- `StarScoringService_EvaluatesTimeAndDeathThresholds`
- `StarScoringService_ReturnsStableStarCount`

## Phase 5 - Mission Result Data

### New Types

Create:

- `MissionOutcome`
- `MissionResultData`
- `MissionResultBuilder`
- `CombatStatsSnapshot`
- `ObjectiveResult`
- `MissionRouteResult`

### MissionOutcome

```csharp
public enum MissionOutcome
{
    Victory,
    Defeat,
    Abandoned
}
```

### MissionResultData Fields

- Mode.
- Scenario id.
- Mission id.
- Outcome.
- Duration seconds.
- Difficulty.
- Star results.
- Objective results.
- Combat stats.
- Economy stats.
- Civilian stats.
- Reward grants.
- Next route.

### Sources

Use:

- `GameRuntimeStats.Snapshot`
- Objective runtime state.
- Scenario/mission config.
- Faction economy snapshots.
- Operation/Saga launch payload.

### Tests

Add:

- `MissionResultBuilder_BuildsVictoryResultWithStats`
- `MissionResultBuilder_BuildsDefeatResultWithFailedObjective`
- `MissionResultBuilder_SelectsRouteFromLaunchMode`

## Phase 6 - Rewards

### New Types

Create:

- `RewardType`
- `RewardConfig`
- `RewardItemConfig`
- `RewardGrantResult`
- `RewardService`
- `UnlockCatalog`
- `UnlockState`

### RewardType

```csharp
public enum RewardType
{
    CommanderXp,
    Credits,
    Materials,
    Fuel,
    Intel,
    CommandAuthority,
    RushTicket,
    UnitUnlock,
    BuildingUnlock,
    SupportAbilityUnlock,
    BlueprintParts,
    GearModule,
    Cosmetic,
    OperationSupply,
    CampaignStars, // May map to existing SagaStars runtime enum/storage until migration.
    OperationTrust,
    OperationSecurity,
    OperationIntel,
    OperationInfrastructure
}
```

### RewardConfig

Fields:

- Reward id.
- Canonical reward type from `Economy_Reward_Design.md`.
- Target item id for unlocks, blueprint parts, gear modules, cosmetics, and Operation supplies.
- Preview title key.
- Items.
- First-clear bonus flag.
- Star threshold.
- Difficulty multiplier flag.
- Mode restrictions.
- Duplicate fallback reward id.
- Balance tag and telemetry bucket.

### RewardService Responsibilities

- Preview rewards before mission.
- Grant rewards after result.
- Avoid duplicate first-clear grants.
- Convert duplicate unlocks, gear, and cosmetics through explicit fallback rewards.
- Update profile/progression/operation state.
- Return grant result for UI display.

### Initial Reward Scope

Start with:

- Commander XP.
- Credits.
- Unit unlock.
- Campaign stars. Existing reward enum names such as `SagaStars` may remain as storage/runtime compatibility until renamed.

Operation-specific rewards are `OperationSupply`, `OperationTrust`, `OperationSecurity`, `OperationIntel`, and `OperationInfrastructure` as defined in `Economy_Reward_Design.md`.

Current implementation note:

- `RewardType`, `RewardItemConfig`, `RewardConfig`, `RewardGrantResult`, and `RewardService` cover the first reward-service slice.
- `RewardService.GrantMissionRewards` updates profile Commander XP, derived commander level, Credits, wallet resources covered by the initial profile schema, unique unlock arrays, BlueprintParts duplicate fallback entries, profile win/loss counters, account combat totals, Campaign mission progress, saved operation supplies, and targeted Operation district trust/security/intel/infrastructure rewards.
- Mission Briefing and Mission Result reward rows now format Operation rewards with readable labels and district targets so operation mission rewards can be previewed and reviewed without custom UI text per mission.
- Every Chapter 1 mission now includes an authored Operation outcome reward: operation supply plus targeted North Bridge, Old Market, or Port Breach trust/security/intel/infrastructure gains. Breach Assault remains the Port Breach stabilization hook used by the Operation raid flow.
- Operation-launched mission sessions prioritize Operation reward rows in briefing/result views so the player sees district consequences within the limited reward-card surfaces; Campaign-launched sessions keep the default reward ordering.
- Chapter 1 mission configs now include reward configs. Campaign Map node info, node locked/available state, and Mission Briefing previews bind from this mission data plus local Campaign progress, and the current mission completion flow grants rewards through `SaveService` before showing the mission result popup with the granted reward rows.
- Remaining reward-service work: authored data assets under `Assets/Game/Configs/Rewards`, richer item-specific inventory models, and POP-04 unlock reveal chaining.

Current progression implementation note:

- `ProgressionService` provides the first fixed commander XP table from level 1-10, derives commander level from total XP, preserves a higher saved level if present, and accumulates account result stats from `MissionResultData`.
- `RewardTrackService` provides the first fixed commander-level reward track, including milestone eligibility, persisted claimed-node ids, duplicate/locked claim protection, and grants for credits, materials, rush tickets, command authority, and cosmetics.
- `MissionHistoryService` archives recent local mission results into saved profile data, ordered newest-first and capped for a lightweight Profile History surface.
- `CommanderProfileScreenController` binds saved profile state into `SCN-03 Commander Profile`, including wallet counters, commander level/XP progress, unlock counts, win/loss history, account combat totals, saved recent mission report data, reward-track eligibility, claimable reward-track row buttons with modal detail/claim feedback, local tab content, and a first-claim CTA.

### Tests

Add:

- `RewardService_GrantsXpAndCredits`
- `RewardService_DoesNotDuplicateFirstClearUnlock`
- `RewardService_RequiresStarThreshold`
- `RewardService_UpdatesCampaignProgress`

## Phase 7 - Progression and Profile

### New Types

Create:

- `PlayerProfileState`
- `CommanderProgression`
- `AccountStats`
- `PlayerInventory`
- `ProgressionService`

### PlayerProfileState Fields

- Commander id/name.
- Level.
- Current XP.
- Total XP.
- Wallet.
- Unlock state.
- Account stats.
- Last selected mode.

### Commander Progression

Start with a simple XP table:

- Level 1 to 10 early tuning.
- XP required grows per level.
- Unlocks can be tied to level and/or mission rewards.

### AccountStats

Track:

- Victories.
- Defeats.
- Missions completed.
- Stars earned.
- Units lost.
- Enemies defeated.
- Civilians protected/lost.
- Buildings built.
- Resources earned.

### Tests

Add:

- `ProgressionService_AddXpLevelsUp`
- `ProgressionService_AppliesUnlocks`
- `AccountStats_AccumulatesMissionResult`

## Phase 8 - Persistence

### New Types

Create:

- `WarlineCaptureSaveData`
- `SaveService`
- `SaveSlotInfo`
- `JsonSaveRepository`
- `SaveMigration`

### Save Scope

Persist:

- Save version.
- Player profile.
- Campaign progress.
- Operation state.
- Settings.
- Last Skirmish setup. Existing `quickgame.json` may remain as the compatibility filename until save migration.

Do not persist:

- Raw ECS entities.
- Runtime pathfinding state.
- Temporary UI state.

### Save Path

Use:

```csharp
Application.persistentDataPath
```

Initial files:

```text
profile.json
saga.json
operation.json
settings.json
quickgame.json
```

Combined save files are a migration option after the initial separate files are stable.

### Tests

Add:

- `SaveService_ProfileRoundTrip`
- `SaveService_CampaignProgressRoundTrip`
- `SaveService_OperationRoundTrip`
- `SaveService_MissingFileCreatesDefault`
- `SaveService_VersionMigrationKeepsKnownFields`

## Phase 9 - Campaign

### New Types

Create:

- `CampaignProgress` or existing `SagaProgress` compatibility storage
- `ChapterConfig`
- `CampaignMissionNodeConfig` or existing `SagaMissionNodeConfig` compatibility config
- `CampaignMissionLaunchData` or existing `SagaMissionLaunchData` compatibility payload
- `CampaignProgressService` or existing `SagaProgressService` compatibility wrapper
- `CampaignUnlockService` or existing `SagaUnlockService` compatibility wrapper

### ChapterConfig Fields

- Chapter id.
- Chapter title key.
- Map art reference.
- Planning camera / operation-map preview reference.
- Mission nodes.
- Chapter reward thresholds.

### CampaignMissionNodeConfig Fields

- Mission id.
- Mission archetype from `Gameplay_North_Star_And_Content_Grammar.md`.
- Node number.
- Title key.
- Description key.
- Chapter or district context.
- Primary threat family.
- Scenario setup reference.
- Objective configs.
- Star goals.
- Reward config.
- Consequence set.
- Target balance band.
- Validation checklist.
- Required previous mission.
- Required stars.
- Unlock rewards.

### Chapter 1 Content Plan

Use `Level_And_Mission_Content_Plan.md` for shared content rules and `SagaChapters/Saga_Chapter01_First_Response.md` as the source of truth for Chapter 1 mission-by-mission authoring. Together they define:

- The required mission spec template.
- The high-level Campaign chapter set.
- The Chapter 1 mission matrix.
- Detailed Chapter 1 specs for `saga.ch01.m01.first_contact` through `saga.ch01.m05.breach_assault`.
- Operation mission hooks.
- Skirmish probe mapping.
- Mission acceptance gate.

### Tests

Add:

- `CampaignProgress_FirstMissionUnlockedByDefault`
- `CampaignProgress_CompletionUnlocksNextMission`
- `CampaignProgress_StoresBestStarCount`
- `CampaignMissionLaunch_BuildsPayloadFromNode`

Current implementation note:

- `CampaignMapScreenController` applies the initial Chapter 1 unlock rule in the UI layer. Existing `SagaMapScreenController` / `SagaProgressStore` names may remain as runtime compatibility until renamed.
- Locked mission nodes remain selectable for their info panel but do not start a mission. Unlocked mission nodes update selected state, seed `ActiveMissionSession`, and route toward Mission Briefing when a shell router is present.

## Phase 10 - Operations

### New Types

Create:

- `OperationState`
- `DistrictState`
- `DistrictMetricSet`
- `OperationEvent`
- `OperationActionType`
- `OperationActionRequest`
- `OperationActionResult`
- `OperationSimulationService`
- `DistrictMissionGenerator`
- `IntelEvidenceItem`
- `IntelArchive`

Current implementation note:

- `OperationActionConfig`, `OperationDistrictActionModifier`, `OperationDistrictEventRule`, and `OperationActionConfigSet` provide the first authored, Resources-backed action tuning layer for Patrol/Scan/Aid/Raid/Repair/Evacuate/Build Outpost meter changes, secondary district consequences, threshold-based district alert events, operation supply costs/rewards, district-specific consequences, raid mission intent, and event copy. `OperationService` consumes those configs and blocks supply-gated actions when supplies are too low.
- `OperationService` provides the first live Operation state and action simulation: default districts, Patrol/Scan/Aid/Raid/Repair/Evacuate/Build Outpost meter changes, secondary trust/security/infrastructure/enemy-influence/heat/civilian-risk deltas, operation supply deltas, typed pending event rows, raid mission intent, and end-of-day pressure.
- `WarlineCaptureOperationRuntime` now persists Operation state through `SaveService`; actions and End Day save `operation.json`, and missing/empty saved district data normalizes back to the default operation state.
- `OperationDashboardScreenController`, `DistrictDetailScreenController`, and `WarlineCaptureOperationModalFlow` bind the first Operation UI slice to this state and modal flow. District Detail now exposes the six-action ActionGrid for Patrol, Drone Scan, Raid, Repair, Evacuate, and Build Outpost; dashboard/detail cards share a metric text contract for trust, security, infrastructure, enemy influence, heat, civilian risk, stability, and intel; Raid confirmation uses heat/civilian risk/security/trust instead of old proxy meters; End Day reports trust/security/heat/civilian-risk averages; and `OperationInboxScreenController`, `OperationEventsScreenController`, and `OperationCommandFeedScreenController` also surface pending Operation events and archived intel evidence into `SCN-15 Inbox`, `SCN-16 Events`, and `SCN-18 Command Feed`.
- `OperationEventData` carries category, severity, district, action, day, unread, source metric, and metric value metadata so the future Inbox/Event filters can consume the same saved ledger without replacing the first live UI binding.
- `OperationIntelEvidenceData` provides the first saved intel archive slice. Scan actions add district evidence rows with confidence, source event id, operation day, and unread state; `OperationIntelArchive` provides shared latest/count/read queries; `POP-08 Intel Reveal` reads the latest evidence row for the selected district and marks it read when the player chooses View Intel.

### DistrictState Fields

- District id/name.
- Security.
- Trust.
- Infrastructure.
- Enemy influence.
- Intel confidence.
- Civilian density.
- Heat.
- Active events.
- Recent activity.
- Known threat estimate.

### OperationState Fields

- Operation id.
- Current day.
- Region stability.
- Civilian trust.
- Threat level.
- Heat level.
- Force readiness.
- District list.
- Intel archive.
- Resource state.
- Pending missions.
- Completed operation actions.

### Actions

Patrol:

- Reduces enemy influence/heat slightly.
- May increase security.
- May reveal recent activity.

Drone Scan:

- Increases intel confidence.
- May reveal evidence.
- May increase heat if overused.

Aid:

- Increases trust/stability.
- Costs resources.

Raid:

- Requires confirm popup.
- Uses intel confidence and collateral risk.
- May generate tactical mission.
- Can reduce enemy influence or hurt trust if poor intel/collateral damage.

Repair:

- Improves infrastructure.
- Costs operation supplies.
- Current authored defaults improve stability/trust/security, reduce heat/civilian risk, and have a Port Breach utility repair modifier.

Evacuate:

- Reduces civilian density/risk.
- May reduce trust/infrastructure short-term.
- Current authored defaults reduce civilian risk strongly, raise heat, lower trust, and have an Old Market corridor modifier.

Build Outpost:

- Improves security and readiness.
- Costs operation supplies.
- Current authored defaults improve security/readiness, reduce enemy influence, raise heat, and have a North Bridge checkpoint modifier.

### End of Day Simulation

On end day:

- Resolve passive enemy influence changes.
- Update heat.
- Generate warnings/events.
- Apply trust/security/infrastructure drift.
- Save operation.
- Show end-of-day report.

### Tests

Add:

- `OperationState_NewGameCreatesDistricts`
- `OperationAction_PatrolReducesThreat`
- `OperationAction_DroneScanIncreasesIntel`
- `OperationAction_RaidWithLowIntelHasRisk`
- `OperationSimulation_EndDayGeneratesReport`
- `OperationSave_RoundTripPreservesDistricts`

## Phase 11 - AI Profiles and Encounters

### New Types

Create:

- `AIProfileDefinition`
- `EncounterTemplate`
- `EncounterDirector`
- `SpawnWaveConfig`
- `ThreatEventConfig`
- `MissionAISetup`

### AI Profiles

Initial profiles:

- Tutorial Cell
- Hidden Cell Network
- Defensive Garrison
- Armored Column
- Air Assault
- Mixed Force
- Random

Each profile defines:

- AI controller config reference.
- Preferred buildings.
- Preferred units.
- Preferred vehicles.
- Allowed tech.
- Economy multiplier.
- Aggression.
- Attack timing.
- Target priority.
- Threat warning behavior.

### Encounter Templates

Initial templates:

- Patrol Ambush.
- Convoy Attack.
- Base Defense.
- Breach Assault.
- Air Raid.
- Extraction.
- District Raid.

### EncounterDirector Responsibilities

- Spawn waves or activate AI behaviors from scenario timeline.
- Request threat warnings.
- Trigger objective events.
- Avoid spawning unfair waves directly on top of player.

### Tests

Add:

- `AIProfileDefinition_MapsToControllerConfig`
- `EncounterDirector_SchedulesWave`
- `EncounterDirector_RequestsThreatWarning`
- `EncounterTemplate_BuildsValidScenarioAdditions`

## Phase 12 - Gameplay Balance Data

### Balance Configs

Create:

- `EconomyBalanceConfig`
- `RewardBalanceConfig`
- `DifficultyBalanceConfig`
- `ProgressionBalanceConfig`
- `OperationBalanceConfig`

### Balance Areas

Tune:

- Starting resources.
- Building prices.
- Unit production costs.
- AI income multipliers.
- Mission reward amounts.
- XP curve.
- Star thresholds.
- Operation action costs.
- District drift rates.

### Validation

Add editor tests for data sanity:

- No negative prices/rewards.
- Unlock ids exist.
- Mission objective ids unique.
- Required scenario references exist.
- Chapter graph has no dead-end caused by missing unlock.

## Phase 13 - Opt-In Balance and Gameplay Probes

### Purpose

Balance and gameplay probes are developer tools for tuning WarlineCapture. They should help answer questions like "is this AI profile too aggressive?", "does this economy snowball too fast?", and "does this mission usually finish in the target time window?"

These probes are not build-validation tests. They must not be part of normal EditMode, PlayMode, Android, or CI validation gates, and they must not make builds fail because a gameplay value drifted.

Use `Balancing_Automated_Test_Plan.md` as the concrete implementation checklist. It records the current files under `Assets/Game/Scripts/Balance` and `Assets/Tests/Editor/Balance`, the short harness tests, the opt-in report probes, and the next data-sanity tests. The first shared probe-definition layer now covers `QuickCustom_Default_Medium` and `QuickCustom_Hard_Swarm`.

### Folder Layout

Create separate balance-only test/runtime folders:

```text
Assets/Tests/Editor/Balance
Assets/Game/Scripts/Balance
```

The current project does not use game/test asmdefs, so the first probes should live under `Assets/Tests/Editor/Balance` and rely on `[Explicit]` plus `[Category("Balance")]` to stay out of normal validation.

Suggested follow-up test assembly, after game/test asmdefs are introduced:

```text
Assets/Tests/Balance/WarlineCapture.BalanceTests.asmdef
```

The future balance test assembly should not be referenced by production assemblies. Keep it editor/test-only.

### Required Attributes and Filters

Balance probe tests should use:

```csharp
[Category("Balance")]
[Explicit]
```

Use `[Explicit]` for long, stochastic, or experimental probes. Short harness smoke checks may omit `[Explicit]`, but they still must keep the `Balance` category so CI can exclude them.

Normal validation commands must exclude the `Balance` category. Optional balance jobs may run only `Balance` tests and publish reports.

### New Types

Create:

- `BalanceScenarioConfig`
- `BalanceProbeDefinition`
- `BalanceSimulationRunner`
- `BalanceSimulationResult`
- `BalanceMetrics`
- `BalanceMetricSample`
- `BalanceOutcomeClassifier`
- `BalanceReportWriter`

Additional probe extensions:

- `BalanceBaselineSnapshot`
- `BalanceComparisonReport`
- `BalanceProbeMenuItems`

### Harness Rules

Balance probes should assert only on harness correctness:

- Scenario config loaded.
- Simulation started.
- Simulation completed or reached the configured time limit.
- No unexpected exception occurred.
- Metrics were collected.
- Report file was written.

Balance probes should not fail because:

- AI won too often.
- Match duration was outside the target range.
- Resource income was too high.
- Casualties were too high.
- A unit or building appears overpowered.

Those are balance findings and belong in the generated report.

### Metrics

Collect at minimum:

- Scenario id.
- Random seed.
- Winner and result reason.
- Simulation duration.
- Time to first attack.
- Time to first production.
- Time to first base breach.
- Resource income, spend, and float over time.
- Unit count and army value over time.
- Buildings built and destroyed.
- Unit losses by faction.
- Kill/death ratio.
- Civilian losses and collateral damage.
- Objective completion timing.
- Threat warning count and warning lead time.

### Outcome Classification

Reports should classify metrics without failing the test:

```text
Good
Watch
Problem
InvalidRun
```

Example match length classification:

- `Good`: 8 to 14 minutes.
- `Watch`: 6 to 8 minutes or 14 to 18 minutes.
- `Problem`: under 6 minutes or over 18 minutes.
- `InvalidRun`: simulation did not start, crashed, or produced incomplete metrics.

### Initial Probe Scenarios

Create repeatable named probes:

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

Each probe should run with a fixed seed by default. Later, optional sweeps can run multiple seeds and aggregate the distribution.

### Report Output

Write reports outside `Assets`:

```text
Library/WarlineCaptureBalanceReports
Temp/BalanceReports
```

Preferred formats:

- JSON for machine comparison.
- CSV for spreadsheet review.
- Markdown summary for quick reading.

Prefer `Library/WarlineCaptureBalanceReports` for Unity batchmode runs because Unity may clean `Temp` during shutdown. Do not write generated balance reports into `Assets` unless intentionally creating a reviewed baseline snapshot.

### Runner

Add a manual runner through one or both:

- Unity menu item: `WarlineCapture/Balance/Run Balance Probes`
- CLI method: `BalanceProbeRunner.RunAllBalanceProbes`

The runner should make it obvious that it is optional tuning tooling, not build validation.

### Build Validation Boundary

Keep one tiny normal validation test only if needed:

- The balance harness can instantiate a minimal fixed-seed scenario.
- The test does not judge balance quality.
- The test does not run long simulations.

All real balance probes stay opt-in under the `Balance` category.

## Phase 14 - Telemetry and Debug Tools

### Debug Views

Add development-only overlays:

- Active mission id.
- Objective states.
- Star goal states.
- Encounter director state.
- Operation district values.
- Reward grant log.

### Logs

Use structured logs only where useful:

- `[Objective]`
- `[MissionResult]`
- `[Reward]`
- `[SagaProgress]`
- `[Operation]`
- `[Encounter]`

Keep logs opt-in or expected in tests to avoid Unity Test Framework failures.

## Implementation Order

Recommended implementation order for the UI/UX shell:

1. `QuickGameConfig` and launch payload.
2. `GameBootstrap.BeginGameplay(GameLaunchPayload payload)` compatibility path.
3. `ObjectiveConfig`, `ObjectiveManager`, and first objective types.
4. `MissionResultData` and `MissionResultBuilder`.
5. `RewardConfig` and `RewardService`.
6. `SaveService` with profile/saga/quickgame files.
7. `SagaProgress`, `ChapterConfig`, and Chapter 1 mission configs.
8. `OperationState`, district actions, and operation save.
9. AI profiles and encounter templates.
10. Balance configs and content pass.
11. Opt-in balance probes and report writer.

## First Coding Milestone

Build this first because it directly supports the planned UI/UX work:

### Deliverable

Skirmish gameplay launch with objective/result skeleton.

### Scope

- `QuickGameConfig`
- `GameLaunchPayload`
- `ScenarioSetup`
- default quick scenario loader.
- default 3D operation-map ids, planning camera id, and minimap projection id in the launch payload.
- `BeginGameplay(GameLaunchPayload payload)` overload.
- objective config with Sandbox and Destroy All Enemies support.
- result builder for Victory/Defeat/Abandoned.

### Acceptance Criteria

- Existing direct play still works.
- Skirmish UI can create a config and launch current gameplay.
- AI settings from Skirmish apply to the match.
- The payload includes stable operation-map, terrain, planning-camera, and minimap-projection ids even before every final art pass is complete.
- Objective manager can run in Sandbox mode without ending the match.
- Destroy All Enemies objective can produce victory in a unit test.
- Full EditMode tests pass.

## Second Coding Milestone

### Deliverable

Mission results, stars, and rewards.

### Scope

- Mission result data.
- Star scoring.
- Reward configs.
- Reward service.
- Basic player profile.

### Acceptance Criteria

- Match result popup can show real data.
- Rewards grant Commander XP, Credits, and unlocks.
- Campaign progress can store best mission stars. Existing `SagaProgress` storage can remain as the compatibility backing store until renamed.
- Save/load roundtrip works.

## Third Coding Milestone

### Deliverable

Campaign Chapter 1 playable loop.

### Scope

- Campaign progress, using `SagaProgress` only as a compatibility wrapper where needed.
- Chapter and mission node configs.
- Mission briefing data.
- Loadout payload.
- 3 playable missions.

### Acceptance Criteria

- Completing Mission 1 unlocks Mission 2.
- Best stars persist.
- Reward preview and grant match.
- Android build passes.

## Fourth Coding Milestone

### Deliverable

Operations prototype.

### Scope

- Operation state.
- 5 to 8 districts.
- Patrol/scan/aid/raid actions.
- End day report.
- Raid mission generation.

### Acceptance Criteria

- Operation save/load persists day and district values.
- District actions have visible and tested consequences.
- Raid can route to a 3D operation-map match.
- Result can update district state.

## Risks and Guardrails

- Avoid making `MenuView.cs` larger. New gameplay features should not be added there.
- Avoid hard-coded mission ids in UI scripts. Use config objects.
- Avoid persisting raw ECS state initially.
- Avoid hidden win/loss rules. Objectives should be visible in briefing and HUD.
- Avoid reward duplication. First-clear and repeat rewards must be explicit.
- Avoid unexpected test logs. Gameplay diagnostics should be opt-in or covered by `LogAssert`.
- Keep Skirmish as the first gameplay mode because it uses existing systems and is the lowest-risk bridge from UI to simulation. Existing QuickCustom runtime names can remain behind the player-facing Skirmish label until migration.

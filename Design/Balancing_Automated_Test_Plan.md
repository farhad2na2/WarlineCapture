# WarlineCapture Balancing Automated Test Plan

Date: 2026-05-05

## Purpose

This document is the implementation plan for automated balance support in WarlineCapture. It connects the existing balance probe docs, current Unity test files, and economy/reward specs so an implementation agent can extend balancing tests without turning tuning values into build-breaking assertions.

Balance automation has two jobs:

- Protect the balance harness, reports, data contracts, and scenario setup from breaking.
- Produce repeatable reports that economy and gameplay balancers can use to tune AI pressure, resources, rewards, stores, objectives, and mission pacing.

Balance automation must not fail normal builds because a mission is too short, an AI profile wins too often, a resource float is high, or a reward amount needs tuning. Those are report findings, not validation failures.

## Source Documents

Read these in order before changing balance tests:

1. `Economy_Reward_Design.md`
2. `Combat_Catalog_And_Upgrade_Design.md`
3. `BalanceConfigs/Combat_Balance_Config_v0_1.json`
4. `VisualConfigs/Combat_Visual_Config_v0_1.json`
5. `Gameplay_North_Star_And_Content_Grammar.md`
6. `Level_And_Mission_Content_Plan.md`
7. `SagaChapters/Saga_Chapter01_First_Response.md`
8. `Gameplay_Features_High_Level_Spec.md`
9. `Gameplay_Features_Detailed_Spec.md`
10. `3D_SingleMap_Gameplay_Direction.md`
11. `M01_FirstContact_Production_Contract.md`
12. `Monetization/Monetization_Store_Catalog.md`
13. `GAME_DESIGN_REFERENCE.md`
14. `AI_CONTROLLER_DESIGN.md`

The balance probe sections in the high-level and detailed gameplay specs remain the source for the opt-in probe philosophy. This document adds the concrete implementation checklist and automated test matrix.

Mission-specific target bands should come from `Gameplay_North_Star_And_Content_Grammar.md`, `Level_And_Mission_Content_Plan.md`, and the relevant chapter doc first. Balance probes classify whether a mission run is inside those bands; they do not invent the content target during test implementation.

## Current Implementation Snapshot

Existing runtime/helper files:

| File | Current Role |
|---|---|
| `Assets/Game/Scripts/Balance/BalanceMetrics.cs` | Serializable metrics payload for Quick Custom balance reports. |
| `Assets/Game/Scripts/Balance/BalanceMetricSample.cs` | Lightweight sampled runtime-counter payload used by opt-in probes until full simulation metrics replace it. |
| `Assets/Game/Scripts/Balance/BalanceProbeDefinition.cs` | Shared probe metadata/config/sample definition used by multiple fixed-seed probe scenarios. |
| `Assets/Game/Scripts/Balance/BalanceOutcomeClassifier.cs` | Classifies report outcomes as `Good`, `Watch`, `Problem`, or `InvalidRun`. |
| `Assets/Game/Scripts/Balance/BalanceReportWriter.cs` | Writes JSON and Markdown reports outside `Assets`, currently under `Library/WarlineCaptureBalanceReports`. |
| `Assets/Tests/Editor/Balance/QuickCustomBalanceProbe.cs` | Opt-in report-producing Quick Custom probes, currently `QuickCustom_Default_Medium` and `QuickCustom_Hard_Swarm`. |
| `Assets/Tests/Editor/Balance/QuickCustomBalanceProbeTests.cs` | Explicit opt-in balance report test using `[Category("Balance")]`. |
| `Assets/Tests/Editor/Balance/BalanceProbeRunner.cs` | Unity menu/CLI runner for individual Quick Custom probes and `RunAllBalanceProbes`. |

Existing supporting tests:

| File | Balance-Relevant Coverage |
|---|---|
| `Assets/Tests/Editor/AISettingsValidationTests.cs` | Difficulty, starting Materials/Fuel/Oil, income/production rates, build speed, production speed, and AI cadence mapping. Legacy tactical Money fields remain migration coverage only. |
| `Assets/Tests/Editor/GameRuntimeStatsTests.cs` | Resource, production, build, and casualty counter correctness. |
| `Assets/Tests/Editor/AIEconomyValidationTests.cs` | Existing tactical economy behavior. |
| `Assets/Tests/Editor/AIEndToEndValidationTests.cs` | AI economy/build/production/squad/targeting/combat vertical slice. |

## Test Categories

| Category | Runs In Normal Validation | Attributes | Purpose |
|---|---|---|---|
| Balance harness contract tests | Yes when specifically selected; safe to run with EditMode tests. | `[Category("Balance")]` | Validate classifier, metrics, and report writer behavior. These tests must not simulate long matches. |
| Opt-in balance probes | No. | `[Category("Balance")]` and `[Explicit]` | Produce balance reports for human review. These can take longer and write reports. |
| Data sanity tests | Yes. | Normal `[Test]` or `[Category("BalanceData")]` | Validate configs have nonnegative values, valid ids, and no missing references. |
| Full simulation sweeps | No. | `[Category("Balance")]` and `[Explicit]` | Multi-seed or long-running probes for tuning distributions. |

Normal build validation should exclude long-running balance probes. It may run short harness contract tests because they assert only infrastructure behavior.

## Automated Test Matrix

### Current Tests To Keep

| Test | Type | Required Behavior |
|---|---|---|
| `QuickCustom_Default_Medium_ProducesBalanceReport` | Opt-in probe | Writes JSON and Markdown report files. Must stay `[Explicit] [Category("Balance")]`. |
| `AISettingsRuntimeState_AppliesDifficultyEconomyAndCadenceMultipliers` | Normal EditMode | Confirms AI tuning knobs affect economy and cadence. |
| `Snapshot_AccumulatesResourceAndBuildStats` | Normal EditMode | Confirms runtime stat counters feed balance metrics correctly. |

### New Harness Contract Tests

| Test | Type | Required Behavior |
|---|---|---|
| `BalanceOutcomeClassifier_ClassifiesGoodRuntimeSnapshot` | Short balance harness | Confirms a representative valid metric payload classifies as `Good`. |
| `BalanceOutcomeClassifier_ProblemClassificationDoesNotRepresentHarnessFailure` | Short balance harness | Confirms a bad balance result is represented as `Problem` in data, not as an exception. |
| `BalanceMetrics_FromQuickGameConfig_UsesCanonicalStartingCreditsField` | Short balance harness | Confirms Quick Custom metrics expose player-facing `StartingCredits` while still mapping from the existing `AISettingsRuntimeState.StartingMoney` enum. |
| `BalanceReportWriter_WritesReportsOutsideAssetsWithNonValidationNotice` | Short balance harness | Confirms report output is outside `Assets` and Markdown states it is not a build-validation gate. |

### Data Sanity Tests To Add Next

| Test | Reads | Required Behavior |
|---|---|---|
| `EconomyRewardDesign_CanonicalRewardTypesMatchRewardEnum` | `RewardType` enum and economy doc. | All code reward types map to `Economy_Reward_Design.md`. |
| `StoreCatalog_ItemsMapToCanonicalRewardTypes` | Store catalog config. | Every product content line maps to Credits, Command, Rush Tickets, BlueprintParts, GearModule, Cosmetic, UnitUnlock, BuildingUnlock, SupportAbilityUnlock, or OperationSupply. No product grants match Materials/Fuel/Oil. |
| `ChapterOneCatalog_RewardConfigsHaveValidIdsAmountsTargetsAndFallbacks` | Chapter 1 mission reward configs. | Implemented first mission-reward sanity gate: unique reward ids, positive amounts, required target ids, and first-clear duplicate fallbacks. |
| `OperationActionConfig_ResourceCostsAndMetricDeltasAreValid` | Operation action configs. | Costs are nonnegative; metric deltas target valid Operation metrics. |
| `MissionRewardPreviewAndGrantUseSameRewardConfig` | Mission configs and result flow. | Briefing previews and result grants reference the same `RewardConfig`. |
| `CombatBalanceConfig_AllIdsUnique` | `BalanceConfigs/Combat_Balance_Config_v0_1.json`. | Unit, building, ability, and upgrade-track ids are unique. |
| `CombatBalanceConfig_AllVisualRefsExist` | Combat balance and visual configs. | Every balance `visualCatalogId` has a matching visual entry. |
| `CombatBalanceConfig_NoVisualPathsInBalanceData` | Combat balance config. | Balance config contains no world/icon/portrait/VFX asset paths. |
| `CombatVisualConfig_NoBalanceValues` | Combat visual config. | Visual config contains no costs, HP, damage, range, cooldown, production time, or upgrade-cost values. |
| `CombatBalanceConfig_AllAbilityRefsExist` | Combat balance config. | Every unit/building ability id resolves to an ability config. |
| `CombatBalanceConfig_AllUpgradeTrackRefsExist` | Combat balance config. | Every unit/building upgrade track resolves to an upgrade-track config. |
| `CombatAbilityConfig_AllAvailabilitySpecsComplete` | Combat balance config. | Every ability has unlock moment, modes, UI surfaces, precondition, locked/disabled state, runtime owner, state owner, and validation test names. |
| `CombatUpgradeTrackConfig_AllAvailabilitySpecsComplete` | Combat balance config. | Every upgrade track has unlock moment, source reward types, store eligibility, apply window, runtime owner, target resolution, and validation test names. |
| `CombatUpgradeTrackConfig_AllPlayerTracksResolveItems` | Combat balance config. | Every player-facing upgrade track has at least one resolved item id. Enemy escalation tracks resolve enemy units only. |
| `CombatAbilityUpgradeUnlocks_AlignWithSagaOperationAndStoreRules` | Combat balance config and Saga/Operation/store docs. | Ability and upgrade unlock moments match chapter pacing and store grants remain parts-only after earn path. |
| `ScenarioSetup_MapViewIdsResolve` | Mission configs, Chapter docs, and operation-map definitions. | Every mission resolves `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, camera bounds, and required metadata anchors. |
| `OperationMapDefinition_MetadataSupportsGameplay` | Operation-map metadata assets. | Walkable, road, blocker, spawn, route, objective, attack-target, build-zone, minimap projection, deployment-zone, civilian-risk, and camera-bound data exists for the mission's required gameplay. |
| `M01_FirstContact_ProductionContract_ResolvesTargets` | M01 production contract, mission catalog, operation-map metadata, UI element ids. | M01 ids, anchors, command reason codes, FTUE targets, operation-map camera/minimap ids, and result/reward ids resolve before the playable slice is marked ready. |

### Opt-In Probe Scenarios To Implement

| Probe Id | Source | Metrics Focus |
|---|---|---|
| `QuickCustom_Default_Medium` | Existing implemented probe. | Baseline Quick Custom configuration, runtime stats, report writer. |
| `QuickCustom_Hard_Swarm` | Implemented `QuickGameConfig` with Swarm, Hard, high pressure, frequent attacks. | Casualties, production pressure, aggressive swarm configuration sanity. |
| `Campaign_Chapter1_Mission1` | `saga.ch01.m01.first_contact` from `SagaChapters/Saga_Chapter01_First_Response.md`. | Tutorial length, first contact timing, no-loss star, first-clear rewards. |
| `Campaign_Chapter1_Mission2` | `saga.ch01.m02.establish_base` from `SagaChapters/Saga_Chapter01_First_Response.md`. | Build timing, production timing, resource float, first threat. |
| `Campaign_Chapter1_Mission3` | `saga.ch01.m03.radar_warning` from `SagaChapters/Saga_Chapter01_First_Response.md`. | Warning lead time, convoy timing, base damage, losses. |
| `Campaign_Chapter1_Mission4` | `saga.ch01.m04.airlift` from `SagaChapters/Saga_Chapter01_First_Response.md`. | Transport timing, extraction result, Fuel spend, transport survival. |
| `Campaign_Chapter1_Mission5` | `saga.ch01.m05.breach_assault` from `SagaChapters/Saga_Chapter01_First_Response.md`. | Breach timing, core destruction, vehicle/support survival, star distribution. |
| `Operation_Raid_MediumIntel` | Operation action simulation. | Intel spend, raid confidence, trust/security risk. |
| `BaseDefense_HeavyAir` | Encounter template. | Warning lead time, anti-air readiness, base damage. |
| `EconomyRush_FastBuild` | Quick Custom/economy tuning. | Resource income, spend, float, production queue pressure. |

## Metrics Contract

Balance reports should grow toward this common payload. Add fields gradually as the matching systems become real.

| Metric | Source |
|---|---|
| Probe id, scenario id, seed | `BalanceProbeDefinition` or current probe constants. |
| Quick Custom config values | `QuickGameConfig`. |
| Winner and result reason | `MissionResultData` or probe runner. |
| Match duration | Simulation runner or sampled duration. |
| Time to first attack, production, base breach | Encounter/AI/combat events. |
| Resource income, spend, float | Tactical economy and wallet/economy events. |
| Persistent Credits/Command, match Materials/Fuel/Oil, Rush Ticket inventory, and Operation metric deltas | Economy events from `Economy_Reward_Design.md`, grouped by ownership scope. |
| Reward grants | `RewardGrantResult`. |
| Store grants and spends | `StoreCatalogItem`, `PurchaseGrant`, economy events. |
| Unit count and army value | Roster/combat systems. |
| Unit/building catalog ids, upgrade tiers, and army value bands | `BalanceConfigs/Combat_Balance_Config_v0_1.json`. |
| Buildings built/destroyed | Build and combat systems. |
| Unit losses and kill/death ratio | Runtime stats/combat events. |
| Civilian losses/collateral damage | Civilian and objective systems. |
| Objective completion timing | Objective runtime. |
| Threat warning count and lead time | Threat warning runtime. |
| Operation trust/security/intel/infrastructure deltas | Operation action and day summary systems. |
| Mission archetype, threat family, mission id, and target band id | `Gameplay_North_Star_And_Content_Grammar.md`, `Level_And_Mission_Content_Plan.md`, relevant `SagaChapters` doc, and mission config. |

## Classification Rules

Classifications are report labels only.

| Classification | Meaning |
|---|---|
| `Good` | Inside target band. |
| `Watch` | Outside ideal band, but usable for review. |
| `Problem` | Strong tuning concern. The test still passes when the harness and report succeeded. |
| `InvalidRun` | Scenario failed to start, crashed, produced incomplete metrics, or did not write a report. This can fail the test because it is a harness failure. |

The current `BalanceOutcomeClassifier` uses match duration, economy activity, and casualties. Future classifiers should add economy and reward classifications without throwing exceptions for tuning problems.

## Implementation Sequence For The Next Agent

1. Keep `QuickCustom_Default_Medium` working before adding new probes.
2. Add or maintain short balance harness tests in `Assets/Tests/Editor/Balance` without `[Explicit]`; keep `[Category("Balance")]`.
3. Keep long report-producing tests `[Explicit] [Category("Balance")]`.
4. Extend `BalanceMetrics` with scoped canonical fields from `Economy_Reward_Design.md`: persistent Credits/Command, match Materials/Fuel/Oil, Rush Ticket inventory, reward grants, store spends, and Operation metric deltas.
5. Add `BalanceProbeDefinition` and `BalanceSimulationRunner` only after a second probe needs shared scenario/run logic.
6. Add CSV output after JSON/Markdown are stable.
7. Add baseline snapshots only for reviewed values. Generated probe reports stay outside `Assets`.
8. Add config sanity tests when `RewardConfig`, `StoreCatalogItem`, `OperationActionConfig`, and `EconomyBalanceConfig` exist as code/data assets.
9. Add multi-seed sweeps last. They must stay explicit and report-oriented.

## Commands

Focused fast harness tests:

```text
Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter BalanceHarnessContractTests
```

Current opt-in report probe:

```text
Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter QuickCustomBalanceProbeTests.QuickCustom_Default_Medium_ProducesBalanceReport
```

Generated reports should be inspected under:

```text
Library/WarlineCaptureBalanceReports
```

## Acceptance Criteria

- Short harness tests pass without running long simulations.
- Opt-in probes write JSON and Markdown reports outside `Assets`.
- Balance classifications appear in reports but do not fail tests by themselves.
- Store, reward, and economy data tests use the canonical resource/reward vocabulary from `Economy_Reward_Design.md`.
- New probes use fixed seeds by default.

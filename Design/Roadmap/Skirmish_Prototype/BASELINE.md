# Skirmish source baseline and delivery risks

Inspected 2026-09-17 at campaign commit `b9d17a700`. This is a source inspection, not a new gameplay acceptance run. S0 in [the plan](PLAN.md) must verify the observed paths in a live Editor.

## Existing foundations

| Area | Evidence in the repository | Consequence for the prototype |
|---|---|---|
| Setup presentation | [QuickCustomScreenView](../../../Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.cs), [setup builder](../../../Assets/Game/Scripts/Editor/SkirmishSetupPrefabBuilder.cs), [screen tests](../../../Assets/Tests/Editor/SkirmishSetupScreenTests.cs) | Reuse SCN-13 and the established shell; simplify unsupported controls |
| Configuration | [QuickGameConfig](../../../Assets/Game/Scripts/Configs/QuickGameConfig.cs) | Has enemy, AI, economy, rule, visibility and seed fields. Defaults include one Normal enemy, full intel and no fog; fields existing does not prove runtime consumption |
| Setup adapter | [UiRuntimeAdapters](../../../Assets/Game/Scripts/Composition/UiRuntimeAdapters.cs) | `QuickCustomGameConfigStore` retains only `AISettingsSnapshot`. `Current` reconstructs other fields from defaults, losing selected enemy type, rule, visibility, resource preset and seed |
| Launch request | `MatchLaunchCommand` in the same adapter; [flow helper](../../../Assets/Game/Scripts/UI/Screens/QuickCustomScreenFlowUiSystemHelper.cs) | Constructor currently discards the config store; launch queues `EnterMatch` without a full skirmish snapshot. Do not claim setup choices are honored until traced and tested |
| Operation-map resolution | [MatchSceneView.OperationMapLaunch](../../../Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs) | Resolves campaign requests or fallback values. Add explicit skirmish selection ownership without breaking campaign resolution |
| Authored map/scenario | [ScenarioSetup_Skirmish_DesertBaseStandard](../../../Assets/Game/Configs/OperationMaps/Scenarios/ScenarioSetup_Skirmish_DesertBaseStandard.asset) | Existing IDs: `scenario.skirmish.desert_base_standard`, `opmap.skirmish.desert_base_01`; two deployment anchors. Their existence is not validation of base spacing/build areas |
| Opponent | [AIBuildPlannerSystem](../../../Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs), [AIProductionSystem](../../../Assets/Game/Scripts/Systems/AIProductionSystem.cs), [AISquadSystem](../../../Assets/Game/Scripts/Systems/AISquadSystem.cs), [AITargetingSystem](../../../Assets/Game/Scripts/Systems/AITargetingSystem.cs), [AICombatOrderSystem](../../../Assets/Game/Scripts/Systems/AICombatOrderSystem.cs) | Reuse current systems; verify initialization, faction ownership, actual requests and outcomes in the preset |
| Economy | [FactionEconomyStartupSystem](../../../Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs), [Materials startup](../../../Assets/Game/Scripts/Systems/FactionTacticalMaterialsStartupSystemHelper.cs), [AI build policy](../../../Assets/Game/Scripts/Systems/AIBuildPlanningPolicySystemHelper.cs) | Materials support exists alongside legacy Money fields. Audit UI costs and both factions' deductions; do not seed account Credits as match funds |
| Persistence | [SaveDataModel](../../../Assets/Game/Scripts/Persistence/SaveDataModel.cs), [SaveService](../../../Assets/Game/Scripts/Persistence/SaveService.cs) | `QuickGameSaveData` currently holds only presetId, enemyCount, difficulty and fogOfWar; defaults differ from QuickGameConfig. Version/migrate and round-trip the supported full snapshot |
| Buildings | [Barracks](../../../Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset), [Field Fabrication Depot](../../../Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Ammunition_Depot_Config.asset) | Existing assets, damaged/destroyed presentation and production can be reused. Barracks currently authors a four-unit batch and 90 Materials construction cost; verify all dependent costs before tuning |
| Automated checks | SkirmishSetupScreenTests, QuickCustomBalanceProbeTests, AISettingsOwnershipTests, AIEndToEndValidationTests, AIProductionValidationTests, AIBuildPlannerValidationTests and campaign readiness checks | Starting regression inventory, not a declaration that all tests currently pass for this new preset |

The older [Skirmish implementation spec](../../Skirmish_Mode_Implementation_Spec.md) references prior controller paths/global runtime state and permits an M1 fallback. Those are historical design assumptions. The current prototype must use the actual source boundaries above and its own mode identity.

## Initial risk register

| ID | Risk | Priority | Resolution / evidence required |
|---|---|---|---|
| SK-01 | Setup values discarded before launch or silently reset on revisit | P0 | Full snapshot round-trip, save migration and startup readback in S1 |
| SK-02 | Generic EnterMatch inherits campaign objectives/tutorial/rewards or stale faction settings | P0 | Explicit mode/session ownership and Campaign → Skirmish → Campaign regression |
| SK-03 | A good-looking setup launches an inert opponent | P0 | Trace actual AI startup, spending, production, pathing and attacks during S3 |
| SK-04 | Resource migration leaves Credits costs or unusable starting Fuel | P1 | Match budget ledger, usable-storage verification and wallet isolation |
| SK-05 | Shared map/prefab changes regress M3 road gates or M5 compound | P1 | Isolated scenario overlays; preserve anchors; rerun affected mission checks |
| SK-06 | Large map creates long travel, camera hunting or last-enemy cleanup | P1 | Measured route times, visible Main Base objective, bounded match and two meaningful approaches |
| SK-07 | Production/population/logistics stalls have no player explanation | P1 | Cause-specific status and recovery cases, including full storage and blocked delivery |
| SK-08 | Recommended squad cards reorder during a tap or select unintended units | P1 | Stable group identity/slots, screen-position selection checks under combat |
| SK-09 | A stored neutral avatar/locale or gameplay voice survives a mode/result transition | P1 | Shared portrait/language routing and terminal audio guards retained |
| SK-10 | Internal automation succeeds but players cannot explain the objective or enjoy choices | Acceptance | Uncoached observation with separate findings, not a scripted victory-only sign-off |
| SK-11 | More AI/production entities exceed mobile performance or leak across replays | Acceptance | Bounded counts, allocation/cleanup tests and explicit later phone profiling |

Priorities: P0 blocks a usable match; P1 breaks expected behavior or clarity. No risk is marked fixed by this document. Create an implementation issue register with reproduction, owner, change, affected systems and exact evidence during S0.

## Decisions fixed for this plan

One Base Assault preset; one map; one Normal AI opponent; limited ground roster; fully revealed map; no new gameplay buttons; no persistent rewards; EN/FA parity; existing mobile HUD; configuration-owned balance; separate skirmish session and result policy.

## Values to establish in S0/S3

Exact base coordinates and path lengths; starting Materials/Fuel/Oil and capacities; fabrication/refinery/delivery throughput; vehicle counterplay; visible population count; achievable pressure interval and duration. These are bounded tuning tasks, not missing permission to inspect or implement the authorized scope. Do not hide an unsupported system with a test-only fallback.

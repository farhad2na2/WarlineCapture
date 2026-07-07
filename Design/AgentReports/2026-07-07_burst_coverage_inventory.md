# Burst Coverage Inventory - 2026-07-07

## Scope
Inventory runtime `ISystem` files under:

- `Assets/Game/Scripts/Systems`
- `Assets/Game/Scripts/Rendering/Systems`
- `Assets/Game/Scripts/UI/Shell/Ecs`

This report supports `Design/Architecture/perf_architecture_reaudit_followup_tracker.md` Phase 1.

## Summary

| Metric | Count |
|---|---:|
| Runtime `ISystem` files | 130 |
| Files with `[BurstCompile]` | 57 |
| Files without `[BurstCompile]` | 73 |
| Unclassified no-Burst `ISystem` files before this slice | 1 |
| Unclassified no-Burst `ISystem` files after this slice | 0 |
| Low-risk `Burstable as-is` additions identified | 0 |
| Files intentionally left no-Burst | 73 |

## Decision
No `[BurstCompile]` attributes were added in this slice. The only unclassified no-Burst file was `TacticalFollowAttackCinematicSystem`, and inspection showed it is not a safe Burst candidate because it uses `EntityManager`, writes tactical-follow camera state, and can create/name the target entity. It is now classified in `EcsBurstHotPathArchitectureTests` as a managed camera cinematic orchestration edge.

The existing architecture guardrail already enforces that runtime no-Burst `ISystem` files are classified. This slice closes the stale gap instead of adding a duplicate guardrail.

## Category Totals

| Category | Count |
|---|---:|
| Managed bootstrap edge | 12 |
| Managed debug edge | 1 |
| Managed diagnostic edge | 7 |
| Managed gameplay orchestration edge | 37 |
| Presentation only | 9 |
| UI shell managed edge | 7 |

## Full No-Burst Ledger

### Managed bootstrap edge
- `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs` - startup projection boundary; writes initial AI faction control state from runtime setup data.
- `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs` - startup projection boundary; initializes AI plan entries before recurring AI systems consume them.
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs` - startup orchestration boundary; creates AI runtime entities and buffers rather than running recurring simulation.
- `Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs` - startup/native-container initialization boundary; not a recurring simulation hot path.
- `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs` - startup projection boundary; initializes faction economy state before recurring economy ticks.
- `Assets/Game/Scripts/Systems/FixedWingRunwayHomeInitializationSystem.cs` - startup initialization boundary; resolves runway home state for fixed-wing units.
- `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs` - startup spawn-cell projection boundary; computes initial faction spawn cells before match simulation.
- `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs` - startup spawn apply boundary; creates initial unit requests and should remain separate from recurring simulation.
- `Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs` - startup/reset boundary; clears initial unit spawn state for match bootstrap.
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs` - startup spawn/config projection boundary; entity creation and prefab/config projection stay managed.
- `Assets/Game/Scripts/Systems/MapSurfaceFlatEquivalentBootstrapSystem.cs` - bootstrap/blob-builder boundary; not a recurring simulation hot path.
- `Assets/Game/Scripts/Systems/RuntimeGridDeduplicationSystem.cs` - startup/runtime-grid ownership boundary; native-container disposal and one-time cleanup stay managed.

### Managed debug edge
- `Assets/Game/Scripts/Systems/SelectedUnitDebugFireSystem.cs` - developer debug input boundary; intentionally managed and not production gameplay policy.

### Managed diagnostic edge
- `Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs` - diagnostic flush boundary; managed log formatting outside gameplay hot paths.
- `Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogFlushSystem.cs` - diagnostic flush boundary; managed log formatting outside gameplay hot paths.
- `Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs` - pre-game diagnostics boundary; managed reporting only.
- `Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs` - diagnostic flush boundary; managed log formatting outside gameplay hot paths.
- `Assets/Game/Scripts/Systems/UnitMoveTargetDiagnosticSystem.cs` - diagnostic boundary; managed reporting only.
- `Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticLogFlushSystem.cs` - diagnostic flush boundary; managed log formatting outside gameplay hot paths.
- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetDiagnosticLogFlushSystem.cs` - render-budget diagnostic flush boundary; managed log formatting only.

### Managed gameplay orchestration edge
- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs` - budgeted AI combat orchestration boundary; owns diagnostics, breach targeting, command-buffer order writes, and caps work by interval, squads, and unit writes.
- `Assets/Game/Scripts/Systems/AIProductionSystem.cs` - budgeted AI production orchestration boundary; interval-gated production planning, economy writes, production requests, and diagnostics stay managed.
- `Assets/Game/Scripts/Systems/AttackOrderCommandSystem.cs` - selection command boundary; consumes low-cardinality attack command requests and writes command state.
- `Assets/Game/Scripts/Systems/BuildingEntityManagerAccessSystem.cs` - building composition bridge; provides managed EntityManager access to non-ECS building runtime code.
- `Assets/Game/Scripts/Systems/BuildingGameplayChildSystem.cs` - building composition bridge; coordinates child gameplay systems through managed runtime context.
- `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs` - building composition bridge; exposes grid data to managed building placement/runtime code.
- `Assets/Game/Scripts/Systems/BuildingPlacementQueryCompositionSystem.cs` - building composition bridge; adapts placement query state for managed UI/runtime callers.
- `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs` - building spawn bridge; resolves managed spawn prefab references to ECS prefab entities.
- `Assets/Game/Scripts/Systems/BuildingStartupConfigProjectionSystem.cs` - building startup projection boundary; maps authored building setup into ECS runtime data.
- `Assets/Game/Scripts/Systems/BuildingTargetMoveOrderSystem.cs` - building command boundary; drains low-cardinality building-target move requests and delegates immediate unit move commands.
- `Assets/Game/Scripts/Systems/CitizenMovementCommandSystem.cs` - citizen command boundary; drains explicit citizen move requests and delegates immediate unit move commands.
- `Assets/Game/Scripts/Systems/FocusedUnitCommandSystem.cs` - selection focus command boundary; consumes low-cardinality focus requests from UI/input.
- `Assets/Game/Scripts/Systems/RtsSelectionAttackTargetModeCommandSystem.cs` - selection command boundary; consumes low-cardinality attack target mode requests.
- `Assets/Game/Scripts/Systems/RtsSelectionBoardTargetModeCommandSystem.cs` - selection command boundary; consumes low-cardinality transport board mode requests.
- `Assets/Game/Scripts/Systems/RtsSelectionCancelActiveCommandModeSystem.cs` - selection command boundary; consumes low-cardinality cancel command mode requests.
- `Assets/Game/Scripts/Systems/RtsSelectionDeselectAllCommandSystem.cs` - selection command boundary; consumes low-cardinality deselect requests.
- `Assets/Game/Scripts/Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs` - selection command boundary; consumes low-cardinality immediate selected-unit requests.
- `Assets/Game/Scripts/Systems/RtsSelectionMissileLauncherRadarAttackCommandSystem.cs` - selection command boundary; consumes low-cardinality radar attack requests.
- `Assets/Game/Scripts/Systems/RtsSelectionModeCommandSystem.cs` - selection command boundary; consumes low-cardinality selection mode requests.
- `Assets/Game/Scripts/Systems/RtsSelectionMoveTargetModeCommandSystem.cs` - selection command boundary; consumes low-cardinality move target mode requests.
- `Assets/Game/Scripts/Systems/RtsSelectionScanTargetModeCommandSystem.cs` - selection command boundary; consumes low-cardinality scan target mode requests.
- `Assets/Game/Scripts/Systems/RtsSelectionSelectAllCommandSystem.cs` - selection command boundary; consumes low-cardinality select-all requests.
- `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs` - runtime diagnostics boundary; gathers and formats diagnostic state outside Burst hot paths.
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs` - runtime state boundary; owns low-cardinality match/input state for managed UI and systems.
- `Assets/Game/Scripts/Systems/ScanIntelCommandSystem.cs` - tactical command boundary; consumes low-cardinality scan requests and writes command state.
- `Assets/Game/Scripts/Systems/SelectedMoveOrderCommandSystem.cs` - selection command boundary; expands one selected-move request into unit move requests.
- `Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicSystem.cs` - camera cinematic orchestration edge; consumes low-cardinality attack VFX requests and writes temporary tactical-follow camera target state through EntityManager.
- `Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs` - transport command boundary; consumes low-cardinality board requests and writes boarding command state.
- `Assets/Game/Scripts/Systems/UnitAttackOrderRequestSystem.cs` - selection command boundary; expands low-cardinality attack requests into unit attack order state.
- `Assets/Game/Scripts/Systems/UnitMoveOrderRequestSystem.cs` - selection command boundary; consumes queued move requests before pathfinding.
- `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs` - disabled command helper; exposes move-order methods to composition systems and has no scheduled runtime tick.
- `Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs` - detached pathfinding job orchestration boundary; expensive path search runs in Burst `PathfindBatchJob`, while this shell owns native snapshot lifetime, pending job state, diagnostics, and result playback.
- `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs` - disabled command helper; exposes attack/target-order methods to composition systems and has no scheduled runtime tick.
- `Assets/Game/Scripts/Systems/UnitTransportAirPickupSystem.cs` - disabled transport helper; exposes pickup methods to transport composition code and has no scheduled runtime tick.
- `Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs` - disabled transport helper; exposes capacity helpers to transport code and has no scheduled runtime tick.
- `Assets/Game/Scripts/Systems/UnitTransportDeployOrderSystem.cs` - transport deploy orchestration boundary; processes at most one active deploy order per frame and delegates disembark/move commands through managed helpers.
- `Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs` - disabled transport helper; exposes passenger-state helpers and has no scheduled runtime tick.

### Presentation only
- `Assets/Game/Scripts/Systems/CitizenPrefabSelectionSystem.cs` - citizen presentation bridge; resolves managed prefab choices for citizen visuals.
- `Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs` - citizen presentation bridge; spawns and owns managed citizen prefab instances.
- `Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs` - runtime prefab presentation bridge; resolves managed unit prefab references for ECS spawning.
- `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs` - spawn/presentation boundary; prefab instantiation, grounding warnings, and setup stay managed.
- `Assets/Game/Scripts/Systems/UnitVisualPrefabReferenceBackfillSystem.cs` - GameObject/prefab reference bridge; managed presentation boundary.
- `Assets/Game/Scripts/Systems/VehicleDestroyedVisualSystem.cs` - presentation/prefab instantiate boundary; managed visual lifecycle only.
- `Assets/Game/Scripts/Rendering/Systems/UnitFactionTintTargetBackfillSystem.cs` - render-material presentation bridge; managed tint/material backfill remains outside Burst.
- `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs` - model/prefab spawn presentation bridge; GameObject and prefab work stays managed.
- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs` - render-budget camera/orchestration shell; pure distance, sorting, banding, and plan helpers are Burst-covered separately.

### UI shell managed edge
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs` - UI shell command boundary; consumes low-cardinality UI action requests.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerReadModelSystem.cs` - UI shell read-model boundary; projects ECS build catalog data for managed UI views.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildPlacementReadModelSystem.cs` - UI shell read-model boundary; projects build placement state for managed UI views.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiDiagnosticsReadModelSystem.cs` - UI shell diagnostics boundary; projects runtime diagnostics into UI read models.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellArmoryCategorySystem.cs` - UI shell state boundary; single boundary entity command consumption stays managed.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs` - UI shell transition boundary; route/popup/presentation command buffering stays managed.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` - UI shell bootstrap boundary; creates the shell boundary entity and buffers.

## Validation
- `git diff --check` passed after rebasing onto `origin/main` at `c218b124a`.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 0 errors after the fetched `TacticalFollowAttackCinematicHelper.cs` generated-project include was synced locally.
- Unity no-Burst classification validation passed:
  `Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-reaudit-burst-classification.log -- -quit -executeMethod EcsBurstHotPathArchitectureTests.RunNoBurstISystemClassificationValidation`
- Unity log marker: `[EcsNoBurstISystemClassificationValidation] result=Passed tests=1`.
- Unity inventory marker: `Runtime ISystem Burst classification: noBurst=73 classified=73 unclassified=0`.
- Performance baseline was not rerun for this slice because no runtime gameplay code or Burst attributes changed.

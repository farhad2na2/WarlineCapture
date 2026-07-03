# Burst Coverage Classification

## Scope
- Source tracker: `Design/Architecture/architecture_performance_audit_followup_tracker.md`
- Date: 2026-07-03
- Runtime scan roots: `Assets/Game/Scripts/Systems`, `Assets/Game/Scripts/Rendering/Systems`, `Assets/Game/Scripts/UI/Shell/Ecs`
- Runtime `ISystem` file count: 124
- Runtime files with no `[BurstCompile]` marker: 71

## Classification Rule
This is a conservative first-pass classification for Phase 5. Files are not marked Burst-eligible just because they implement `ISystem`; they must avoid managed Unity APIs, GameObject/prefab/presentation ownership, diagnostic string/log paths, live-helper static entry points, and composition-helper state ownership.

## Summary

| Classification | Count | Notes |
|---|---:|---|
| Burst eligible | 0 | No file in the no-marker set is safe for a blind type-level `[BurstCompile]` addition without either compile probing or helper separation. |
| Managed edge | 24 | UI, rendering, diagnostics, prefab/model, or live helper access. |
| Presentation only | 8 | Visual/model/read-model systems that should stay at the presentation edge unless split. |
| Needs refactor | 39 | Gameplay systems that need helper extraction, ECB/job conversion, or managed diagnostics separation before Burst can be applied safely. |

## Burst Eligible

None in this first pass.

The closest compile-probe candidates are disabled composition stubs such as `BuildingGameplayChildSystem` and `BuildingStartupConfigProjectionSystem`, but their files expose managed helper methods. Those should not receive a type-level Burst attribute until the helper API is separated or the attribute can be limited to empty lifecycle methods without inflating coverage metrics.

## Managed Edge

- `Assets/Game/Scripts/Rendering/Systems/UnitFactionTintTargetBackfillSystem.cs` - rendering backfill edge.
- `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs` - model/prefab spawn edge.
- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetDiagnosticLogFlushSystem.cs` - diagnostic log flush.
- `Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs` - diagnostic log flush.
- `Assets/Game/Scripts/Systems/InitialSpawnDiagnosticLogFlushSystem.cs` - diagnostic log flush.
- `Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs` - diagnostics.
- `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs` - diagnostics.
- `Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs` - diagnostic log flush.
- `Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticLogFlushSystem.cs` - diagnostic log flush.
- `Assets/Game/Scripts/Systems/UnitMoveTargetDiagnosticSystem.cs` - diagnostic state.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs` - UI request edge.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildDrawerReadModelSystem.cs` - UI read model.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildPlacementReadModelSystem.cs` - UI read model.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiDiagnosticsReadModelSystem.cs` - UI diagnostics read model.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellArmoryCategorySystem.cs` - UI state.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs` - UI state.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` - UI state.
- `Assets/Game/Scripts/Systems/BuildingEntityManagerAccessSystem.cs` - EntityManager access helper.
- `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs` - composition/helper edge with UnityEngine grid input.
- `Assets/Game/Scripts/Systems/BuildingPlacementQueryCompositionSystem.cs` - composition/helper edge.
- `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs` - prefab lookup helper with managed names.
- `Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs` - prefab lookup helper.
- `Assets/Game/Scripts/Systems/CitizenPrefabSelectionSystem.cs` - prefab selection helper.
- `Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs` - prefab helper.

## Presentation Only

- `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs` - render budget presentation.
- `Assets/Game/Scripts/Systems/UnitVisualPrefabReferenceBackfillSystem.cs` - visual prefab reference backfill.
- `Assets/Game/Scripts/Systems/VehicleDestroyedVisualSystem.cs` - destroyed visual presentation.
- `Assets/Game/Scripts/Systems/MapSurfaceFlatEquivalentBootstrapSystem.cs` - visual/map bootstrap projection.
- `Assets/Game/Scripts/Systems/BuildingGameplayChildSystem.cs` - disabled composition helper.
- `Assets/Game/Scripts/Systems/BuildingStartupConfigProjectionSystem.cs` - disabled config projection helper.
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs` - runtime state helper API used by camera/UI edges.
- `Assets/Game/Scripts/Systems/RuntimeGridDeduplicationSystem.cs` - runtime grid cleanup path with edge ownership.

## Needs Refactor

- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs` - large gameplay loop with UnityEngine diagnostics and direct EntityManager mutation.
- `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs` - startup/config composition.
- `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs` - startup/config composition.
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs` - startup/config composition.
- `Assets/Game/Scripts/Systems/AttackOrderCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/BuildingTargetMoveOrderSystem.cs` - command/helper path.
- `Assets/Game/Scripts/Systems/CitizenMovementCommandSystem.cs` - command/helper path.
- `Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs` - initialization path with blocker setup.
- `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs` - startup/config composition.
- `Assets/Game/Scripts/Systems/FixedWingRunwayHomeInitializationSystem.cs` - initialization path.
- `Assets/Game/Scripts/Systems/FocusedUnitCommandSystem.cs` - selection command helper path.
- `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs` - startup/spawn composition.
- `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs` - startup/spawn apply path.
- `Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs` - startup/spawn reset path.
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs` - startup/spawn path.
- `Assets/Game/Scripts/Systems/RtsSelectionAttackTargetModeCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionBoardTargetModeCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionCancelActiveCommandModeSystem.cs` - command helper with live static EntityManager entry points.
- `Assets/Game/Scripts/Systems/RtsSelectionDeselectAllCommandSystem.cs` - command helper with live static EntityManager entry points.
- `Assets/Game/Scripts/Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionMissileLauncherRadarAttackCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionModeCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionMoveTargetModeCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionScanTargetModeCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/RtsSelectionSelectAllCommandSystem.cs` - command helper path.
- `Assets/Game/Scripts/Systems/ScanIntelCommandSystem.cs` - command/helper path.
- `Assets/Game/Scripts/Systems/SelectedMoveOrderCommandSystem.cs` - command/helper path.
- `Assets/Game/Scripts/Systems/SelectedUnitDebugFireSystem.cs` - debug command path.
- `Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs` - large managed command system; already tracked for decomposition.
- `Assets/Game/Scripts/Systems/UnitAttackOrderRequestSystem.cs` - command request path.
- `Assets/Game/Scripts/Systems/UnitMoveOrderRequestSystem.cs` - command request path.
- `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs` - disabled helper with managed diagnostics and stack/Debug aliases.
- `Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs` - job scheduler/apply owner with UnityEngine frame/diagnostic use; split diagnostics before type-level Burst.
- `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs` - respawn/spawn path.
- `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs` - command/helper path.
- `Assets/Game/Scripts/Systems/UnitTransportAirPickupSystem.cs` - transport command/simulation path.
- `Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs` - transport helper path.
- `Assets/Game/Scripts/Systems/UnitTransportDeployOrderSystem.cs` - deploy command path that calls `TransportBoardingCommandSystem` helpers.
- `Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs` - transport state path.

## Next Step
The guardrail lives in `Assets/Tests/Editor/EcsBurstHotPathArchitectureTests.cs`: `NoBurstISystemFilesMustBeClassified` fails if a runtime `ISystem` file lacks `[BurstCompile]` and is not listed in the existing managed-boundary/tracked-debt classification dictionaries. Future work should handle Burst in small refactor-backed batches rather than adding attributes blindly. The first practical code batch should split diagnostics from an otherwise Burst-compatible gameplay system or add method-level Burst only where compile validation proves it is truthful.

# ECS Burst Hot-Path Baseline

Date: 2026-06-12

Roadmap:
- `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md`

## Static Audit

Scanned root:
- `Assets/Game/Scripts/Systems`

Commands:
- `rg --files Assets/Game/Scripts/Systems | wc -l`
- `rg -l "\[BurstCompile\]" Assets/Game/Scripts/Systems | wc -l`
- `rg -n "void\s+OnUpdate\s*\(" Assets/Game/Scripts/Systems | wc -l`
- `rg --count-matches "ToEntityArray\(|ToComponentDataArray\(" Assets/Game/Scripts/Systems`
- `rg --count-matches "EntityManager\.(AddComponent|RemoveComponent|DestroyEntity|Instantiate|CreateEntity|SetComponent)" Assets/Game/Scripts/Systems`

Observed:
- System source files: 723.
- Files with `[BurstCompile]`: 13.
- `OnUpdate` source matches: 73.
- Files with `OnUpdate` and no `[BurstCompile]`: 48.
- `ToEntityArray` / `ToComponentDataArray` calls: 109.
- Direct `EntityManager` structural or component mutation calls in system sources: 40.

Top `ToEntityArray` / `ToComponentDataArray` sources:
- `SelectionGameplayStartupSystem.cs`: 9.
- `TransportBoardingCommandSystem.cs`: 6.
- `CustomGameStartupSystem.cs`: 5.
- `RtsSelectionFocusCommandSystem.cs`: 5.
- `AISquadSystem.cs`: 4.
- `AIStartupSystem.cs`: 4.
- `FocusableUnitLookupSystem.cs`: 4.
- `SelectedUnitDebugFireSystem.cs`: 4.
- `UnitVisualPrefabReferenceBackfillSystem.cs`: 4.
- `ThreatDetectionWarningSystem.cs`: 3.

Top direct `EntityManager` mutation sources:
- `CitizenVisibleUnitSystem.cs`: 15.
- `CitizenMovementCommandSystem.cs`: 10.
- `DynamicBlockerInitSystem.cs`: 3.
- `MapSurfaceFlatEquivalentBootstrapSystem.cs`: 3.
- `UnitPathfindingPendingStateSystem.cs`: 3.
- `MapSurfaceDiagnosticsSystem.cs`: 2.

## Runtime Baseline Status

Runtime FPS and system-timing baselines are partially captured.

Captured:
- Match startup after loading gate.
- Graphics-capable shell-to-Match smoke.
- Stable post-intro observation window.
- Focused pathfinding group/long-distance validation.
- Focused transport boarding/disembark validation.
- Focused ground missile projectile dependency validation.
- Performance diagnostics allocation validation.
- Focused selection/command validation.
- Focused render-budget validation.
- Existing architecture contract validation.
- Explicit full-editor validation runner for non-explicit editor tests that do not require Unity TestRunner log scope.

Command:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchRuntimeShellSmokeValidation.RunFrameRateDiagnostics -logFile /private/tmp/warline-ecs-burst-runtime-framerate-baseline.log`

Result:
- Passed.
- Log marker: `[MatchRuntimeShellSmokeValidation] result=Passed No low-FPS FrameRateDiag emitted during stable observation window.`
- Ready status: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1 stable=playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=683`.
- Startup caveat: one pre-ready `[FreezeDetect]` logged `BuildingPlacement=503.2ms`; it occurred before the ready/stable observation window and is not evidence of a post-ready frame regression, but it remains a loading/startup optimization concern.

Required next baseline capture:
- select unit then move
- attack command and missile impact flow
- transport board/board-all/unboard flow
- AI steady-state with current unit/building count

## Focused Validation Additions

Pathfinding:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitPathfindingFocusedPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-pathfinding-baseline.log`
- Result:
  - Passed.
  - Report: `/private/tmp/warlinecapture-unit-pathfinding-focused-performance.json`.
  - Metrics: `updates=9`, `elapsedMs=26.76`, `allocatedBytesCurrentThread=0`, `pathPoolCells=10`, `remainingRequests=0`, `pathDiagnosticsCount=0`.
- Harness note:
  - The validation harness now waits briefly between updates because `UnitPathfindingSystem` intentionally schedules detached jobs that are not chained into `state.Dependency`. This avoids tight-loop starvation in editor batchmode without changing gameplay runtime code.

Transport:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-transport-baseline.log`
- Result:
  - Passed.
  - Log marker: `[UnitTransportValidation] result=Passed tests=16`.

Ground missile:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod GroundMissileLauncherRuntimeTests.RunProjectileDependencyValidation -logFile /private/tmp/warline-ecs-burst-ground-missile-baseline.log`
- Result:
  - Passed.
  - Log marker: `[GroundMissileProjectileDependencyValidation] result=Passed tests=1`.

Performance diagnostics:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-performance-diagnostics-allocation.log`
- Result:
  - Passed.
  - Log marker: `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=1`.

Selection/command:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
- Result:
  - Passed.
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=57`.
  - Covered fixtures: `RtsSelectionInputSystemTests`, `UnitMoveOrderSystemTests`, `SelectionUiQuerySystemTests`, `SelectionOrderMarkerSystemTests`, `MatchHudCommandFeedbackPanelTests`, `MatchHudCommandControlsCurrentPrefabTests`, and `ScanIntelCommandSystemTests`.

Full EditMode attempt:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testResults /private/tmp/warline-ecs-burst-full-editmode.xml -logFile /private/tmp/warline-ecs-burst-full-editmode.log`
- Result:
  - Not accepted as a full-suite pass.
  - Unity exited `0`, but did not write `/private/tmp/warline-ecs-burst-full-editmode.xml` and the log did not contain a TestRunner summary.
  - Keep the roadmap full-suite acceptance open until the TestRunner output path is reliable or a dedicated all-editor validation runner is added.

Explicit full-editor runner:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
- Result:
  - Passed.
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=432 skipped=23`.
  - Skipped fixtures require Unity TestRunner's internal `LogAssert` scope: `AIBuildPlannerValidationTests`, `AICombatOrderValidationTests`, `AIControlModeValidationTests`, `AIEconomyValidationTests`, `AIEndToEndValidationTests`, `AIProductionValidationTests`, `AISquadValidationTests`, `AITargetingValidationTests`, and `MatchHudMinimapProjectionSystemTests`.
  - Validation cleanup found and fixed stale expectations rather than product regressions: production plan entries use normalized spawn keys, the ground missile runtime fixture now matches one-shot missile damage behavior, and runtime camera reference tests now target the ECS-managed camera reference instead of removed static camera state.

Render budget:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitRenderBudgetSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-render-budget-baseline.log`
- Result:
  - Passed.
  - Log marker: `[UnitRenderBudgetFocusedValidation] result=Passed tests=14`.

Architecture contract:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation -logFile /private/tmp/warline-ecs-burst-architecture-contract.log`
- Result:
  - Passed.
  - Log marker: `[ScriptArchitectureBoundaryValidation] result=Passed tests=11`.

Metrics to record:
- frame average, p95, p99, and max after warmup
- GC allocation after warmup
- hot system p95, p99, and max where available
- entity counts for units, buildings, projectiles, markers, and visible models

## Guardrail Added

Added `EcsBurstHotPathArchitectureTests` with current ratchet ceilings:
- Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` calls must not exceed 29.
- Direct `EntityManager` mutation calls must not exceed 40.
- Files with `OnUpdate` and no `[BurstCompile]` must not exceed 38.
- Files with `[BurstCompile]` must not drop below 23.

These are debt ceilings, not success targets. The roadmap should reduce them over time.

Focused validation:
- Command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
- Result:
  - Passed.
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.

Note:
- The Unity TestRunner `-runTests` path exited `0` but did not emit the requested XML file, so this report uses the explicit execute-method validation as the authoritative focused result.

## Phase 2 First Ratchet

`UnitIdleWanderSystem` was converted to `[BurstCompile]` with a Burst `IJobEntity` after the initial baseline was captured.
`UnitAttackStateEnsureSystem` was then converted to `[BurstCompile]` as a simple initialization cleanup system that keeps using the existing command buffer boundary.
`UnitAttackTraceStateEnsureSystem`, `UnitGridSnapSystem`, and `EngageTargetValidateSystem` were then converted as the next low-risk Phase 2 slice. `UnitGridSnapSystem` and `EngageTargetValidateSystem` use Burst `IJobEntity` jobs and retain immediate command buffer playback because downstream gameplay systems depend on same-frame initialization/target cleanup.
`UnitGroundingSystem` was then marked `[BurstCompile]`; it was already a pure scheduled `IJobEntity`, so no gameplay logic changed.
`UnitManualMoveRetrySystem` was then split into a managed frame-count shell plus Burst jobs. This preserved `Time.frameCount` retry semantics while removing the stale-group `ToEntityArray` snapshot and keeping immediate playback before pathfinding.
`UnitSurfaceTrackingSystem.TrackUnitSurfacesJob` was then marked `[BurstCompile]` while keeping the runtime overlay cache/setup shell managed.
`BaseBreachOrderSystem` was then converted to a Burst `IJobEntity` with immediate playback before `EngageTargetValidateSystem`, preserving the existing base-breach state machine.
`PathPoolMaintenanceSystem` was then converted to `[BurstCompile]` and now clears the native path pool through singleton `RefRW<PathPoolComponent>` instead of an EntityManager get/set round trip.
`SelectionGameplayStartupSystem` then started Phase 3 by resolving Match HUD board-button availability through one selected-entity scan instead of separate passenger and transport scans.
`RtsSelectionFocusCommandSystem` then continued Phase 3 by resolving selected Board-mode transport and passenger candidates through one selected-entity scan while preserving transport-first priority.
`RtsSelectionFocusCommandSystem` then merged Attack-mode air-defense-only and normal attack-capability checks into one selected-entity scan while preserving mixed-selection Attack mode.
The unused `SelectionUiReadModelSystem.GetSelectedUnitEntities` API and its `SelectionUiQuerySystem` helper were removed, deleting a dead selected-entity snapshot.
`VisibleUnitSelectionSystem.HasVisiblePlayerUnits` now reuses the same filtered screen-rect scan as `CollectVisiblePlayerUnits` with early exit, deleting a duplicate visible-selection snapshot.
`FocusableUnitLookupSystem` now uses one combined UnitGrid/UnitFootprint changed-version query for coverage refresh instead of separate changed-grid and changed-footprint snapshots.
`SelectedUnitDebugFireSystem` now resolves ground-missile debug enemy-base targets through read-only archetype chunk iteration instead of copying all enemy building entities into a flat `ToEntityArray`.
`ThreatDetectionWarningSystem` now reads the singleton `GridConfig` directly and walks cached type-handle archetype chunks for sensor/target scanning instead of copying grid, sensor, or target entities.
`UnitTargetOrderSystem.TryFindRadarTargetForMissileLauncher` now walks detector and target archetype chunks instead of copying detector and target entity arrays for missile/radar target selection.
`AITargetingSystem` now walks squad, target, and target-priority chunks instead of copying those refresh inputs into flat arrays.
`AISquadSystem` now walks chunks for active-squad counting and initial target-cell resolution instead of taking helper-level entity snapshots.
`AIBuildPlannerSystem` now walks faction-economy chunks instead of taking an economy entity snapshot.
`AICombatOrderSystem` now builds one temporary runtime-building combat record list from archetype chunks instead of four parallel runtime-building entity/component arrays.
`AIProductionSystem` now walks production plans and faction economies through cached type handles and archetype chunks instead of copying plan/economy entity arrays each update.
`ScanIntelCommandSystem` now walks scan targets by archetype chunk into reveal candidates, then applies reveal component mutations after collection.
`FocusedUnitLifecycleSystem` now clears selected tags from a chunk-collected entity list and uses singleton selected-entity lookup for single focus refresh.
`SelectionOrderMarkerSystem` now walks attack/board preview targets by archetype chunk before applying marker presentation.
`BuildingBarrierSystem` now collects compact live-unit door records from archetype chunks instead of copying three parallel component arrays for road-barrier proximity checks.
`SelectionSummaryQuerySystem` now walks selected entities by archetype chunk before resolving category, health, and order summary state.
`SelectedUnitOrderSnapshotSystem` now walks selected entities by archetype chunk before preserving command-mode order components.
`SelectedMoveOrderCommandSystem` now walks selected move entities by archetype chunk before move goal assignment.
`FocusedUnitCommandSystem` now walks selected move entities by archetype chunk before immediate hold/stop command handling.
`BuildingTargetMoveOrderSystem` now walks selected move entities by archetype chunk before building approach-cell move order assignment.
`AttackOrderCommandSystem` now walks selected attack entities by archetype chunk before attack-target order assignment.
`RtsSelectionPointerTargetCommandSystem` now walks runtime building combat chunks before attack-target screen-distance selection.
`UnitPathfindingApplySystem` now walks pending manual-move chunks when building diagnostic manual-move samples.
`RuntimeGridDeduplicationSystem` now walks grid chunks and collects runtime-generated grid entities before duplicate cleanup.
`UnitMoveTargetDiagnosticSystem` now walks player unit target chunks for target-change diagnostics and resolves optional detail only for changed player-controlled targets.
`AIDiagnosticLogFlushSystem`, `UnitPathfindingDiagnosticLogFlushSystem`, and `TransportBoardingDiagnosticLogFlushSystem` now iterate diagnostic dynamic buffers directly through `SystemAPI.Query`.
`InitialSpawnDiagnosticLogFlushSystem` now iterates initial-spawn diagnostic buffers directly through `SystemAPI.Query`.
`InitialSpawnResourceSystem`, `FactionEconomyStartupSystem`, `AIFactionControlStartupSystem`, `InitialFactionSpawnCellSystem`, `InitialUnitsSpawnProgressSystem`, and `InitialSpawnReservationSystem` now use chunk traversal or chunk-collected entity lists for startup entity scans.
`AIBuildPlannerSystem` and `AISquadSystem` now collect plan/unit entities from chunks before preserving their existing plan-processing and squad-creation logic.
`InitialSpawnDuplicateCellDiagnosticSystem`, `InitialSpawnGridContextSystem`, `MapSurfaceFlatEquivalentBootstrapSystem`, `RuntimeGridBootstrapSystem`, `SceneLifecycleSystem`, `RoadBuildEcsBoundarySystem`, `VisibleUnitSelectionSystem`, `MatchHudSquadTraySelectionSystem`, `BuildingPlacementRedirectSystem`, `InitialUnitsBlockerChurnSystem`, and `InitialUnitsSpawnSystem` now use chunk traversal or chunk-collected entity lists for their one-off startup/HUD/boundary scans.
`DynamicOccupancyRebuildSystem`, `GroundMissileLauncherSystems`, `BuildingRuntimeBoundarySystem`, `BuildingSpawnSystem`, `BuildingSpawnPrefabSystem`, and `BuildingResourceHaulerBridgeSystem` now use chunk traversal or chunk-collected entity lists for contained runtime/building scans.
`AIStartupSystem`, `UnitVisualPrefabReferenceBackfillSystem`, `SelectedUnitDebugFireSystem`, `FocusableUnitLookupSystem`, and `RtsSelectionFocusCommandSystem` now use chunk traversal or chunk-collected entity lists for startup plan/reference scans, debug-fire cleanup, focusable lookup refresh, and selected move/attack/board command checks.
`TransportBoardingCommandSystem` now uses chunk traversal or chunk-filled `NativeList` buffers for selected passenger source scans, nearby-transport searches, pending-boarding counts, and paired pathing live-unit arrays.

Validation:
- Full explicit editor validation after the Phase 2 conversions:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=438 skipped=23`.
- Hot-path architecture guardrail:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - Ratcheted guardrails after latest slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling `29`, direct `EntityManager` mutation ceiling `40`, non-Burst `OnUpdate` ceiling `38`, Burst file floor `23`.
- Selected debug-fire focused validation after the chunk-iteration slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectedUnitDebugFireSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selected-debug-fire.log`
  - Log marker: `[SelectedUnitDebugFireFocusedValidation] result=Passed tests=6`.
- Threat-warning focused validation after the singleton grid read and chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ThreatWarningValidationTests.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-threat-warning.log`
  - Log marker: `[ThreatWarningValidation] result=Passed`.
  - Confirmed the ECS type-handle creation warning no longer appears after caching handles and lookups.
- Missile/radar attack focused validation after the chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MissileLauncherRadarAttackValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-missile-radar.log`
  - Log marker: `[MissileLauncherRadarAttackValidation] result=Passed tests=5`.
- Full explicit editor validation after the missile/radar chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static `ToEntityArray` / `ToComponentDataArray` count confirmed at `96`.
  - `git diff --check` passed.
- AI targeting focused validation after the chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AITargetingValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-targeting.log`
  - Log marker: `[AITargetingFocusedValidation] result=Passed tests=2`.
- Full explicit editor validation after the AI targeting chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static `ToEntityArray` / `ToComponentDataArray` count confirmed at `94`.
  - `git diff --check` passed.
- AISquad focused validation after the helper chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISquadValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-squad.log`
  - Log marker: `[AISquadFocusedValidation] result=Passed tests=1`.
- AIBuildPlanner focused validation after the economy chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIBuildPlannerValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-build-planner.log`
  - Log marker: `[AIBuildPlannerFocusedValidation] result=Passed tests=1`.
- AIProduction focused validation after the chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIProductionValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-production.log`
  - Log marker: `[AIProductionFocusedValidation] result=Passed tests=1`.
- Full explicit editor validation after the AISquad/AIBuildPlanner chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static `ToEntityArray` / `ToComponentDataArray` count confirmed at `91`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `114`.
  - `git diff --check` passed.
- AI combat focused validation after the runtime-building record-list slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AICombatOrderValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-combat.log`
  - Log marker: `[AICombatOrderFocusedValidation] result=Passed tests=2`.
- Full explicit editor validation after the AICombat/generic-aware guardrail slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `90`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `110`.
  - `git diff --check` passed.
- Static count after the AIProduction chunk traversal slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `88`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `108`.
- Scan intel focused validation after the reveal-candidate chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ScanIntelCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-scan-intel.log`
  - Log marker: `[ScanIntelCommandFocusedValidation] result=Passed tests=2`.
- Static count after the scan chunk traversal slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `86`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `106`.
- Final validation after the AIProduction and scan chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - `git diff --check` passed.
- Selection state focused validation after the focused-lifecycle slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectionStateSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-state.log`
  - Log marker: `[SelectionStateFocusedValidation] result=Passed tests=5`.
- Static count after the focused-lifecycle slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `84`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `104`.
- Selection order marker focused validation after the preview chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectionOrderMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-order-marker.log`
  - Log marker: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=5`.
- Static count after the selection-marker preview slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `82`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `102`.
- Building barrier focused validation after the road-barrier chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BuildingBarrierSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-building-barrier.log`
  - Log marker: `[BuildingBarrierFocusedValidation] result=Passed tests=2`.
- Static count after the road-barrier chunk traversal slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `82`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `99`.
- Selection summary focused validation after the selected-summary chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectionSummaryQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-summary.log`
  - Log marker: `[SelectionSummaryFocusedValidation] result=Passed tests=10`.
- Selected unit order snapshot focused validation after the order-snapshot chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectedUnitOrderSnapshotSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selected-order-snapshot.log`
  - Log marker: `[SelectedUnitOrderSnapshotFocusedValidation] result=Passed tests=1`.
- Static count after the selected-summary/order-snapshot chunk traversal slices:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `80`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `97`.
- Unit move order focused validation after the selected-move command chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitMoveOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-unit-move-order.log`
  - Log marker: `[UnitMoveOrderFocusedValidation] result=Passed tests=6`.
- Focused unit command validation after the hold/stop command chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod FocusedUnitCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-focused-unit-command.log`
  - Log marker: `[FocusedUnitCommandFocusedValidation] result=Passed tests=2`.
- Static count after the selected move and focused command chunk traversal slices:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `78`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `95`.
- Unit move order focused validation after the building-target move chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitMoveOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-unit-move-order.log`
  - Log marker: `[UnitMoveOrderFocusedValidation] result=Passed tests=7`.
- Static count after the building-target move chunk traversal slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `77`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `94`.
- Unit target order focused validation after the attack-order command chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTargetOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-unit-target-order.log`
  - Log marker: `[UnitTargetOrderFocusedValidation] result=Passed tests=6`.
- Static count after the attack-order command chunk traversal slice:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `76`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `93`.
- Selection command focused validation after the runtime-building attack-target chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=70`.
- Pathfinding focused validation after the manual-move diagnostic sample chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitPathfindingFocusedPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-pathfinding-baseline.log`
  - Log marker: `[UnitPathfindingFocusedPerformanceValidation] result=Passed`.
  - Report metrics: `updates=10 elapsedMs=28.33 allocatedBytes=0 pathPoolCells=10 remainingRequests=0`.
- Static count after the runtime-building attack-target and pathfinding diagnostic slices:
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `74`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `91`.
- Final validation after the focused-lifecycle, selection-marker preview, road-barrier, selection-summary, order-snapshot, selected-move, focused-command, building-target move, attack-order command, runtime-building attack-target, and pathfinding diagnostic slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=454 skipped=23`.
  - `git diff --check` passed.
- Full explicit editor validation after the latest array-copy ratchet:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
- Selection/command focused validation after the third Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=59`.
- Full explicit editor validation after the third Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=440 skipped=23`.
- Shadow validation after the fourth Phase 3 slice:
  - Main project was open in Unity, so the current script and editor-test trees were mirrored into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` before validation.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-shadow-ecs-burst-hot-path-architecture.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-shadow-ecs-burst-selection-command.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=59`.
- Main-project validation after the fifth Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=62`.
  - `BaseBreachValidationTests.UnitPathfinding_RoutesOwnerFactionThroughFriendlyRoadBarrierGate` now uses the same 128-update detached-pathfinding wait budget as the focused pathfinding validation harness.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=440 skipped=23`.
- Main-project validation after the sixth Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=63`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
- Main-project validation after the runtime-grid, move-target diagnostic, diagnostic-flush, initial-spawn diagnostic-flush, startup-boundary, AI plan/squad, one-off startup/HUD/boundary, and contained runtime/building chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod RuntimeGridDeduplicationSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-runtime-grid-dedup.log`
  - Log marker: `[RuntimeGridDeduplicationFocusedValidation] result=Passed tests=2`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=456 skipped=23`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `17`.
  - Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` count confirmed at `29`.
- `git diff --check` passed after the latest slice.

Behavior coverage added in the latest slice:
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_ExpiredCooldownClearsBeforePathRestoresOnNextUpdate`
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_RemovesStaleGroupMemberTag`
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_RestoresManualPathRequest`
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_RestoresLongDistanceFinalGoal`

Behavior coverage added:
- `UnitIdleWanderSystemTests.IdleWander_NonFactionUnitIssuesPathRequest`
- `UnitIdleWanderSystemTests.IdleWander_FactionOwnedUnitStaysIdle`

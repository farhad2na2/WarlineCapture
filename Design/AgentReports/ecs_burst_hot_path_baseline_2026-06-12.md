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
- `ToEntityArray` / `ToComponentDataArray` calls must not exceed 104.
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

Validation:
- Full explicit editor validation:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=438 skipped=23`.
- Hot-path architecture guardrail:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - Ratcheted guardrails after latest slice: `ToEntityArray` / `ToComponentDataArray` ceiling `104`, direct `EntityManager` mutation ceiling `40`, non-Burst `OnUpdate` ceiling `38`, Burst file floor `23`.
- Selection/command focused validation:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=59`.
- Full explicit editor validation after the latest Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=440 skipped=23`.
- `git diff --check` passed after the latest slice.

Behavior coverage added in the latest slice:
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_ExpiredCooldownClearsBeforePathRestoresOnNextUpdate`
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_RemovesStaleGroupMemberTag`
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_RestoresManualPathRequest`
- `UnitManualMoveRetrySystemTests.ManualMoveRetry_RestoresLongDistanceFinalGoal`

Behavior coverage added:
- `UnitIdleWanderSystemTests.IdleWander_NonFactionUnitIssuesPathRequest`
- `UnitIdleWanderSystemTests.IdleWander_FactionOwnedUnitStaysIdle`

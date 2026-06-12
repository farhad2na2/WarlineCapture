# ECS Burst Hot-Path Refactor Roadmap

## Goal

Improve runtime performance by moving frequent gameplay work toward Burst-compatible ECS jobs, reducing main-thread array copies, and batching structural changes without breaking gameplay behavior.

Target shape:
- Pure simulation/data transforms use `[BurstCompile]`, `ISystem`, `IJobEntity`, `IJobChunk`, or Burst jobs.
- Managed code remains only at real boundaries: UI views, GameObject/prefab presentation, bootstrap composition, editor tooling, config assets, and diagnostics flushing.
- Frequent-tick systems do not allocate `ToEntityArray` or `ToComponentDataArray` snapshots every frame unless explicitly justified.
- Frequent structural changes go through `EntityCommandBuffer`.
- Refactor slices are small, validated, and performance-measured.

## Current Audit Snapshot

Audit date: 2026-06-12.

Scanned root:
- `Assets/Game/Scripts/Systems`

Observed:
- 723 system source files.
- 23 system files currently use `[BurstCompile]`.
- 73 `OnUpdate` source matches.
- 38 files have `OnUpdate` and no `[BurstCompile]`.
- 74 non-generic `ToEntityArray` / `ToComponentDataArray` calls.
- 91 generic-aware `ToEntityArray` / `ToComponentDataArray<T>` calls; this is the active guardrail count.
- 40 direct `EntityManager` structural or component mutation calls.
- Main array-copy offenders:
  - `SelectionGameplayStartupSystem`: 10 `To*Array` calls.
  - `TransportBoardingCommandSystem`: 10.
  - `UnitTransportRopeDisembarkSystem`: 8.
  - `CustomGameStartupSystem`: 5.
  - `AIStartupSystem`, `UnitVisualPrefabReferenceBackfillSystem`: 4 each.
  - `RtsSelectionFocusCommandSystem`, `FocusableUnitLookupSystem`, `SelectedUnitDebugFireSystem`, `UnitGridMovementSystem`, `UnitPathLiveUnitSnapshotSystem`: 3 each.
  - `AISquadSystem`, `BuildingResourceHaulerBridgeSystem`, `BuildingSpawnPrefabSystem`, `InitialSpawnReservationSystem`: 2 each.
  - `AIBuildPlannerSystem`: 1.
- Direct structural/component mutation is concentrated mainly in:
  - `CitizenVisibleUnitSystem`: 15.
  - `CitizenMovementCommandSystem`: 10.
  - `DynamicBlockerInitSystem`, `MapSurfaceFlatEquivalentBootstrapSystem`, `UnitPathfindingPendingStateSystem`: 3 each.
  - `MapSurfaceDiagnosticsSystem`: 2.

Superseded rough audit notes:
- The first rough scan saw about 400 system files, 14 Burst files, and 55 non-Burst `OnUpdate` matches before the final static baseline was captured. Use the 2026-06-12 numbers above as the active baseline.

Earlier notable offenders retained for context:
  - `UnitTransportRopeDisembarkSystem`: previously noted as high-risk transport work.
  - `AICombatOrderSystem`: previously noted as high-risk combat work.

Important interpretation:
- Low Burst count alone is not the bug.
- The highest-value fixes are fewer per-frame array copies, fewer sync points, Burst jobs for actual simulation loops, and clearer managed/ECS boundaries.

## Initial Managed Boundary Allowlist

This allowlist is for guardrail classification only. It is not permission to add new hot-path work or grow existing debt.

Intentionally managed diagnostic boundaries:
- `PerformanceDiagnosticsSystem`
- `*DiagnosticLogFlushSystem`
- focused editor validation/execute-method entry points

Intentionally managed startup/bootstrap boundaries:
- `Initial*System` files that run only during initial projection/spawn setup
- `*StartupSystem` files that project authored config into ECS once or on explicit startup
- `RuntimeGridBootstrapSystem`
- `MapSurfaceFlatEquivalentBootstrapSystem`
- `DynamicBlockerInitSystem`

Intentionally managed presentation/config bridges:
- systems that instantiate, bind, or patch GameObject/prefab references
- systems that read ScriptableObject/config references at setup boundaries

Hot managed systems that are not exempt:
- `CitizenVisibleUnitSystem`
- `CitizenMovementCommandSystem`
- selection command/input systems
- transport boarding systems
- AI targeting/combat/squad systems
- render-budget and visual-state decision systems

## Architecture Constraints

- Follow `Design/Architecture/gameplay_solid_ecs_contract.md`.
- Follow `Design/Architecture/performance_regression_contract.md`.
- Do not rewrite gameplay behavior while optimizing.
- Do not add `Object.Find*`, `GameObject.Find`, `Camera.main`, hierarchy path lookup, static mutable registries, service locators, broad manager/controller/facade shells, or ungated hot-path logs.
- Do not force UI, GameObject presentation, prefab spawning, config loading, or bootstrap composition into Burst.
- Preserve `.meta` files during any move or rename.
- Every slice must pass focused tests before continuing.

## Progress Tracker

### Phase 0: Baseline And Safety Rails

Status: [~]

Purpose:
Establish measurable baseline and prevent accidental architecture drift before optimizing.

Implementation steps:
- [x] Create `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md` with this tracker.
- [x] Add a current audit snapshot section with counts for Burst systems, non-Burst `OnUpdate`, `ToEntityArray`, `ToComponentDataArray`, and direct structural changes.
- [ ] Capture baseline performance for:
  - [x] match startup after loading gate
  - [ ] select unit then move
  - [ ] attack command and missile impact flow
  - [ ] transport board/board-all/unboard flow
  - [ ] AI steady-state with current unit/building count
- [ ] Record baseline metrics:
  - frame average, p95, p99, max after warmup
  - GC allocations after warmup
  - hot system p95/p99/max where available
  - entity counts for units, buildings, projectiles, markers, visible models
- [x] Add an allowlist for systems that are intentionally managed.

Acceptance checks:
- [ ] Full EditMode suite passes.
- [x] Explicit full-editor validation runner passes for non-explicit editor tests that do not require Unity TestRunner log scope.
- [x] Baseline report exists under `Design/AgentReports`.
- [ ] The roadmap records runtime baseline numbers and validation command used.

Validation:
- 2026-06-12: Graphics-capable Match runtime smoke passed with Unity 6000.4.0f1:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchRuntimeShellSmokeValidation.RunFrameRateDiagnostics -logFile /private/tmp/warline-ecs-burst-runtime-framerate-baseline.log`
  - Result marker: `[MatchRuntimeShellSmokeValidation] result=Passed No low-FPS FrameRateDiag emitted during stable observation window.`
  - Ready status: `mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1 stable=playRequested=1 spawnConfigs=1/1 progressing=0 sourceKeys=683`.
  - Startup caveat: one pre-ready `[FreezeDetect]` logged `BuildingPlacement=503.2ms`; it occurred before the ready/stable observation window and remains a loading/startup optimization concern.
- 2026-06-12: Focused pathfinding performance validation passed after making the editor validation harness wait briefly for detached path jobs:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitPathfindingFocusedPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-pathfinding-baseline.log`
  - Report: `/private/tmp/warlinecapture-unit-pathfinding-focused-performance.json`
  - Result: `updates=9 elapsedMs=26.76 allocatedBytesCurrentThread=0 pathPoolCells=10 remainingRequests=0 pathDiagnosticsCount=0`.
- 2026-06-12: Focused transport boarding validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-transport-baseline.log`
  - Log marker: `[UnitTransportValidation] result=Passed tests=16`.
- 2026-06-12: Focused ground missile projectile dependency validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod GroundMissileLauncherRuntimeTests.RunProjectileDependencyValidation -logFile /private/tmp/warline-ecs-burst-ground-missile-baseline.log`
  - Log marker: `[GroundMissileProjectileDependencyValidation] result=Passed tests=1`.
- 2026-06-12: Performance diagnostics allocation validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-performance-diagnostics-allocation.log`
  - Log marker: `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=1`.
- 2026-06-12: Focused selection/command validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=57`.
  - Covered fixtures: `RtsSelectionInputSystemTests`, `UnitMoveOrderSystemTests`, `SelectionUiQuerySystemTests`, `SelectionOrderMarkerSystemTests`, `MatchHudCommandFeedbackPanelTests`, `MatchHudCommandControlsCurrentPrefabTests`, and `ScanIntelCommandSystemTests`.
- 2026-06-12: Full EditMode TestRunner command attempted but did not produce `/private/tmp/warline-ecs-burst-full-editmode.xml` or a TestRunner summary, despite exiting `0`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testResults /private/tmp/warline-ecs-burst-full-editmode.xml -logFile /private/tmp/warline-ecs-burst-full-editmode.log`
  - Do not treat this as a full-suite pass; use explicit focused validation runners until the Unity TestRunner output path is reliable.
- 2026-06-12: Explicit full-editor validation runner passed after fixing stale test expectations uncovered by the runner:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=432 skipped=23`.
  - Skipped fixtures require Unity TestRunner's `LogAssert` scope and are not skipped because of known product failures: `AIBuildPlannerValidationTests`, `AICombatOrderValidationTests`, `AIControlModeValidationTests`, `AIEconomyValidationTests`, `AIEndToEndValidationTests`, `AIProductionValidationTests`, `AISquadValidationTests`, `AITargetingValidationTests`, and `MatchHudMinimapProjectionSystemTests`.
  - Test expectation cleanup: production plan entry tests now expect normalized spawn keys, ground missile runtime fixture damage now matches one-shot missile behavior, and runtime camera reference tests now assert the managed ECS camera reference instead of removed static camera state.
- 2026-06-12: Focused render-budget validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitRenderBudgetSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-render-budget-baseline.log`
  - Log marker: `[UnitRenderBudgetFocusedValidation] result=Passed tests=14`.
- 2026-06-12: Existing architecture contract validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation -logFile /private/tmp/warline-ecs-burst-architecture-contract.log`
  - Log marker: `[ScriptArchitectureBoundaryValidation] result=Passed tests=11`.

### Phase 1: Audit Guardrail Tests

Status: [x]

Purpose:
Make future regressions visible while allowing current debt to be reduced gradually.

Implementation steps:
- [x] Add or extend architecture tests to report new frequent-tick `ToEntityArray` / `ToComponentDataArray` calls.
- [x] Add a test/report for direct `EntityManager` structural changes in runtime systems.
- [x] Add a test/report for non-Burst `OnUpdate` systems.
- [x] Add an allowlist for diagnostics flush systems, bootstrap/init systems, UI/presentation boundary systems, and GameObject/prefab bridge systems.
- [x] Make the first version report-only where needed, then ratchet to fail on new unallowlisted debt.

Acceptance checks:
- [x] Tests pass with current allowlist.
- [x] Report clearly identifies newly introduced hot-path debt.
- [x] No false positives from editor/test-only code.

Validation:
- 2026-06-12: Focused guardrail validation passed with Unity 6000.4.0f1:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`

### Phase 2: Low-Risk Burst Conversion Pass

Status: [~]

Purpose:
Convert simple pure ECS systems first to establish patterns without touching complex command behavior.

Candidate systems:
- `UnitIdleWanderSystem` [x]
- `UnitAttackStateEnsureSystem` [x]
- `UnitAttackTraceStateEnsureSystem` [x]
- `UnitGridSnapSystem` [x]
- `EngageTargetValidateSystem` [x]
- `UnitGroundingSystem` [x]
- `UnitManualMoveRetrySystem` [x]
- `UnitSurfaceTrackingSystem` [x]
- `BaseBreachOrderSystem` [x]
- `PathPoolMaintenanceSystem` [x]
- simple cleanup/state sync systems that do not touch managed objects

Implementation steps:
- [x] For `UnitIdleWanderSystem`, confirm it does not touch `UnityEngine.Object`, GameObject APIs, strings/logging, managed collections, or ScriptableObjects in `OnUpdate`.
- [x] Add `[BurstCompile]` to `UnitIdleWanderSystem` and update lifecycle methods.
- [x] Move `UnitIdleWanderSystem` loop body into a Burst `IJobEntity`.
- [x] Keep immediate ECB playback before pathfinding for `UnitIdleWanderSystem`.
- [~] For remaining candidates, confirm they do not touch `UnityEngine.Object`, GameObject APIs, strings/logging, managed collections, or ScriptableObjects in `OnUpdate`.
- [~] Add `[BurstCompile]` to remaining systems and update lifecycle methods where valid.
- [~] Move remaining loop bodies into `IJobEntity` when structural changes are not needed.
- [ ] Use `EndSimulationEntityCommandBufferSystem.Singleton` for add/remove component work when same-frame playback is not required.
- [x] Keep one behavior-preserving slice per system or small related group for completed Phase 2 candidates.

Acceptance checks:
- [x] Explicit full-editor validation runner passes after each completed Phase 2 group.
- [~] No new managed allocations in converted systems.
- [x] No behavior changes in movement, target validation, or order cleanup tests covered by current focused validation.

Progress notes:
- 2026-06-12: Converted `UnitIdleWanderSystem` to `[BurstCompile]` with a Burst `IJobEntity`. Behavior preserved:
  - faction-owned RTS units remain excluded from idle wander
  - non-faction idle wanderers still emit `UnitTarget`, `UnitPathRequest`, and `AutoWanderMoveTag` through an ECB before `UnitPathfindingSystem`
  - immediate ECB playback is retained because pathfinding must see the request in the same frame.
- 2026-06-12: Added `UnitIdleWanderSystemTests`.
  - `IdleWander_NonFactionUnitIssuesPathRequest`
  - `IdleWander_FactionOwnedUnitStaysIdle`
- 2026-06-12: Validation passed:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=434 skipped=23`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
- 2026-06-12: Converted `UnitAttackStateEnsureSystem` to `[BurstCompile]`. It remains a simple initialization-group ECS cleanup that adds `UnitAttackCooldownComponent` through the existing command buffer boundary.
- 2026-06-12: Validation passed after the second conversion:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=434 skipped=23`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - Guardrail ratchet after this slice: non-Burst `OnUpdate` ceiling reduced to `46`, Burst file floor increased to `15`.
- 2026-06-12: Converted three additional low-risk ECS systems:
  - `UnitAttackTraceStateEnsureSystem` now uses `[BurstCompile]` while preserving the existing command buffer boundary for missing attack trace state.
  - `UnitGridSnapSystem` now schedules a Burst `IJobEntity` that writes initial snapped transforms and immediately plays back `UnitGridInitialized` because downstream systems rely on same-frame initialization.
  - `EngageTargetValidateSystem` now schedules a Burst `IJobEntity` that clears invalid engage targets, marks air units returning home, and restores path requests before `UnitEngagementSystem` can re-acquire.
- 2026-06-12: Focused hot-path architecture validation passed after the three-system slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - Guardrail ratchet after this slice: non-Burst `OnUpdate` ceiling reduced to `43`, Burst file floor increased to `18`.
- 2026-06-12: Explicit full-editor validation passed after the three-system slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=434 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Converted `UnitGroundingSystem` to `[BurstCompile]`. It was already a pure scheduled `IJobEntity`; this slice only Burst-compiles the existing grounding job and lifecycle methods.
- 2026-06-12: Validation passed after `UnitGroundingSystem`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=434 skipped=23`.
  - `git diff --check` passed.
  - Guardrail ratchet after `UnitGroundingSystem`: non-Burst `OnUpdate` ceiling reduced to `42`, Burst file floor increased to `19`.
- 2026-06-12: Refactored `UnitManualMoveRetrySystem` into a small managed frame-count shell plus Burst `IJobEntity` jobs. This keeps retry cooldown semantics tied to `Time.frameCount`, removes the stale-group `ToEntityArray` snapshot, and preserves immediate command-buffer playback before `UnitPathfindingSystem`.
- 2026-06-12: Added `UnitManualMoveRetrySystemTests` covering expired cooldown clearing, stale group cleanup, manual path restoration, and long-distance final-goal restoration.
- 2026-06-12: Validation passed after `UnitManualMoveRetrySystem`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=438 skipped=23`.
  - `git diff --check` passed.
  - Guardrail ratchet after `UnitManualMoveRetrySystem`: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `108`, non-Burst `OnUpdate` ceiling reduced to `41`, Burst file floor increased to `20`.
- 2026-06-12: Added Burst coverage to `UnitSurfaceTrackingSystem.TrackUnitSurfacesJob`. The system still keeps its managed cache/setup shell for runtime surface overlays, but the per-unit surface sampling loop is now Burst-compiled.
- 2026-06-12: Validation passed after `UnitSurfaceTrackingSystem`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=438 skipped=23`.
  - `git diff --check` passed.
  - Guardrail ratchet after `UnitSurfaceTrackingSystem`: non-Burst `OnUpdate` ceiling reduced to `40`, Burst file floor increased to `21`.
- 2026-06-12: Converted `BaseBreachOrderSystem` to a Burst `IJobEntity` with same-frame command-buffer playback. The job preserves existing breach approach, breach attack, final-target transition, and path-state cleanup behavior without introducing managed runtime dependencies.
- 2026-06-12: Guardrail ratchet after `BaseBreachOrderSystem`: non-Burst `OnUpdate` ceiling reduced to `39`, Burst file floor increased to `22`.
- 2026-06-12: Converted `PathPoolMaintenanceSystem` to `[BurstCompile]` and replaced the EntityManager get/set path with singleton `RefRW<PathPoolComponent>` mutation. It still only clears the shared native path pool when no `UnitPathRange` components are active.
- 2026-06-12: Guardrail ratchet after `PathPoolMaintenanceSystem`: direct `EntityManager` mutation ceiling reduced to `40`, non-Burst `OnUpdate` ceiling reduced to `38`, Burst file floor increased to `23`.
- 2026-06-12: Validation passed after `BaseBreachOrderSystem` and `PathPoolMaintenanceSystem`:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=438 skipped=23`.
- 2026-06-12: Started Phase 3 with a narrow `SelectionGameplayStartupSystem` cleanup. The Match HUD squad panel now resolves board-button availability through one selected-entity scan instead of separate passenger and transport scans.
- 2026-06-12: Guardrail ratchet after the first Phase 3 slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `107`.
- 2026-06-12: Validation passed after the first Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=57`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=438 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 with a narrow `RtsSelectionFocusCommandSystem` cleanup. Entering Board mode now resolves selected transport and passenger candidates through one selected-entity scan while preserving transport-first priority.
- 2026-06-12: Added `RtsSelectionInputSystemTests.BoardTargetMode_SelectedTransportAndPassengerUsesTransportFirstMode`.
- 2026-06-12: Guardrail ratchet after the second Phase 3 slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `106`.
- 2026-06-12: Validation passed after the second Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=58`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=439 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 with another narrow `RtsSelectionFocusCommandSystem` cleanup. Entering Attack mode now resolves selected air-defense and normal attack-capable units through one selected-entity scan while preserving air-defense-only auto-engage feedback.
- 2026-06-12: Added `RtsSelectionInputSystemTests.AttackTargetMode_MixedAirDefenseAndAttackUnitEntersTargetMode`.
- 2026-06-12: Guardrail ratchet after the third Phase 3 slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `105`.
- 2026-06-12: Validation passed after the third Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=59`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=440 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Removed unused `SelectionUiReadModelSystem.GetSelectedUnitEntities` and its `SelectionUiQuerySystem` helper. This deletes a dead selected-entity snapshot API rather than hiding it behind a helper.
- 2026-06-12: Guardrail ratchet after the fourth Phase 3 slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `104`.
- 2026-06-12: Validation passed after the fourth Phase 3 slice. The main project was open in Unity, so validation ran in the synced `WarlineCapture-CodexUnity1` shadow project after mirroring the current script and editor-test trees:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-shadow-ecs-burst-hot-path-architecture.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-shadow-ecs-burst-selection-command.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=59`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 with a narrow `VisibleUnitSelectionSystem` cleanup. The "has visible player units" path now reuses the same filtered screen-rect scan as the "collect visible player units" path with early exit, removing one duplicate visible-selection snapshot while preserving filters.
- 2026-06-12: Added visible-selection test coverage for the shared `HasVisiblePlayerUnits` path and vehicle-filter negative case.
- 2026-06-12: Guardrail ratchet after the fifth Phase 3 slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `103`.
- 2026-06-12: Validation passed after the fifth Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=62`.
  - `git diff --check` passed.
- 2026-06-12: Fixed `BaseBreachValidationTests.UnitPathfinding_RoutesOwnerFactionThroughFriendlyRoadBarrierGate` to use the same 128-update detached-pathfinding wait budget as the focused pathfinding validation harness. This preserves the assertion while avoiding editor batchmode starvation of path jobs.
- 2026-06-12: Full explicit editor validation passed after the fifth Phase 3 slice and BaseBreach harness fix:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=440 skipped=23`.
- 2026-06-12: Continued Phase 3 with a narrow `FocusableUnitLookupSystem` cleanup. Unit grid and footprint changed-version lookup refresh now uses one combined coverage-change query instead of separate grid and footprint snapshots.
- 2026-06-12: Added `FocusableUnitLookupSystemTests.LookupRefreshesWhenGridOrFootprintChangesWithoutCountChange` to cover same-count lookup refresh for moved units and footprint expansion.
- 2026-06-12: Guardrail ratchet after the sixth Phase 3 slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `102`.
- 2026-06-12: Validation passed after the sixth Phase 3 slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=63`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Inspected `TransportBoardingCommandSystem` for the next Phase 4 slice. The selected-source fallback and paired pathing live-unit snapshots were left unchanged because they either preserve building/odd-selection fallback behavior or feed approach-cell pathing with synchronized entity/grid/footprint arrays. Those should move as one board-all/pathing job-buffer rewrite, not as a local snapshot deletion.
- 2026-06-12: Completed an adjacent combat/debug hot-path cleanup in `SelectedUnitDebugFireSystem`. Ground-missile debug enemy-base targeting now walks read-only archetype chunks instead of copying every enemy building entity into a flat `ToEntityArray`, preserving scoring and target selection while reducing the global array-copy debt by one.
- 2026-06-12: Added `SelectedUnitDebugFireSystemTests.RunFocusedValidation` so the debug-fire path can be validated directly by execute-method.
- 2026-06-12: Guardrail ratchet after the debug-fire chunk-iteration slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `101`.
- 2026-06-12: Validation passed after the debug-fire chunk-iteration slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectedUnitDebugFireSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selected-debug-fire.log`
  - Log marker: `[SelectedUnitDebugFireFocusedValidation] result=Passed tests=6`.
  - `git diff --check` passed.
- 2026-06-12: Continued the adjacent combat/detection cleanup in `ThreatDetectionWarningSystem`. The grid cell-size read now uses the existing singleton `GridConfig` query instead of copying grid entities to read the first result.
- 2026-06-12: Guardrail ratchet after the threat-warning singleton slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `100`.
- 2026-06-12: Validation passed after the threat-warning singleton slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ThreatWarningValidationTests.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-threat-warning.log`
  - Log marker: `[ThreatWarningValidation] result=Passed`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued `ThreatDetectionWarningSystem` by replacing the remaining sensor and target `ToEntityArray` snapshots with cached type handles, component lookups, and read-only archetype chunk traversal. This removes all `To*Array` calls from the system while preserving the existing nested sensor/target behavior.
- 2026-06-12: Fixed the initial chunk-iteration version to cache type handles and component lookups from `OnCreate` and update them in `OnUpdate`, removing Unity's ECS type-handle creation warning.
- 2026-06-12: Guardrail ratchet after the threat-warning chunk traversal slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `98`.
- 2026-06-12: Validation passed after the threat-warning chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ThreatWarningValidationTests.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-threat-warning.log`
  - Log marker: `[ThreatWarningValidation] result=Passed`.
  - Confirmed the prior ECS type-handle warning no longer appears in `/private/tmp/warline-ecs-burst-threat-warning.log`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued the adjacent combat targeting cleanup in `UnitTargetOrderSystem`. Missile-launcher radar target selection now walks detector and target archetype chunks instead of copying detector and target entity arrays before scoring candidates.
- 2026-06-12: Added `MissileLauncherRadarAttackValidationTests.RunFocusedValidation` so missile/radar attack selection can be validated directly by execute-method.
- 2026-06-12: Guardrail ratchet after the missile/radar chunk traversal slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `96`.
- 2026-06-12: Focused validation passed after the missile/radar chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MissileLauncherRadarAttackValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-missile-radar.log`
  - Log marker: `[MissileLauncherRadarAttackValidation] result=Passed tests=5`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static `ToEntityArray` / `ToComponentDataArray` count confirmed at `96`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 5 in `AITargetingSystem`. AI squad, target, and target-priority refresh now use cached type handles and archetype chunk traversal instead of per-refresh squad/target/priority array snapshots. The system remains managed because it still owns diagnostic queuing and component-presence scoring.
- 2026-06-12: Added `AITargetingValidationTests.RunFocusedValidation` to exercise target scoring and economy-priority behavior without relying on Unity TestRunner's `LogAssert` scope.
- 2026-06-12: Guardrail ratchet after the AI targeting chunk traversal slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `94`.
- 2026-06-12: Validation passed after the AI targeting chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AITargetingValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-targeting.log`
  - Log marker: `[AITargetingFocusedValidation] result=Passed tests=2`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static `ToEntityArray` / `ToComponentDataArray` count confirmed at `94`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 5 in `AISquadSystem`. Active-squad counting and initial target-cell resolution now use cached type handles and archetype chunk traversal instead of helper-level entity snapshots. Direct squad creation and member tagging remain unchanged because they are command/structural behavior and need a dedicated ECB slice.
- 2026-06-12: Continued Phase 5 in `AIBuildPlannerSystem`. The faction economy lookup now uses cached type handles and archetype chunk traversal instead of an economy entity snapshot. Runtime building request behavior remains unchanged.
- 2026-06-12: Added `AISquadValidationTests.RunFocusedValidation` and `AIBuildPlannerValidationTests.RunFocusedValidation` so these AI fixtures can be validated directly without Unity TestRunner `LogAssert` scope.
- 2026-06-12: Inspected the same economy lookup pattern in `AIProductionSystem`, but initially left it unchanged after the direct execute-method runner failed to reproduce queued production state. A later slice added a deterministic ECS-boundary focused runner before changing production AI.
- 2026-06-12: Guardrail ratchet after the AISquad/AIBuildPlanner chunk traversal slice: `ToEntityArray` / `ToComponentDataArray` ceiling reduced to `91`.
- 2026-06-12: Validation passed after the AISquad/AIBuildPlanner chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISquadValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-squad.log`
  - Log marker: `[AISquadFocusedValidation] result=Passed tests=1`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIBuildPlannerValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-build-planner.log`
  - Log marker: `[AIBuildPlannerFocusedValidation] result=Passed tests=1`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static `ToEntityArray` / `ToComponentDataArray` count confirmed at `91`; generic-aware count confirmed at `114`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 5 in `AICombatOrderSystem`. Runtime building combat data now builds one temporary native record list from archetype chunks instead of four parallel `ToEntityArray` / `ToComponentDataArray<T>` snapshots. This keeps the cross-query breach-target matching data local to the AI combat tick while removing repeated query-array copies.
- 2026-06-12: Added `AICombatOrderValidationTests.RunFocusedValidation` so commanded combat orders and manual-player-faction exclusion can be validated directly without Unity TestRunner `LogAssert` scope.
- 2026-06-12: Tightened `EcsBurstHotPathArchitectureTests` so the array snapshot guardrail counts generic `ToComponentDataArray<T>` calls too. The active generic-aware ceiling is now `110`; the simpler non-generic count is `90`.
- 2026-06-12: Validation passed after the AICombat/generic-aware guardrail slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AICombatOrderValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-combat.log`
  - Log marker: `[AICombatOrderFocusedValidation] result=Passed tests=2`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `90`; generic-aware count confirmed at `110`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 5 in `AIProductionSystem`. Production plan iteration and faction economy lookup now use cached type handles, archetype chunk traversal, and temporary economy records instead of `ToEntityArray` snapshots. Existing building-runtime producer behavior remains covered by the original TestRunner fixture; the new execute-method focused runner validates the AI production ECS boundary by queuing a request, consuming an accepted request, spending faction money, and advancing the plan index.
- 2026-06-12: Added `AIProductionValidationTests.RunFocusedValidation` with a deterministic ECS-boundary fixture because Unity's filtered TestRunner still exits without writing a result XML in this project.
- 2026-06-12: Guardrail ratchet after the AIProduction chunk traversal slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `108`.
- 2026-06-12: Focused validation passed after the AIProduction chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIProductionValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-ai-production.log`
  - Log marker: `[AIProductionFocusedValidation] result=Passed tests=1`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `88`; generic-aware count confirmed at `108`.
- 2026-06-12: Continued with `ScanIntelCommandSystem`. Scan reveal target collection now walks unit and building archetype chunks into a temporary reveal-candidate list, then applies reveal components afterward so structural changes do not happen during chunk iteration.
- 2026-06-12: Added `ScanIntelCommandSystemTests.RunFocusedValidation` for direct scan validation.
- 2026-06-12: Guardrail ratchet after the scan chunk traversal slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `106`.
- 2026-06-12: Focused validation passed after the scan chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ScanIntelCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-scan-intel.log`
  - Log marker: `[ScanIntelCommandFocusedValidation] result=Passed tests=2`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `86`; generic-aware count confirmed at `106`.
- 2026-06-12: Validation passed after the AIProduction and scan chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=441 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 in `FocusedUnitLifecycleSystem`. Clear-selection now collects selected entities through archetype chunks before removing tags, and single-selected focus refresh uses the selected query singleton path instead of copying selected entities.
- 2026-06-12: Added focused lifecycle coverage to `SelectionStateSystemTests.RunFocusedValidation`.
- 2026-06-12: Guardrail ratchet after the focused-lifecycle slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `104`.
- 2026-06-12: Focused validation passed after the focused-lifecycle slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectionStateSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-state.log`
  - Log marker: `[SelectionStateFocusedValidation] result=Passed tests=5`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `84`; generic-aware count confirmed at `104`.
- 2026-06-12: Continued Phase 3 in `SelectionOrderMarkerSystem`. Attack and board target preview markers now walk the preview target query by archetype chunks, reading `Faction`, `LocalTransform`, and optional `UnitHealth` from chunk arrays before applying marker presentation.
- 2026-06-12: Added focused marker preview coverage to `SelectionOrderMarkerSystemTests.RunFocusedValidation`.
- 2026-06-12: Guardrail ratchet after the selection-marker preview slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `102`.
- 2026-06-12: Focused validation passed after the selection-marker preview slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectionOrderMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-order-marker.log`
  - Log marker: `[SelectionOrderMarkerFocusedValidation] result=Passed tests=5`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `82`; generic-aware count confirmed at `102`.
- 2026-06-12: Validation passed after the focused-lifecycle and selection-marker preview slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=445 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 in `BuildingBarrierSystem`. Road-barrier door proximity checks now collect compact live-unit records from archetype chunks instead of copying `Faction`, `UnitGrid`, and `UnitFootprint` into three parallel component arrays each tick.
- 2026-06-12: Added `BuildingBarrierSystemTests.RunFocusedValidation` for direct road-barrier open/closed door behavior.
- 2026-06-12: Guardrail ratchet after the road-barrier chunk traversal slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `99`.
- 2026-06-12: Focused validation passed after the road-barrier chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BuildingBarrierSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-building-barrier.log`
  - Log marker: `[BuildingBarrierFocusedValidation] result=Passed tests=2`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `82`; generic-aware count confirmed at `99`.
- 2026-06-12: Validation passed after the road-barrier chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=447 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 in `SelectionSummaryQuerySystem`. Selected-summary aggregation now walks selected entities by archetype chunks instead of copying the selected entity array, preserving per-entity category, health, and order read-model resolution.
- 2026-06-12: Added `SelectionSummaryQuerySystemTests.RunFocusedValidation` for direct summary aggregation validation.
- 2026-06-12: Continued Phase 3 in `SelectedUnitOrderSnapshotSystem`. Command-mode order preservation now walks selected entities by archetype chunks before storing existing order components.
- 2026-06-12: Added `SelectedUnitOrderSnapshotSystemTests.RunFocusedValidation` for direct preserve/restore behavior.
- 2026-06-12: Guardrail ratchet after the selection-summary/order-snapshot chunk traversal slices: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `97`.
- 2026-06-12: Focused validation passed after the selection-summary/order-snapshot chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectionSummaryQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-summary.log`
  - Log marker: `[SelectionSummaryFocusedValidation] result=Passed tests=10`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod SelectedUnitOrderSnapshotSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selected-order-snapshot.log`
  - Log marker: `[SelectedUnitOrderSnapshotFocusedValidation] result=Passed tests=1`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `80`; generic-aware count confirmed at `97`.
- 2026-06-12: Validation passed after the selection-summary/order-snapshot chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=448 skipped=23`.
  - `git diff --check` passed.
- 2026-06-12: Continued Phase 3 in `SelectedMoveOrderCommandSystem`. Move command selected-unit collection now walks the selected move query by archetype chunks before formation/goal assignment.
- 2026-06-12: Added a selected move command case to `UnitMoveOrderSystemTests.RunFocusedValidation`.
- 2026-06-12: Continued Phase 3 in `FocusedUnitCommandSystem`. Immediate hold/stop command selected-unit collection now walks selected move entities by archetype chunks before clearing or preserving order state.
- 2026-06-12: Added `FocusedUnitCommandSystemTests.RunFocusedValidation` for direct hold/stop command behavior.
- 2026-06-12: Guardrail ratchet after the selected move and focused command chunk traversal slices: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `95`.
- 2026-06-12: Focused validation passed after the selected move and focused command chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitMoveOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-unit-move-order.log`
  - Log marker: `[UnitMoveOrderFocusedValidation] result=Passed tests=6`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod FocusedUnitCommandSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-focused-unit-command.log`
  - Log marker: `[FocusedUnitCommandFocusedValidation] result=Passed tests=2`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `78`; generic-aware count confirmed at `95`.
- 2026-06-12: Continued Phase 3 in `BuildingTargetMoveOrderSystem`. Building-target approach move ordering now walks selected move entities by archetype chunks before issuing approach-cell move orders.
- 2026-06-12: Added a building-target approach move case to `UnitMoveOrderSystemTests.RunFocusedValidation`.
- 2026-06-12: Guardrail ratchet after the building-target move chunk traversal slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `94`.
- 2026-06-12: Focused validation passed after the building-target move chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitMoveOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-unit-move-order.log`
  - Log marker: `[UnitMoveOrderFocusedValidation] result=Passed tests=7`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `77`; generic-aware count confirmed at `94`.
- 2026-06-12: Continued Phase 3 in `AttackOrderCommandSystem`. Selected attack source fallback now walks the selected attack query by archetype chunks before issuing the attack target order.
- 2026-06-12: Added a direct attack-order command fallback case to `UnitTargetOrderSystemTests.RunFocusedValidation`.
- 2026-06-12: Guardrail ratchet after the attack-order command chunk traversal slice: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `93`.
- 2026-06-12: Focused validation passed after the attack-order command chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTargetOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-unit-target-order.log`
  - Log marker: `[UnitTargetOrderFocusedValidation] result=Passed tests=6`.
  - Static non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `76`; generic-aware count confirmed at `93`.
- 2026-06-12: Continued Phase 3 in `RtsSelectionPointerTargetCommandSystem`. Runtime-building attack-target lookup now walks runtime building combat chunks instead of copying runtime building entities, preserving nearest-screen candidate selection.
- 2026-06-12: Added `RtsSelectionInputSystemTests.AttackTargetLookup_ReturnsRuntimeBuildingFromScreenClick` with a temporary orthographic camera and runtime building footprint.
- 2026-06-12: Focused selection command validation passed after the runtime-building attack-target chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstSelectionCommandValidationRunner.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-selection-command-baseline.log`
  - Log marker: `[EcsBurstSelectionCommandValidation] result=Passed tests=70`.
- 2026-06-12: Continued Phase 7 in `UnitPathfindingApplySystem`. Manual-move diagnostic sample collection now walks the pending manual-move query by chunks instead of copying pending entities.
- 2026-06-12: Focused pathfinding validation passed after the diagnostic sample chunk traversal slice:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitPathfindingFocusedPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-pathfinding-baseline.log`
  - Log marker: `[UnitPathfindingFocusedPerformanceValidation] result=Passed`.
  - Report metrics: `updates=10 elapsedMs=28.33 allocatedBytes=0 pathPoolCells=10 remainingRequests=0`.
- 2026-06-12: Guardrail ratchet after the runtime-building attack-target and pathfinding diagnostic chunk traversal slices: generic-aware `ToEntityArray` / `ToComponentDataArray<T>` ceiling reduced to `91`.
- 2026-06-12: Static count after the runtime-building attack-target and pathfinding diagnostic slices: non-generic `ToEntityArray` / `ToComponentDataArray` count confirmed at `74`; generic-aware count confirmed at `91`.
- 2026-06-12: Validation passed after the selected move, focused command, building-target move, attack-order command, runtime-building attack-target, and pathfinding diagnostic chunk traversal slices:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-hot-path-architecture-execute.log`
  - Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=4`.
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-full-editor-runner.log`
  - Log marker: `[EcsBurstFullEditorValidation] result=Passed tests=454 skipped=23`.
  - `git diff --check` passed.

### Phase 3: Replace Hot Array Copies In Selection

Status: [~]

Purpose:
Reduce the largest known source of main-thread query snapshots while keeping active Match HUD command behavior stable.

Target areas:
- `SelectionGameplayStartupSystem`
- `RtsSelectionFocusCommandSystem`
- `FocusableUnitLookupSystem`
- `SelectionSummaryQuerySystem`
- `SelectionUiReadModelSystem`

Implementation steps:
- [ ] Split any remaining mixed selection logic into managed shell/input bridge, ECS request writers, Burst-compatible candidate/filter/scoring jobs, and managed UI presentation/read-model apply boundary.
- [ ] Replace visible-unit and closest-unit candidate collection with Burst-compatible jobs.
- [~] Replace selected-entity snapshots with query iteration or a reused native result buffer.
- [ ] Keep UI feedback and marker presentation outside Burst.
- [ ] Keep camera actions flowing through existing camera request boundaries.

Acceptance checks:
- [ ] Select single unit.
- [ ] Select multiple soldiers through squad tray.
- [ ] Select vehicles/helicopter/jet/transport through squad tray.
- [ ] Selection panel updates.
- [ ] Command buttons still arm/deselect correctly.
- [ ] No command button click falls through to world selection.
- [ ] Array-copy count is reduced for selection files.

### Phase 4: Replace Hot Array Copies In Transport And Boarding

Status: [~]

Purpose:
Optimize boarding candidate search, board-all, passenger state, and rope disembark without changing UX.

Target areas:
- `TransportBoardingCommandSystem`
- `UnitTransportRopeDisembarkSystem`
- `SelectionTransportCommandRequestSystem`
- transport passenger drawer read model

Implementation steps:
- [ ] Move nearest eligible passenger search into a Burst-compatible candidate job.
- [ ] Use native result buffers for board-all candidates up to remaining transport capacity.
- [ ] Replace repeated entity/component array snapshots in rope disembark with chunk/job processing.
- [ ] Batch passenger add/remove/hidden visual ECS state through ECB.
- [ ] Keep GameObject visual hiding/restoring in the managed presentation utility boundary only.

Acceptance checks:
- [ ] Soldier-first board flow works.
- [ ] Transport-first board flow works.
- [ ] Board All fills only available seats.
- [ ] Cancel exits board mode and clears feedback actions.
- [ ] Passenger drawer shows correct count, portraits, health, names, and exit buttons.
- [ ] Unboard/exit-all behavior remains correct.
- [ ] Array-copy count is reduced for transport files.

### Phase 5: AI Targeting, Squad, And Combat Optimization

Status: [~]

Purpose:
Reduce main-thread copies in AI steady state, especially targeting and combat orders.

Target areas:
- `AITargetingSystem`
- `AICombatOrderSystem`
- `AISquadSystem`
- `AIProductionSystem`
- `AIBuildPlannerSystem`
- adjacent combat/debug helpers such as `SelectedUnitDebugFireSystem`
- adjacent detection/warning helpers such as `ThreatDetectionWarningSystem`

Implementation steps:
- [ ] Convert target scanning to chunk/job-based collection.
- [ ] Build temporary native target/squad data only once per AI update tick when cross-query matching is required.
- [ ] Reuse persistent native containers owned by the system where safe.
- [ ] Batch combat/path/engage mutations through ECB.
- [ ] Keep authored AI config and policy unchanged.
- [ ] Keep diagnostics gated and outside hot loops.

Acceptance checks:
- [ ] AI squads form.
- [ ] AI targets valid enemies/buildings.
- [ ] AI combat orders still issue.
- [ ] No AI behavior regressions in existing EditMode tests.
- [ ] AI steady-state frame/system timing improves or remains equal.

### Phase 6: Structural Change Cleanup

Status: [ ]

Purpose:
Remove direct structural changes from frequent runtime loops while leaving true startup/init code alone.

Target areas:
- `CitizenVisibleUnitSystem`
- `CitizenMovementCommandSystem`
- runtime command systems that add/remove path/order components in loops

Implementation steps:
- [ ] Classify structural changes as startup-only, command-event, or frequent-tick.
- [ ] Leave startup-only structural changes unless diagnostics show they affect runtime frame rate.
- [ ] Convert frequent-tick structural changes to ECB.
- [ ] Convert repeated command-event changes to ECB where behavior ordering remains clear.
- [ ] Add tests where ECB playback timing could change visible behavior.

Acceptance checks:
- [ ] Citizens still spawn/project correctly.
- [ ] Citizen movement commands still work.
- [ ] Unit path/manual move components are added/removed in the expected frame.
- [ ] No new sync-point regressions appear in diagnostics.

### Phase 7: Pathfinding And Occupancy Protection Pass

Status: [ ]

Purpose:
Protect already optimized pathfinding semantics while removing surrounding hot-path allocations only where safe.

Target areas:
- `UnitPathfindingSystem`
- `UnitPathfindingScheduleSystem`
- `UnitPathfindingApplySystem`
- `UnitPathLiveUnitSnapshotSystem`
- `DynamicOccupancyRebuildSystem`
- `BuildingBarrierSystem`

Implementation steps:
- [ ] Do not change pathfinding algorithm, traversal costs, request budgets, or detached-job scheduling without a dedicated performance report.
- [ ] Audit surrounding snapshots and live-unit collection for avoidable allocations.
- [ ] Keep pathfinding native snapshots system-owned and persistent where already designed that way.
- [ ] Add any new path diagnostics through ECS diagnostic buffers, disabled by default.
- [ ] Use focused pathfinding validation after every path-related slice.

Acceptance checks:
- [ ] Manual group move completes.
- [ ] Long-distance move segmentation works.
- [ ] Mixed infantry/vehicle pathing works.
- [ ] Friendly-pass gates remain pathable.
- [ ] No recurring allocations after warmup.
- [ ] Pathfinding p95/p99 does not regress.

### Phase 8: Render-Budget And Visual-State Hot Paths

Status: [ ]

Purpose:
Improve visual-state update costs without moving GameObject presentation into Burst incorrectly.

Target areas:
- `UnitRenderBudgetSystem`
- `UnitMassRenderSettingsSystem`
- `UnitModelSpawnSystem`
- `UnitSelectionMarkerSystem`
- helicopter/missile visual systems where pure ECS state can be separated from GameObject presentation

Implementation steps:
- [ ] Keep model/prefab/GameObject mutation managed.
- [ ] Move pure visibility, LOD decision, distance scoring, and state-tag calculation into Burst jobs.
- [ ] Batch ECS render-state tags through ECB.
- [ ] Avoid instantiate/destroy churn; keep pooling/retained presentation.
- [ ] Preserve current visible-character detailed-model policy and impostor thresholds.

Acceptance checks:
- [ ] Unit visual LOD behavior remains correct.
- [ ] Selection markers still display.
- [ ] Helicopter blade and missile visual behavior remain correct.
- [ ] Render-budget focused scenario does not regress p95/p99.
- [ ] No new GC during steady-state camera pan/zoom.

### Phase 9: Managed Boundary Cleanup

Status: [ ]

Purpose:
Make it obvious which systems are intentionally managed and prevent accidental performance debt from hiding there.

Implementation steps:
- [ ] Name and document managed boundary systems clearly.
- [ ] Ensure managed systems do not own gameplay policy if they are presentation/binding boundaries.
- [ ] Move data-only logic out of managed composition classes into ECS systems or stateless utilities.
- [ ] Keep config projection at startup and ECS-native data during runtime.
- [ ] Update architecture docs with the Burst/managed-boundary rule.

Acceptance checks:
- [ ] Architecture tests distinguish allowed managed boundaries from hot-path debt.
- [ ] No new broad manager/controller/facade shells are introduced.
- [ ] Runtime gameplay policy is still ECS-owned.

### Phase 10: Performance Ratchet And Completion

Status: [ ]

Purpose:
Lock in improvements and prevent backsliding.

Implementation steps:
- [ ] Re-run all baseline scenarios.
- [ ] Compare before/after metrics.
- [ ] Record the final report under `Design/AgentReports`.
- [ ] Tighten architecture tests from report-only to fail-on-new-debt where stable.
- [ ] Update this roadmap with completed counts:
  - Burst systems increased where appropriate.
  - `To*Array` hot-path count reduced.
  - direct structural changes in frequent loops reduced.
  - no new managed hot-path debt.
- [ ] Keep skipped opt-in balance probes skipped unless explicitly running balance reports.

Acceptance checks:
- [ ] Full EditMode suite passes.
- [ ] Focused PlayMode/runtime smoke passes.
- [ ] `git diff --check` passes.
- [ ] Performance report shows no regression and documents improvements.
- [ ] Roadmap has no incomplete required steps.

## Per-Slice Validation Checklist

- [ ] Inspect touched systems and contracts first.
- [ ] Confirm the system is a real hot path or dependency of a hot path.
- [ ] Make the smallest behavior-preserving change.
- [ ] Run focused tests for the touched domain.
- [ ] Run full EditMode suite in the shadow project after each phase.
- [ ] Run `git diff --check`.
- [ ] Record progress notes in this roadmap.
- [ ] Remove temporary diagnostics/logs before handoff.

## Test Scenarios

Required focused scenarios:
- [x] Match startup to loading gate complete.
- [ ] Select unit and issue Move.
- [ ] Select missile launcher and issue Attack.
- [ ] Select transport and Board All.
- [ ] Passenger drawer unboard/exit-all.
- [ ] AI steady-state with current unit/building counts.
- [ ] Camera pan/zoom while units are visible.
- [x] Pathfinding group move and long-distance move.
- [ ] Initial units spawn with faction base and air platforms.

Required automated validation:
- [ ] Full EditMode suite.
- [x] Existing architecture contract tests.
- [x] Focused pathfinding validation.
- [x] Focused render-budget validation.
- [x] Focused selection/command validation.
- [x] Focused transport boarding validation.

## Definition Of Done

- [ ] No valid editor tests fail.
- [ ] No gameplay UX regression is found in the focused runtime scenarios.
- [ ] Hot-path `ToEntityArray` / `ToComponentDataArray` counts are materially reduced in selection, transport, AI, and combat systems.
- [ ] Frequent structural changes use ECB unless explicitly allowed.
- [ ] Pure ECS hot systems touched by the refactor are Burst-compatible.
- [ ] Managed systems are documented as presentation/bootstrap/config/diagnostic boundaries.
- [ ] Performance reports show equal or improved p95/p99 frame time and no recurring GC regression after warmup.

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
- 104 `ToEntityArray` / `ToComponentDataArray` calls.
- 40 direct `EntityManager` structural or component mutation calls.
- Main array-copy offenders:
  - `SelectionGameplayStartupSystem`: 8 `To*Array` calls.
  - `TransportBoardingCommandSystem`: 6.
  - `CustomGameStartupSystem`: 5.
  - `RtsSelectionFocusCommandSystem`: 3.
  - `AISquadSystem`, `AIStartupSystem`, `FocusableUnitLookupSystem`, `SelectedUnitDebugFireSystem`, `UnitVisualPrefabReferenceBackfillSystem`: 4 each.
  - `ThreatDetectionWarningSystem`: 3.
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

Status: [ ]

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

Status: [ ]

Purpose:
Reduce main-thread copies in AI steady state, especially targeting and combat orders.

Target areas:
- `AITargetingSystem`
- `AICombatOrderSystem`
- `AISquadSystem`
- `AIProductionSystem`
- `AIBuildPlannerSystem`

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

Lane
Gameplay

Task
Complete UnitPathfindingSystem refactor roadmap through step 35 final validation gate.

Files changed
- Assets/Game/Scripts/Components/UnitPathfindingDiagnosticLogComponents.cs
- Assets/Game/Scripts/Systems/UnitHierarchicalPathSystem.cs
- Assets/Game/Scripts/Systems/UnitPathCoarseWorkspaceSystem.cs
- Assets/Game/Scripts/Systems/UnitPathGoalAssignmentSystem.cs
- Assets/Game/Scripts/Systems/UnitPathIgnoredOccupancySystem.cs
- Assets/Game/Scripts/Systems/UnitPathLiveUnitSnapshotSystem.cs
- Assets/Game/Scripts/Systems/UnitPathPlacementValidationSystem.cs
- Assets/Game/Scripts/Systems/UnitPathRequestBufferSystem.cs
- Assets/Game/Scripts/Systems/UnitPathRequestCollectionSystem.cs
- Assets/Game/Scripts/Systems/UnitPathReservedGoalSystem.cs
- Assets/Game/Scripts/Systems/UnitPathResultApplySystem.cs
- Assets/Game/Scripts/Systems/UnitPathRetrySystem.cs
- Assets/Game/Scripts/Systems/UnitPathScratchWorkspaceSystem.cs
- Assets/Game/Scripts/Systems/UnitPathSegmentationSystem.cs
- Assets/Game/Scripts/Systems/UnitPathValidationMetricsSystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindBatchJob.cs
- Assets/Game/Scripts/Systems/UnitPathfindingApplySystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingBudgetSystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticLogFlushSystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticSystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingPendingStateSystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingQuerySystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingScheduleSystem.cs
- Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Assets/Tests/Editor/UnitPathfindingFocusedPerformanceValidation.cs
- Design/Architecture/performance_regression_contract.md
- Design/Architecture/unit_pathfinding_system_refactor_roadmap.md

Contracts touched
- UnitPathfindingSystem refactor roadmap.
- Gameplay architecture contract tests for pathfinding boundaries.
- Performance regression contract pathfinding scenario.

User-visible behavior
- No intended gameplay behavior change.
- Pathfinding remains ECS-owned and data-oriented; request collection, workspaces, goal assignment, scheduling, apply, retry, diagnostics, and pending-state publication now live in narrow systems.
- The final validation pass fixed a request collection boundary issue caused by helper-owned `SystemAPI.Query`; collection now reads initialized ECS queries from `UnitPathfindingQuerySystem`.

Validation run
- Unity architecture validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests.RunUnitPathfindingArchitectureBatchValidation`.
- Focused pathfinding smoke in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `UnitPathfindingFocusedPerformanceValidation.RunBatchValidation`.
- Runtime play-button FPS smoke in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `RuntimeFpsPlayButtonProbe.Run`.
- Scoped whitespace validation: `git diff --check` for pathfinding, tests, roadmap, performance contract, and this handoff.

Validation result
- Architecture validation passed: 35 methods.
- Focused pathfinding smoke passed: 4 manual infantry requests, 1 long-distance vehicle request, 5 updates, elapsedMs 21.10, allocatedBytesCurrentThread 0, pathPoolCells 10, remainingRequests 0, pathDiagnosticsCount 0.
- Runtime play-button FPS smoke completed: clicked Game button, sampleCount 15100, avgFps 339.90, minFps 0.10, maxFps 362.44 in batchmode/nographics.

Known gaps
- Runtime smoke is an editor batchmode/nographics probe, not Android/device rendering acceptance.
- Runtime smoke logs still include unrelated editor/batchmode issues: Entities Graphics nographics exception, Unity Search indexing exception, RenderTexture create failures, and startup hitches in BuildingPlacement/RuntimeCity.
- No `UnitPathfindingSystem` offender sample appeared in the runtime smoke.
- Request collection now uses ECS query chunk iteration to avoid invalid generated query handles; this preserved behavior in focused validation, but native temp chunk-array cost should be watched in future profiler captures.

Cross-lane impacts
- AI/building/city behavior was not intentionally changed.
- Building and citizen pending-path readers now consume the ECS pending-state read boundary instead of `UnitPathfindingSystem` static state.
- Performance smoke surfaced unrelated BuildingPlacement/RuntimeCity startup hitches that should stay with those owners.

Next recommended task
- If product needs performance acceptance, run an interactive editor/device validation with selected squads, manual move, and long-distance move under the real camera/rendering setup. Otherwise continue the next architecture roadmap outside pathfinding.

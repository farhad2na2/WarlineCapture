# UnitPathfindingSystem Refactor Roadmap

This document owns the `UnitPathfindingSystem` refactor plan. The current pathfinding performance is acceptable, so this roadmap is explicitly performance-preserving. Do not add gameplay features, pathing rule changes, or tuning changes during this refactor.

## Target

Target file: `Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs`

Current size at roadmap creation: 2784 lines.

Goal: reduce `UnitPathfindingSystem` from a broad pathfinding monolith into narrow ECS-aligned systems while preserving current movement behavior and current performance characteristics. The final state may keep a small `UnitPathfindingSystem` as the ECS schedule/apply coordinator, but it must not own unrelated diagnostics, request collection, adaptive budget policy, scratch workspace policy, goal assignment policy, result application, or static runtime state.

## Current Responsibility Inventory

- ECS query ownership: path requests, live units, manual move state, path follow state, long-distance move state, retry cooldown state.
- Runtime state: pending job handle, pending stream, pending request counts, pending grid dimensions, pending schedule frame/time, adaptive request budget, stable-batch counters, pending live-unit snapshots.
- Request collection: manual request prioritization, non-manual fallback collection, ignored occupancy resolution for transport boarding, request NativeList population.
- Goal assignment: long-distance segmentation, hierarchical waypoint selection, nearest free goal search, reserved goal footprint epochs, alternate-goal attempts.
- Native workspace ownership: A* scratch arrays, reserved-goal epochs, coarse hierarchical scratch arrays, scratch epoch rollover.
- Job algorithm: `PathfindBatchJob`, traversal costs, A*, open-list management, direct-path checks, infantry/vehicle placement checks, soft blocker rules, path output writing.
- Result application: path pool allocation, `UnitPathFollow` / `UnitPathRange` writes, retry cooldowns, segmented move continuation, abandoned request cleanup.
- Diagnostics: freeze logs, validation logs, stuck samples, hierarchical validation logs, manual move counters.
- Static runtime access: removed. Pending path-job state is published through `UnitPathfindingPendingStateComponent` and read through `UnitPathfindingPendingStateReadSystem`.

## Public/Static Surface Inventory Freeze

This inventory freezes the current public/static surface. New public/static members must not be added to `UnitPathfindingSystem`. Move new behavior to a narrow `*System` boundary or a pure data/math helper.

Allowed temporary public/static members:

- None. `UnitPathfindingSystem` must not expose public/static runtime state.
- `public static bool HasPendingPathJob { get; private set; }`
  - Status: removed from `UnitPathfindingSystem` in step 31.
  - Replacement: `UnitPathfindingPendingStateComponent` plus `UnitPathfindingPendingStateReadSystem`.
- `public static bool CanPlaceForPathing(...)`
  - Type: pure path placement helper.
  - Status: removed from `UnitPathfindingSystem` in step 18.
  - Target owner: `UnitPathPlacementValidationSystem`.
  - Migration step: 18 complete.

Non-static public surface:

- `OnCreate`, `OnDestroy`, and `OnUpdate` remain the ECS system lifecycle entry points until the final coordinator pass.

## Performance Preservation Rules

- Do not change constants, traversal costs, request budgets, search radii, search expansion limits, or segment thresholds unless a separate gameplay/performance task asks for it.
- Do not add per-frame managed allocations. No LINQ, reflection, boxed delegates, `foreach` over managed collections in hot paths, or new string construction unless diagnostics are enabled.
- Do not add virtual/interface dispatch inside the path job or per-node A* loops.
- Do not change `PathfindBatchJob.Schedule(requestCount, state.Dependency)` scheduling semantics until the existing output is covered by focused performance validation.
- Do not change NativeArray/NativeList allocator lifetimes without proving no allocation increase in frame diagnostics.
- Do not replace current data-oriented arrays with managed objects.
- Every extraction must be behavior-preserving first. Optimizations are separate tasks.

## Required Validation Gates

Each implementation step must run at least:

- Compile validation for gameplay scripts.
- Focused architecture validation for the pathfinding roadmap once added.
- `git diff --check`.

Every phase boundary must also run a focused runtime/performance smoke:

- Start gameplay with the current default unit/faction setup.
- Issue a manual move order to a multi-unit infantry group.
- Issue a long-distance move order across the map.
- Confirm no pathfinding freeze logs beyond the existing baseline.
- Capture `FrameRateDiag` or equivalent path timing evidence and compare against the previous phase.

Acceptance target: no measurable regression in path request throughput, no new GC allocation spikes, no lower steady FPS attributable to pathfinding, and no movement behavior drift.

## Non-Goals

- Do not redesign pathfinding, road preference, sidewalk/dirt preference, blocker behavior, vehicle footprint behavior, manual move segmentation, or hierarchical sectors.
- Do not move pathfinding behavior into UI, bootstrap, building, citizen, road, or AI systems.
- Do not create a singleton/service locator/facade replacement.
- Do not use reflection.
- Do not delete `UnitPathfindingSystem` unless the final coordinator has become truly empty and all validation says deletion is safe.

## Phase 1: Baseline And Contracts

1. Complete: Add roadmap and baseline architecture guard
   - Add this document.
   - Add pathfinding architecture validation entry point.
   - Record the 2784-line baseline and current ownership inventory.
   - Guard against new static runtime state, new direct `Debug.Log*` diagnostics, and new broad responsibilities being added to `UnitPathfindingSystem`.
   - Added `GameplayArchitectureContractTests.RunUnitPathfindingArchitectureBatchValidation`.
   - Added guards for roadmap tracking, baseline line count, bounded direct diagnostics, and bounded public static pathfinding surface.

2. Complete: Add performance baseline scenario
   - Add or document a focused validation command/scenario for default gameplay pathfinding.
   - Capture baseline values for request count, adaptive budget, pending wall time, apply time, FPS, and GC allocation.
   - This is the reference for the rest of the roadmap.
   - Added baseline capture record below using the existing `RuntimeFpsPlayButtonProbe` in `WarlineCapture-CodexUnity1`.
   - Baseline command: Unity 6000.4.0f1 batchmode, `-executeMethod RuntimeFpsPlayButtonProbe.Run`, log `/private/tmp/warlinecapture-unit-pathfinding-fps-baseline-step2.log`, report `/private/tmp/warlinecapture-runtime-fps-probe.json`.
   - Baseline result: completed; clicked Game button; sampleCount 13760; avgFps 309.04; minFps 85.82; maxFps 327.59; units 240-259 during sampled slow logs; `Default World UnitPathfindingSystem=0.0ms` in captured `PerfDiag` samples.
   - Baseline scope: editor batchmode smoke only. It validates startup/default-gameplay pathfinding does not show measurable pathfinding cost in the current automated probe, but it is not rendering acceptance and it does not exercise a manual group move or long-distance move.
   - Request count, adaptive budget, pending wall time, and apply time are not emitted by current always-on diagnostics without enabling disabled path logs. Do not turn on disabled path logs just to refactor; later structured metric work must expose these without hot-path string/log overhead.

3. Complete: Freeze public/static surface
   - Inventory external reads of `UnitPathfindingSystem.HasPendingPathJob`.
   - Add a temporary-debt note that the static property remains only until the ECS pending-state component migration.
   - Prevent additional public/static members on `UnitPathfindingSystem`.
   - Added the Public/Static Surface Inventory Freeze section above.
   - Current direct static pending-job readers are `BuildingGameplayCompositionSystem.cs`, `BuildingProductionRuntimeTickSystem.cs`, and `CitizenPopulationLifecycleSystem.cs`.
   - Current allowed public static members are only `HasPendingPathJob` and pure helper `CanPlaceForPathing`.

## Phase 2: Extract Non-Hot Diagnostics And Budget Policy

4. Complete: Extract diagnostics formatting into `UnitPathfindingDiagnosticSystem`
   - Move freeze-log formatting, validation-start/end formatting, stuck-sample formatting, and hierarchical validation message construction.
   - Keep calls gated by the existing diagnostic booleans.
   - No behavior or logging frequency changes.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindingDiagnosticSystem.cs`.
   - `UnitPathfindingSystem` now delegates pathfinding diagnostics and manual-move sample formatting to that boundary.
   - `UnitPathfindingSystem` direct `Debug.Log*` and `StringBuilder` diagnostic formatting count is now zero.
   - Transition size after extraction: `UnitPathfindingSystem.cs` is 2769 lines; `UnitPathfindingDiagnosticSystem.cs` is 242 lines.

5. Complete: Extract adaptive request budgeting into `UnitPathfindingBudgetSystem`
   - Move adaptive budget fields and `UpdateAdaptiveBudget`.
   - Preserve exact budget constants and state transitions.
   - `UnitPathfindingSystem` asks for current request budget and reports completed job timing.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindingBudgetSystem.cs`.
   - Moved adaptive budget state, pending-job reduction state, budget thresholds, stability counters, and request-budget transition logic to that boundary.
   - `UnitPathfindingSystem` now calls `GetCurrentRequestBudget`, `ReduceForPendingJob`, `ReportCompletedJob`, and `ResetPendingJobReduction`.
   - Budget constants are unchanged: max 32, min 1, target 0.008s, low 0.006s, high 0.012s, manual infantry max 4, stable manual batches 2, stable one-frame batches 3.
   - Transition size after extraction: `UnitPathfindingSystem.cs` is 2691 lines; `UnitPathfindingBudgetSystem.cs` is 122 lines.

6. Complete: Extract pending-job status publication into `UnitPathfindingPendingStateSystem`
   - Keep the existing static `HasPendingPathJob` temporarily.
   - Add an ECS singleton/read model path for pending-job state.
   - The static property becomes a compatibility mirror, not the source of truth.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindingPendingStateSystem.cs`.
   - Added `UnitPathfindingPendingStateComponent` with pending flag, request count, request budget, and scheduled frame.
   - `UnitPathfindingSystem` now publishes pending state through the ECS boundary while mirroring `HasPendingPathJob` for temporary callers.
   - Current `HasPendingPathJob` readers migrate in steps 28-31.

## Phase 3: Extract Query And Request Collection Ownership

7. Complete: Extract query creation into `UnitPathfindingQuerySystem`
   - Own request, live-unit, manual move, path-follow, long-distance, retry-cooldown, and manual-follow queries.
   - `UnitPathfindingSystem.OnCreate` delegates query creation and `RequireForUpdate` setup.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindingQuerySystem.cs`.
   - Moved all pathfinding `RequireForUpdate` calls and pathfinding `EntityQueryDesc` construction into that boundary.
   - `UnitPathfindingSystem` now stores one `_queries` boundary and reads query handles from it.
   - Transition size after extraction: `UnitPathfindingSystem.cs` is 2597 lines; `UnitPathfindingQuerySystem.cs` is 121 lines.

8. Complete: Extract request buffers into `UnitPathRequestBufferSystem`
   - Own NativeLists for request entities, unit grids, goals, footprints, movement behaviors, factions, manual flags, ignored occupancy, assigned goals, status, segmented flags, continuation flags, cheap segment flags, alternate-search flags, and alternate-attempt counts.
   - Preserve allocator types and capacity reuse.
   - Added `Assets/Game/Scripts/Systems/UnitPathRequestBufferSystem.cs`.
   - Moved request NativeList fields, persistent allocation, disposal, and collection clearing into that boundary.
   - `UnitPathfindingSystem` now owns one `_requestBuffers` boundary and reads/writes the same NativeLists through it.
   - Allocation policy is unchanged: `Allocator.Persistent`, initial capacity `UnitPathfindingBudgetSystem.MaxRequestsPerFrame`.
   - Transition size after extraction: `UnitPathfindingSystem.cs` is 2539 lines; `UnitPathRequestBufferSystem.cs` is 82 lines.

9. Complete: Extract ignored occupancy collection into `UnitPathIgnoredOccupancySystem`
   - Move transport-boarding ignored-occupancy lookup.
   - Keep exact component checks and fallback values.
   - No extra entity queries per request beyond current behavior.
   - Added `Assets/Game/Scripts/Systems/UnitPathIgnoredOccupancySystem.cs`.
   - Moved `UnitTransportBoardingTarget` ignored-occupancy lookup and fallback writes into that boundary.
   - `UnitPathfindingSystem` now delegates request ignored-occupancy writes while preserving the same `EntityManager` component checks.

10. Complete: Extract request collection into `UnitPathRequestCollectionSystem`
   - Move manual-first request collection and non-manual request collection.
   - Preserve current request ordering and early break at request budget.
   - Output into `UnitPathRequestBufferSystem` without managed allocations.
   - Added `Assets/Game/Scripts/Systems/UnitPathRequestCollectionSystem.cs`.
   - Moved manual-first and non-manual `SystemAPI.Query` request collection loops into that boundary.
   - Marked it `[DisableAutoCreation]` because it is an ECS source-generation boundary invoked by the coordinator, not a standalone scheduled system.
   - Preserved ordering: manual requests first, then non-manual requests excluding manual-tagged entities.
   - Preserved early return/break at current request budget and the same long-distance continuation flag write.
   - Step 35 validation fix: removed helper-owned `SystemAPI.Query` source-generation use because the helper is not a created ECS system; request collection now consumes the initialized `UnitPathfindingQuerySystem` queries through chunk iteration while preserving the same manual-first ordering and budget break behavior.

## Phase 4: Extract Snapshot And Workspace Ownership

11. Complete: Extract live-unit snapshot ownership into `UnitPathLiveUnitSnapshotSystem`
   - Own persistent arrays for live unit entities, grids, footprints, and manual-group flags.
   - Preserve disposal timing and allocation lifetime.
   - Keep snapshot creation before request goal assignment.
   - Added `Assets/Game/Scripts/Systems/UnitPathLiveUnitSnapshotSystem.cs`.
   - Moved persistent live-unit entity/grid/footprint/manual-group snapshot allocation and disposal into that boundary.
   - `UnitPathfindingSystem` still captures snapshots before request goal assignment and disposes them after pending job completion/disposal.
   - Allocation policy is unchanged: `Allocator.Persistent` arrays retained until pending job completion.

12. Complete: Extract A* scratch workspace into `UnitPathScratchWorkspaceSystem`
   - Move scratch arrays, scratch grid size, scratch epoch reservation, `EnsureScratch`, and `DisposeScratch`.
   - Preserve epoch behavior and thread-slot sizing.
   - Added `Assets/Game/Scripts/Systems/UnitPathScratchWorkspaceSystem.cs`.
   - Moved A* scratch arrays, grid size, search epoch, `Ensure`, `ReserveEpochs`, and disposal into that boundary.
   - Preserved current scratch sizing: one thread slot, `total = gridSize`, and `EpochsPerRequest = 128`.
   - `UnitPathfindingSystem` now passes scratch arrays from `_scratchWorkspace` into the batch job.

13. Complete: Extract reserved-goal workspace into `UnitPathReservedGoalSystem`
   - Move reserved-goal epoch arrays, generation counter, `PrepareReservedGoals`, `ReserveGoalFootprint`, and disposal.
   - Keep the nearest-goal reservation behavior unchanged.
   - Added `Assets/Game/Scripts/Systems/UnitPathReservedGoalSystem.cs`.
   - Moved reserved-goal epoch array, generation counter, preparation, disposal, and footprint reservation helper into that boundary.
   - `UnitPathfindingSystem` now consumes `_reservedGoals.Epochs` and `_reservedGoals.Generation` while nearest-goal assignment behavior stays unchanged.

14. Complete: Extract hierarchical coarse workspace into `UnitPathCoarseWorkspaceSystem`
   - Move coarse arrays, coarse dimensions, coarse epoch reservation, `EnsureCoarseScratch`, and disposal.
   - Do not change sector size or expansion limits.
   - Added `Assets/Game/Scripts/Systems/UnitPathCoarseWorkspaceSystem.cs`.
   - Moved hierarchical coarse arrays, dimensions, search epoch, allocation, epoch reset, and disposal into that boundary.
   - `UnitPathfindingSystem` still owns hierarchical path policy for now but reads coarse workspace arrays/dimensions from `_coarseWorkspace`.
   - Sector size and expansion limits are unchanged.

## Phase 5: Extract Goal Assignment And Hierarchical Planning

15. Complete: Extract movement segmentation policy into `UnitPathSegmentationSystem`
   - Move `GetMaxSegmentCells` and segment-goal selection wrapper.
   - Preserve current manual infantry, manual vehicle, and default segment distances.
   - Added `Assets/Game/Scripts/Systems/UnitPathSegmentationSystem.cs`.
   - Moved segment distance constants, max-segment selection, long-distance threshold check, and fallback segment-goal selection into that boundary.
   - Preserved distances: default 32, manual infantry 1024, manual vehicle 128.

16. Complete: Extract hierarchical waypoint planning into `UnitHierarchicalPathSystem`
   - Move sector conversion, coarse indexing, representative cell lookup, coarse passability, waypoint search, and waypoint choice.
   - Consume `UnitPathCoarseWorkspaceSystem`.
   - Preserve exact fallback behavior.
   - Added `Assets/Game/Scripts/Systems/UnitHierarchicalPathSystem.cs`.
   - Moved hierarchical sector size, expansion cap, coarse search, waypoint choice, representative cell lookup, and coarse passability checks into that boundary.
   - `UnitPathfindingSystem` now only ensures the coarse workspace and delegates manual long-distance hierarchical waypoint selection before falling back to the existing direct segment goal.
   - Preserved constants: sector size 32 and max expanded sectors 2048.

17. Complete: Extract nearest free goal assignment into `UnitPathGoalAssignmentSystem`
   - Move `FindNearestFreeGoal`, `CanUseGoalCell`, `IsFree`, ring offset helpers, alternate-goal limits, and reserved-goal interaction.
   - Preserve manual infantry alternate-search skip behavior and vehicle candidate limits.
   - Added `Assets/Game/Scripts/Systems/UnitPathGoalAssignmentSystem.cs`.
   - Moved nearest-free-goal search, goal-cell reservation checks, free-cell helper, goal search radii, alternate-goal candidate caps, and ring offset helper into that boundary.
   - `UnitPathfindingSystem` now delegates assigned-goal selection to `_goalAssignment.FindNearestFreeGoal`.
   - `PathfindBatchJob` now reads alternate-search radii/caps from `UnitPathGoalAssignmentSystem` while keeping its job-local fallback ring helper unchanged for Burst hot-path stability.
   - Temporary bridge removed in step 18: `UnitPathGoalAssignmentSystem.CanUseGoalCell` now calls `UnitPathPlacementValidationSystem.CanPlaceForPathing`.

18. Complete: Extract placement validity helpers into `UnitPathPlacementValidationSystem`
   - Move `CanPlaceForPathing`, infantry/vehicle placement checks, soft blocker checks, manual-group occupancy checks, and faction pass checks.
   - Keep methods static/pure where they are data-only and hot-path friendly.
   - Added `Assets/Game/Scripts/Systems/UnitPathPlacementValidationSystem.cs`.
   - Moved path placement validation, infantry/vehicle footprint checks, dynamic blocker friendly-pass checks, manual-group occupancy ignore checks, vehicle soft-blocker checks, and vehicle occupancy padding into that boundary.
   - Removed `UnitPathfindingSystem.CanPlaceForPathing`; callers now use `UnitPathPlacementValidationSystem.CanPlaceForPathing`.
   - `PathfindBatchJob` still owns traversal/path writing, but delegates placement validity to the static data-oriented validation boundary.

## Phase 6: Extract Result Application

19. Complete: Extract path result application into `UnitPathResultApplySystem`
   - Move `ApplyResults` and path-pool write behavior.
   - Preserve all component add/remove/set decisions.
   - Reacquire ECS data after structural changes; do not keep invalid DynamicBuffer handles.
   - Added `Assets/Game/Scripts/Systems/UnitPathResultApplySystem.cs`.
   - Moved path stream reading, path-pool writes, `UnitTarget` / `UnitPathFollow` / `UnitPathRange` writes, long-distance continuation writes, retry cooldown writes, abandon cleanup, and request removal into that boundary.
   - `UnitPathfindingSystem` now delegates pending path result application to `_resultApply.Apply` and only persists the returned path pool.

20. Complete: Extract retry/abandon policy into `UnitPathRetrySystem`
   - Move failed manual retry delay, retry cooldown application, segmented retry accounting, and abandoned request cleanup.
   - Preserve exact retry frame delays and counters.
   - Added `Assets/Game/Scripts/Systems/UnitPathRetrySystem.cs`.
   - Moved failed manual retry delay, retry eligibility checks, segmented retry target restoration, retry cooldown application, manual retry counters, and abandoned request cleanup into that boundary.
   - `UnitPathResultApplySystem` now delegates failure retry/abandon decisions to `UnitPathRetrySystem`.

21. Complete: Extract validation counters into `UnitPathValidationMetricsSystem`
   - Own manual validation counters, peaks, totals, and stuck-log scheduling.
   - Diagnostics system formats messages; metrics system owns state.
   - Added `Assets/Game/Scripts/Systems/UnitPathValidationMetricsSystem.cs`.
   - Moved validation active state, peak counters, total counters, stuck-log intervals, stuck sample count, and stuck-log next-frame scheduling into that boundary.
   - `UnitPathfindingSystem` now builds frame input/result snapshots and delegates validation metric begin/update/end decisions to `_validationMetrics`.
   - `UnitPathfindingDiagnosticSystem` remains the formatter for validation start/end/stuck messages.

## Phase 7: Move The Job Without Changing The Job

22. Complete: Move `PathfindBatchJob` into `UnitPathfindBatchJob.cs`; current location is `Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs`
   - Move the job struct and its private job-local helpers as a mechanical relocation.
   - Do not change field order, scheduling, traversal costs, placement rules, or output format.
   - This is the highest-risk mechanical step; validate immediately after.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindBatchJob.cs`; later moved to `Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs` as a path/name-only architecture cleanup.
   - Moved the Burst job, job-local traversal constants, search directions, `TryWritePath`, direct-path helper, traversal-cost helper, scratch-node initialization, fallback segment writer, and square-ring helper into that file.
   - `UnitPathfindingSystem` still constructs `new PathfindBatchJob` and schedules it with `job.Schedule(requestCount, state.Dependency)` unchanged.

23. Complete: Reject job input/output struct extraction for performance stability
   - Introduce `UnitPathfindBatchInput` and `UnitPathfindBatchOutput` only if they are blittable/job-safe and do not add copy overhead.
   - If profiling shows overhead or Burst incompatibility, keep flat job fields and mark this step complete as "rejected for performance".
   - Decision: rejected for performance and Burst layout stability. `PathfindBatchJob` keeps its flat NativeArray/NativeBitArray fields, `NativeStream.Writer`, status arrays, and scratch arrays directly on the job.
   - Do not add `UnitPathfindBatchInput`, `UnitPathfindBatchOutput`, or wrapper/context structs for this job without a separate profiling task proving no scheduling, copy, Burst, or hot-path layout regression.

24. Complete: Reject traversal-cost helper extraction without generated-code proof
   - Move traversal cost constants/helpers only if generated code remains equivalent.
   - No virtual/interface dispatch in `Execute`, `TryWritePath`, or per-node loops.
   - Decision: rejected for performance stability. Traversal cost constants, search directions, `GetTraversalCost`, and `HeuristicOctile` remain job-local inside `PathfindBatchJob`.
   - Do not extract traversal-cost helpers out of the job unless a separate profiling/generated-code task proves equivalent Burst output and no per-node overhead.

## Phase 8: Split Scheduling And Apply Phases

25. Complete: Extract scheduling phase into `UnitPathfindingScheduleSystem`
   - Own grid access, workspace preparation, request collection invocation, goal assignment invocation, job construction, and job scheduling.
   - Return a pending-job state object owned by the main coordinator.
   - Preserve current early returns and disposal paths.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindingScheduleSystem.cs`.
   - `UnitPathfindingScheduleSystem` now owns grid singleton access through `UnitPathfindingQuerySystem.GridQuery`, scratch preparation, live-unit snapshot capture, request collection, reserved-goal preparation, goal assignment, hierarchical waypoint scheduling diagnostics, `NativeStream` allocation, `PathfindBatchJob` construction, and `job.Schedule(requestCount, state.Dependency)`.
   - `UnitPathfindingSystem` now receives a `UnitPathfindingScheduleSystem.Result`, stores the pending job state, resets pending-job budget reduction, and publishes pending state.
   - Current behavior is preserved: early returns for no request query/no collected requests, live-unit snapshot disposal on zero collected requests, and scheduling dependency assignment remain unchanged.

26. Complete: Extract apply phase into `UnitPathfindingApplySystem`
   - Own pending job completion, budget reporting, result application, validation metric updates, and pending resource disposal.
   - Preserve current one-frame/nonblocking behavior when the job is still running.
   - Added `Assets/Game/Scripts/Systems/UnitPathfindingApplySystem.cs`.
   - `UnitPathfindingApplySystem` now owns pending job completion, budget reporting, path-pool result application, manual validation metric updates, validation stuck-sample diagnostics, async timing diagnostics, and disposal/reset of the pending stream plus live-unit snapshot.
   - `UnitPathfindingSystem` now delegates completed pending-job application and pending-job disposal to `_apply`, then publishes the pending-state read model.
   - Current nonblocking behavior is preserved: incomplete jobs still return early with `state.Dependency = _pendingPathHandle`; apply only runs after `_pendingPathHandle.IsCompleted`.

27. Complete: Reduce `UnitPathfindingSystem.OnUpdate` to coordinator flow
   - The coordinator should only:
     - exit when play is not requested,
     - dispose pending work if needed,
     - wait for pending job if incomplete,
     - apply completed job,
     - schedule new job if requests exist.
   - No path rules or diagnostics formatting should remain here.
   - `UnitPathfindingSystem.OnUpdate` now only handles play-state exit, pending-job wait/apply delegation, schedule delegation, pending state storage, and pending-state publication.
   - Scheduling rules live in `UnitPathfindingScheduleSystem`; completed-job application, validation metrics, diagnostics, and pending resource disposal live in `UnitPathfindingApplySystem`.
   - Guarded by architecture tests so path rules, job construction, result application, validation stuck sample formatting, and completed resource disposal do not return to the coordinator.

## Phase 9: Remove Static Pending-State Debt

28. Complete: Migrate building production pending-path reads
   - Move `BuildingProductionRuntimeTickSystem` and building composition callbacks from `UnitPathfindingSystem.HasPendingPathJob` to the ECS pending-state boundary.
   - Added `UnitPathfindingPendingStateReadSystem` as a managed ECS read-model boundary over `UnitPathfindingPendingStateComponent`.
   - `BuildingProductionRuntimeTickSystem.Context` now receives a pending-path delegate from the ECS read-model boundary instead of reading `UnitPathfindingSystem.HasPendingPathJob`.
   - `BuildingGameplayCompositionSystem` now wires building production and building selection-click callbacks through `UnitPathfindingPendingStateReadSystem.HasPendingPathJob`.
   - Remaining temporary static reader after this step was `CitizenPopulationLifecycleSystem.cs`.

29. Complete: Migrate citizen pending-path reads
   - Move citizen lifecycle/runtime update paths from `UnitPathfindingSystem.HasPendingPathJob` to the ECS pending-state boundary.
   - `CitizenPopulationCompositionSystem.Result` now owns a `UnitPathfindingPendingStateReadSystem`.
   - `CitizenPopulationRuntimeUpdateSystem` passes `UnitPathfindingPendingStateReadSystem.HasPendingPathJob` into the lifecycle update.
   - `CitizenPopulationLifecycleSystem` now receives a pending-path delegate and no longer reads `UnitPathfindingSystem.HasPendingPathJob`.

30. Complete: Migrate selection/building click pending-path reads
   - Move selection/building click guards from the static property to the ECS pending-state boundary.
   - Building selection click wiring was migrated with the building composition callbacks in step 28.
   - There are now no production readers of `UnitPathfindingSystem.HasPendingPathJob`; the property remains only as temporary compatibility debt until deletion in step 31.

31. Complete: Delete `UnitPathfindingSystem.HasPendingPathJob`
   - Remove the public static property after all callers are migrated.
   - Add an architecture guard that it cannot return.
   - Removed the public static compatibility property from `UnitPathfindingSystem`.
   - `PublishPendingState` now only writes the ECS read model; there is no static mirror.
   - Architecture validation now guards that no production or test source reads `UnitPathfindingSystem.HasPendingPathJob`.

## Phase 10: Diagnostics Boundary Cleanup

32. Complete: Move pathfinding diagnostics to ECS diagnostic events
   - Replace direct `Debug.Log*` pathfinding diagnostics with structured pathfinding diagnostic events plus an existing or narrow flush system.
   - Keep diagnostics disabled by default unless the current flags/config enable them.
   - Added `UnitPathfindingDiagnosticLogQueueComponent` and `UnitPathfindingDiagnosticLogComponent` as the ECS diagnostic event queue.
   - Added `UnitPathfindingDiagnosticLogFlushSystem` as the only direct Unity log emitter for pathfinding diagnostics.
   - `UnitPathfindingDiagnosticSystem` now formats and enqueues diagnostic events; schedule/apply systems pass `EntityManager` into the diagnostics boundary and do not call `Debug.Log*` directly.

33. Complete: Add pathfinding performance contract coverage
   - Add contract wording in `performance_regression_contract.md` for the pathfinding scenario and budgets.
   - Add architecture tests that require the roadmap and forbid new direct hot-path diagnostics/static state in pathfinding.
   - Added a pathfinding performance scenario section to `Design/Architecture/performance_regression_contract.md`.
   - Architecture validation now requires the pathfinding performance scenario, current budget constants, no direct hot-path `Debug.Log*`, no scene searches, no LINQ filters/projections, and no return of mutable public static pending-job state.

## Phase 11: Final Pass

34. Complete: Compatibility and file ownership audit
   - Confirm no extracted system depends back on `UnitPathfindingSystem` for behavior other than the coordinator.
   - Confirm each extracted system has one responsibility and does not become a new broad shell.
   - Confirm no new managed allocations were introduced in the hot path.
   - Audited extracted pathfinding boundaries: no extracted pathfinding boundary depends back on `UnitPathfindingSystem` for behavior.
   - `UnitPathfindingSystem.cs` is now a narrow coordinator under the step 27 line-count guard.
   - Architecture validation now rejects managed `List`/`Dictionary`, LINQ filter/projection/list materialization, and coordinator diagnostic-formatting drift in extracted pathfinding boundaries.

35. Complete: Final validation gate
   - Run compile validation.
   - Run pathfinding architecture validation.
   - Run gameplay load/play smoke.
   - Run focused pathfinding performance smoke with manual group move and long-distance move.
   - Compare against the Phase 1 baseline and write a handoff report if the roadmap is complete.
   - Completed compile/architecture validation through `GameplayArchitectureContractTests.RunUnitPathfindingArchitectureBatchValidation`: passed 35 methods.
   - Completed existing runtime load/play smoke with `RuntimeFpsPlayButtonProbe.Run`: result `completed`, clicked Game button, sampleCount 15100, avgFps 339.90, minFps 0.10, maxFps 362.44 in batchmode/nographics.
   - Added and ran `UnitPathfindingFocusedPerformanceValidation.RunBatchValidation`.
   - Focused pathfinding result: passed; manual infantry requests 4; long-distance vehicle requests 1; updates 5; elapsedMs 21.10; allocatedBytesCurrentThread 0; pathPoolCells 10; remainingRequests 0; pathDiagnosticsCount 0.
   - Focused report: `/private/tmp/warlinecapture-unit-pathfinding-focused-performance.json`.
   - Handoff report: `Design/AgentReports/2026-05-27_gameplay-unit-pathfinding-refactor-final.md`.
   - Existing runtime smoke log also includes non-pathfinding batchmode/editor issues from Entities Graphics nographics, Unity Search indexing, startup hitches in BuildingPlacement/RuntimeCity, and no `UnitPathfindingSystem` offender sample.

## Completion Definition

- `UnitPathfindingSystem` is a narrow ECS coordinator or is deleted only if no coordinator is needed.
- No static mutable pathfinding runtime state remains.
- Request collection, workspace ownership, goal assignment, result application, diagnostics, budget policy, and job code live in narrow `*System` or pure static data/math helper boundaries.
- The path job output format and movement behavior are unchanged.
- Focused performance evidence shows no regression from the Phase 1 baseline.

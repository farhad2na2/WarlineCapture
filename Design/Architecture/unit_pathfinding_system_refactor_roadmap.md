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
- Static runtime access: `UnitPathfindingSystem.HasPendingPathJob` is read by building production/citizen/selection-facing paths.

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

1. Add roadmap and baseline architecture guard
   - Add this document.
   - Add pathfinding architecture validation entry point.
   - Record the 2784-line baseline and current ownership inventory.
   - Guard against new static runtime state, new direct `Debug.Log*` diagnostics, and new broad responsibilities being added to `UnitPathfindingSystem`.

2. Add performance baseline scenario
   - Add or document a focused validation command/scenario for default gameplay pathfinding.
   - Capture baseline values for request count, adaptive budget, pending wall time, apply time, FPS, and GC allocation.
   - This is the reference for the rest of the roadmap.

3. Freeze public/static surface
   - Inventory external reads of `UnitPathfindingSystem.HasPendingPathJob`.
   - Add a temporary-debt note that the static property remains only until the ECS pending-state component migration.
   - Prevent additional public/static members on `UnitPathfindingSystem`.

## Phase 2: Extract Non-Hot Diagnostics And Budget Policy

4. Extract diagnostics formatting into `UnitPathfindingDiagnosticSystem`
   - Move freeze-log formatting, validation-start/end formatting, stuck-sample formatting, and hierarchical validation message construction.
   - Keep calls gated by the existing diagnostic booleans.
   - No behavior or logging frequency changes.

5. Extract adaptive request budgeting into `UnitPathfindingBudgetSystem`
   - Move adaptive budget fields and `UpdateAdaptiveBudget`.
   - Preserve exact budget constants and state transitions.
   - `UnitPathfindingSystem` asks for current request budget and reports completed job timing.

6. Extract pending-job status publication into `UnitPathfindingPendingStateSystem`
   - Keep the existing static `HasPendingPathJob` temporarily.
   - Add an ECS singleton/read model path for pending-job state.
   - The static property becomes a compatibility mirror, not the source of truth.

## Phase 3: Extract Query And Request Collection Ownership

7. Extract query creation into `UnitPathfindingQuerySystem`
   - Own request, live-unit, manual move, path-follow, long-distance, retry-cooldown, and manual-follow queries.
   - `UnitPathfindingSystem.OnCreate` delegates query creation and `RequireForUpdate` setup.

8. Extract request buffers into `UnitPathRequestBufferSystem`
   - Own NativeLists for request entities, unit grids, goals, footprints, movement behaviors, factions, manual flags, ignored occupancy, assigned goals, status, segmented flags, continuation flags, cheap segment flags, alternate-search flags, and alternate-attempt counts.
   - Preserve allocator types and capacity reuse.

9. Extract ignored occupancy collection into `UnitPathIgnoredOccupancySystem`
   - Move transport-boarding ignored-occupancy lookup.
   - Keep exact component checks and fallback values.
   - No extra entity queries per request beyond current behavior.

10. Extract request collection into `UnitPathRequestCollectionSystem`
   - Move manual-first request collection and non-manual request collection.
   - Preserve current request ordering and early break at request budget.
   - Output into `UnitPathRequestBufferSystem` without managed allocations.

## Phase 4: Extract Snapshot And Workspace Ownership

11. Extract live-unit snapshot ownership into `UnitPathLiveUnitSnapshotSystem`
   - Own persistent arrays for live unit entities, grids, footprints, and manual-group flags.
   - Preserve disposal timing and allocation lifetime.
   - Keep snapshot creation before request goal assignment.

12. Extract A* scratch workspace into `UnitPathScratchWorkspaceSystem`
   - Move scratch arrays, scratch grid size, scratch epoch reservation, `EnsureScratch`, and `DisposeScratch`.
   - Preserve epoch behavior and thread-slot sizing.

13. Extract reserved-goal workspace into `UnitPathReservedGoalSystem`
   - Move reserved-goal epoch arrays, generation counter, `PrepareReservedGoals`, `ReserveGoalFootprint`, and disposal.
   - Keep the nearest-goal reservation behavior unchanged.

14. Extract hierarchical coarse workspace into `UnitPathCoarseWorkspaceSystem`
   - Move coarse arrays, coarse dimensions, coarse epoch reservation, `EnsureCoarseScratch`, and disposal.
   - Do not change sector size or expansion limits.

## Phase 5: Extract Goal Assignment And Hierarchical Planning

15. Extract movement segmentation policy into `UnitPathSegmentationSystem`
   - Move `GetMaxSegmentCells` and segment-goal selection wrapper.
   - Preserve current manual infantry, manual vehicle, and default segment distances.

16. Extract hierarchical waypoint planning into `UnitHierarchicalPathSystem`
   - Move sector conversion, coarse indexing, representative cell lookup, coarse passability, waypoint search, and waypoint choice.
   - Consume `UnitPathCoarseWorkspaceSystem`.
   - Preserve exact fallback behavior.

17. Extract nearest free goal assignment into `UnitPathGoalAssignmentSystem`
   - Move `FindNearestFreeGoal`, `CanUseGoalCell`, `IsFree`, ring offset helpers, alternate-goal limits, and reserved-goal interaction.
   - Preserve manual infantry alternate-search skip behavior and vehicle candidate limits.

18. Extract placement validity helpers into `UnitPathPlacementValidationSystem`
   - Move `CanPlaceForPathing`, infantry/vehicle placement checks, soft blocker checks, manual-group occupancy checks, and faction pass checks.
   - Keep methods static/pure where they are data-only and hot-path friendly.

## Phase 6: Extract Result Application

19. Extract path result application into `UnitPathResultApplySystem`
   - Move `ApplyResults` and path-pool write behavior.
   - Preserve all component add/remove/set decisions.
   - Reacquire ECS data after structural changes; do not keep invalid DynamicBuffer handles.

20. Extract retry/abandon policy into `UnitPathRetrySystem`
   - Move failed manual retry delay, retry cooldown application, segmented retry accounting, and abandoned request cleanup.
   - Preserve exact retry frame delays and counters.

21. Extract validation counters into `UnitPathValidationMetricsSystem`
   - Own manual validation counters, peaks, totals, and stuck-log scheduling.
   - Diagnostics system formats messages; metrics system owns state.

## Phase 7: Move The Job Without Changing The Job

22. Move `PathfindBatchJob` into `UnitPathfindBatchJob.cs`
   - Move the job struct and its private job-local helpers as a mechanical relocation.
   - Do not change field order, scheduling, traversal costs, placement rules, or output format.
   - This is the highest-risk mechanical step; validate immediately after.

23. Extract job input/output structs
   - Introduce `UnitPathfindBatchInput` and `UnitPathfindBatchOutput` only if they are blittable/job-safe and do not add copy overhead.
   - If profiling shows overhead or Burst incompatibility, keep flat job fields and mark this step complete as "rejected for performance".

24. Extract traversal-cost helper as pure static data
   - Move traversal cost constants/helpers only if generated code remains equivalent.
   - No virtual/interface dispatch in `Execute`, `TryWritePath`, or per-node loops.

## Phase 8: Split Scheduling And Apply Phases

25. Extract scheduling phase into `UnitPathfindingScheduleSystem`
   - Own grid access, workspace preparation, request collection invocation, goal assignment invocation, job construction, and job scheduling.
   - Return a pending-job state object owned by the main coordinator.
   - Preserve current early returns and disposal paths.

26. Extract apply phase into `UnitPathfindingApplySystem`
   - Own pending job completion, budget reporting, result application, validation metric updates, and pending resource disposal.
   - Preserve current one-frame/nonblocking behavior when the job is still running.

27. Reduce `UnitPathfindingSystem.OnUpdate` to coordinator flow
   - The coordinator should only:
     - exit when play is not requested,
     - dispose pending work if needed,
     - wait for pending job if incomplete,
     - apply completed job,
     - schedule new job if requests exist.
   - No path rules or diagnostics formatting should remain here.

## Phase 9: Remove Static Pending-State Debt

28. Migrate building production pending-path reads
   - Move `BuildingProductionRuntimeTickSystem` and building composition callbacks from `UnitPathfindingSystem.HasPendingPathJob` to the ECS pending-state boundary.

29. Migrate citizen pending-path reads
   - Move citizen lifecycle/runtime update paths from `UnitPathfindingSystem.HasPendingPathJob` to the ECS pending-state boundary.

30. Migrate selection/building click pending-path reads
   - Move selection/building click guards from the static property to the ECS pending-state boundary.

31. Delete `UnitPathfindingSystem.HasPendingPathJob`
   - Remove the public static property after all callers are migrated.
   - Add an architecture guard that it cannot return.

## Phase 10: Diagnostics Boundary Cleanup

32. Move pathfinding diagnostics to ECS diagnostic events
   - Replace direct `Debug.Log*` pathfinding diagnostics with structured pathfinding diagnostic events plus an existing or narrow flush system.
   - Keep diagnostics disabled by default unless the current flags/config enable them.

33. Add pathfinding performance contract coverage
   - Add contract wording in `performance_regression_contract.md` for the pathfinding scenario and budgets.
   - Add architecture tests that require the roadmap and forbid new direct hot-path diagnostics/static state in pathfinding.

## Phase 11: Final Pass

34. Compatibility and file ownership audit
   - Confirm no extracted system depends back on `UnitPathfindingSystem` for behavior other than the coordinator.
   - Confirm each extracted system has one responsibility and does not become a new broad shell.
   - Confirm no new managed allocations were introduced in the hot path.

35. Final validation gate
   - Run compile validation.
   - Run pathfinding architecture validation.
   - Run gameplay load/play smoke.
   - Run focused pathfinding performance smoke with manual group move and long-distance move.
   - Compare against the Phase 1 baseline and write a handoff report if the roadmap is complete.

## Completion Definition

- `UnitPathfindingSystem` is a narrow ECS coordinator or is deleted only if no coordinator is needed.
- No static mutable pathfinding runtime state remains.
- Request collection, workspace ownership, goal assignment, result application, diagnostics, budget policy, and job code live in narrow `*System` or pure static data/math helper boundaries.
- The path job output format and movement behavior are unchanged.
- Focused performance evidence shows no regression from the Phase 1 baseline.

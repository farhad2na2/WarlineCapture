# Match Editor Profiler Baseline — Analysis

Date: 2026-06-11
Source: User-recorded Unity Profiler capture (~14,235 frames, Play Mode, Apple M5), read directly from Profiler Capture Highlights + Hierarchy views.

## Measured facts

- Frame time: median 12.34 ms, min 10.50 ms, **max 97.20 ms** (frame 13748). CPU over 60 FPS target on **23%** of frames; GPU on 7%.
- Systems impact (mean): Scripts 9.48 ms, Others 9.40 ms (mostly editor/profiler overhead), **Rendering 0.46 ms** — rendering is not the bottleneck in editor.
- **Top marker on every one of the 5 worst frames: `PathfindBatchJob (Burst)` at 81.7–84.5 ms self time** (frames 13748, 13768, 13991, 14010, 14030 — clustered, i.e. during battle).
- GC: **1,867,408 allocations** across the capture (~130/frame); worst frame 2,771 allocations / 179.7 KB; steady state ~58.9 KB/frame. Top GC contributor on every entry: `MatchSceneView.Update` (the managed bootstrap composition loop).

## Root cause of the 120 → 30 FPS battle drop

`PathfindBatchJob` is an `IJobFor` scheduled with `job.Schedule(requestCount, state.Dependency)` (UnitPathfindingScheduleSystem.cs:270) — **single-threaded**: one worker processes up to 32 requests sequentially, each allowed up to 30,000 A* expansions in segmented mode (`InfantrySegmentedMaxAStarExpansions`, PathfindBatchJob.cs:19). During battle bursts this produces single jobs of 80+ ms.

The system intends to run the job asynchronously across frames (`IsCompleted` check + adaptive budget in `UnitPathfindingBudgetSystem`), but the pending handle is chained into `state.Dependency` and the job reads live ECS buffers (`walkable.AsNativeArray()` etc.). Any downstream structural change or write to those buffers forces a sync → the main thread blocks on the 80 ms job → 97 ms frames.

Secondary: constant managed allocations from the `MatchSceneView.Update` composition path, including per-frame diagnostic string interpolation (e.g. `[BuildingPlacementDiag] …` in BuildingPlacementRuntimeTickDiagnosticsSystem.cs:96). ~59 KB/frame steady state guarantees recurring GC cost, worse on device.

## Fix plan (ordered by expected impact)

1. **Parallelize the path job.** `Schedule(requestCount, dep)` → `ScheduleParallel(requestCount, 1, dep)`. The 32 requests spread across worker threads (~8 on M5) → job duration cut roughly by core count. Prerequisite: audit job fields for parallel-safety (NativeStream.Writer is per-index safe; verify no shared writable state).
2. **Break the sync chain.** Snapshot all job inputs (walkable/roads/occupancy buffers — live-unit snapshot system already exists, extend the pattern) so the pending job holds no dependency on live ECS data and nothing on the main thread can ever be forced to wait for it. Apply results only when `IsCompleted`.
3. **Tame worst-case search cost.** 30,000 expansions per segmented infantry request is the budget-killer; lean harder on the hierarchical sector path (already implemented) and lower the segmented cap; under battle bursts reduce per-job request count further (budget system already adapts — tighten its ceiling).
4. **Kill steady-state GC.** Strip/conditionally-compile per-frame diagnostic strings; eliminate allocations in the MatchSceneView.Update composition path until steady state is 0 B/frame.
5. **Re-capture** identical scenario; compare median, p95, max, GC/frame.

## Implemented fixes (2026-06-11, same day)

1. **Binary-heap A* open list** (`PathfindBatchJob.cs`, `UnitPathScratchWorkspaceSystem.cs`): the open list was a linear O(n) scan per expansion (O(n²) total — the true cause of 80 ms batches at 30k expansions). Replaced with a packed `(fScore << 32 | cellIndex)` min-heap with lazy stale-entry deletion. Note: `ScheduleParallel` with per-thread scratch was evaluated and rejected — at 2048×1024 cells it would cost ~46 MB × threadCount.
2. **Grid snapshot + dependency decoupling** (`UnitPathGridSnapshotSystem.cs` new, `UnitPathfindingScheduleSystem.cs`, `UnitPathfindingSystem.cs`): all job inputs (walkable/roads/sidewalk/dirt buffers, blocker/occupancy bit arrays) are copied into system-owned snapshots at schedule time (~8 MB memcpy); the job is scheduled against an empty dependency and no longer chained into `state.Dependency`. No main-thread system can ever be forced to Complete() the path batch mid-frame. Also fixes a latent race where the in-flight job read live bit arrays that other systems mutate.

### Verified results (second profiler capture, same Match battle scenario)

- `PathfindBatchJob` no longer appears in top markers at all (was 81–84 ms on each of the 5 worst frames).
- Worst remaining gameplay frames ~28 ms; capture max is a one-time editor asset-database hitch (EngineJob/Sort Direntries at play start), not gameplay.
- Battle-frame composition now: ScriptRunBehaviourUpdate 6.8 ms + ScriptRunBehaviourLateUpdate 5.6 ms (34.5 KB GC) + SimulationSystemGroup 5.4 ms → managed MonoBehaviour path + GC churn is the new top target.
- EditMode suite: 387 passed / 92 failed / 2 skipped. All pathfinding/movement fixtures pass (UnitPathfindingFocusedPerformanceValidation, UnitMoveOrderSystemTests, UnitMovementBlocker/ConfigValidationTests, GridUtilityTests). Sampled failures (AI config-budget asserts, UI prefab tests, obsolete-API contract on ScriptArchitectureAlignmentContractTests.cs:397, UnitTargetOrderSystem breach assert) are all in code untouched by these fixes and predate them — the project's own 2026-06-10 blocker report documents the broken validation loop.

### Next target

GC churn: ~58 KB allocated per frame steadily, peaks 182 KB/2,813 allocations in one frame, top contributor `MatchSceneView.Update`/LateUpdate path. Needs a profiler GC-callstack drill-down (enable Call Stacks in Profiler, sort Hierarchy by GC Alloc) before touching code.

## Notes

- Editor numbers include profiler/editor overhead (~"Others" 9.4 ms); absolute values will differ on device, but the PathfindBatchJob spikes and GC churn transfer directly.
- Rendering at 0.46 ms mean means visual-side optimization is currently irrelevant to the drops — do not spend effort there yet.

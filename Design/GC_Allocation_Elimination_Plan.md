# GC Allocation Elimination Plan — Match Runtime

Date: 2026-06-11
Lane: Gameplay/Performance
Status: Ready for pickup
Prerequisite reading: `Design/AgentReports/2026-06-11_perf_match-editor-profiler-baseline-analysis.md`

## Goal

Reduce steady-state managed allocations during Match gameplay from ~58 KB/frame to **0 B/frame**, and eliminate the allocation spikes (worst measured: 182.7 KB / 2,813 allocations in a single frame). This removes recurring GC cost on the Mac editor and, more importantly, on the Android target where GC pauses are far more expensive.

## Verified baseline (do not re-derive, already measured)

From two Unity Profiler captures of Match battles (2026-06-11):

- Steady state: ~58 KB GC alloc per frame, every frame, during normal play.
- Total across a ~5,700-frame session: 1.85M allocations.
- Top contributor (every entry in Profiler "Top contributors to GC allocations"): `Game.Runtime.dll::MatchSceneView.Update...` — the managed composition path driven by `MatchSceneView` (Update and LateUpdate).
- Battle-frame split: `Update.ScriptRunBehaviourUpdate` ~6.8 ms / 14.4 KB GC, `PreLateUpdate.ScriptRunBehaviourLateUpdate` ~5.6 ms / **34.5 KB GC**, `SimulationSystemGroup` ~5.4 ms / 8.5 KB GC.
- Pathfinding is already fixed (heap A* + input snapshots) — do not touch `PathfindBatchJob`, `UnitPathfindingScheduleSystem`, `UnitPathfindingSystem`, or `UnitPathGridSnapshotSystem` in this task.

## Step 1 — Locate exact allocation sites (required before any code change)

1. Open Unity Profiler, target Play Mode, enable **Call Stacks** (toolbar button: Call Stacks → GC.Alloc). Keep Deep Profile OFF.
2. Play Match, start a battle, record ~300 frames during combat.
3. Select a steady-state frame → CPU module → Hierarchy view → sort by **GC Alloc** descending. Expand the top entries; with Call Stacks enabled, each GC.Alloc sample carries the allocating C# call stack.
4. Record the top ~15 allocation sites (method, bytes, calls/frame) for three frame types: steady-state, battle, and the 2,813-allocation spike frame type (find via Highlights → Allocations → "Frame with highest count").
5. Write the list into the work report before changing code.

Known starting suspects (verify against call stacks, do not fix blind):

- **LateUpdate path (34.5 KB/frame)**: `UnitImpostorRenderSystem` (managed batching: `Dictionary<Material, BatchState>` lookups/iterators, possible per-frame list/array churn), minimap projection (`MatchHudMinimapProjectionSystem`), attack traces (`UnitAttackTraceSystem` uses `DrawMeshInstanced` with managed arrays).
- **Update path (14.4 KB/frame)**: `MatchBootstrapSystem.Update` → `UpdateRuntime(...)` composition chain — look for per-frame `Func<>`/`Action<>` closure creation, LINQ, `foreach` over interfaces (enumerator boxing), string interpolation in diagnostics that runs even when the gate is open (e.g. `BuildingPlacementRuntimeTickDiagnosticsSystem.LogIfSlow` builds its string at most 1/sec — low priority, but `[FreezeDetect]`/`[AttackMarkerDiag]`-style loggers seen firing in the status bar during play should be audited for per-frame string building).
- **SimulationSystemGroup (8.5 KB/frame)**: managed `ToEntityArray(Allocator.Temp)`-adjacent code paths in non-Burst systems; boxing in `SystemAPI.ManagedAPI` accesses.

## Step 2 — Fix patterns (apply per measured site)

- Per-frame `new List<>`/`new T[]` → preallocate and reuse; `List.Clear()` instead of new.
- Closures/lambdas created per frame → hoist to cached delegates or pass static lambdas with explicit state argument.
- String interpolation/concat in per-frame logs → gate behind `if (!enabled) return;` BEFORE building the string, or wrap the call in `[System.Diagnostics.Conditional("UNITY_EDITOR")]` helpers; rate-limit at the call site, not inside the message builder.
- LINQ in hot paths → explicit loops.
- `foreach` over `IEnumerable<T>`-typed references → type as concrete `List<T>`/array to avoid enumerator boxing.
- `Dictionary` iteration per frame in impostor batching → iterate a parallel `List` of values maintained on add/remove, or switch keys to a precomputed index.
- Unity API allocators (`Camera.allCameras`, `.material`, `GetComponentsInChildren` without buffer, `Mesh.vertices`, etc.) → non-allocating overloads (`GetComponentsInChildren(list)`, `sharedMaterial`, ...).

Rules: one commit-sized change per allocation site or per file; do NOT refactor architecture (no new `Controller`/`Presenter`/`Bridge`/`Button` class names per project contract; keep the existing system-composition style).

## Step 3 — Validation gates (per project convention + perf)

1. Compile clean: `Assembly-CSharp`, `Assembly-CSharp-Editor` — no new warnings (the `EditorScriptsMustNotUseKnownUnity6ObsoleteWarningApis` contract test guards this; it currently fails on a pre-existing issue in `ScriptArchitectureAlignmentContractTests.cs:397` — do not count that one against this task, and do not fix it here unless asked).
2. EditMode tests for every touched area (Test Runner filters): `UnitRenderBudgetSystemTests`, `MatchHudMinimapProjectionSystemTests`, `RuntimeDiagnosticsSystemTests`, plus any fixture matching the files actually edited. Known pre-existing failures are documented in `Design/AgentReports/2026-06-11_perf_match-editor-profiler-baseline-analysis.md` — compare against that, only NEW failures block.
3. Re-profile the same Match battle scenario: steady-state GC Alloc per frame must be **0 B** (or document the irreducible remainder with its call stack); no new frame-time regressions (battle frames must stay ≤ the current ~28 ms worst case in editor).
4. Visual sanity in Play Mode: impostors, minimap, attack traces, HUD all render as before.

## Definition of done

- Profiler steady-state frame shows 0 B GC Alloc (or a documented, justified remainder < 1 KB).
- Allocation-spike frames eliminated or attributed to one-time events (match start), not recurring gameplay.
- Report in `Design/AgentReports/` with: before/after GC per frame, the site-by-site list (bytes → 0), test results, and capture screenshots.

## Out of scope

- Pathfinding files (already optimized 2026-06-11; see prerequisite report).
- The 92 pre-existing EditMode test failures (config drift, UI prefab drift) — separate task.
- Burst-converting SimulationSystemGroup systems — separate task; here only remove managed allocations, don't re-architect.

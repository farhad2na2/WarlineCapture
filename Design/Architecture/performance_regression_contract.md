# Performance Regression Contract

This contract defines how WarlineCapture prevents new gameplay, UI, and shell code from introducing avoidable performance regressions.

Use `Design/AAA_Mobile_Technical_Targets.md` for the product-level device-tier, frame, scale, marker, readability, and evidence targets that this contract validates.

## Core Rule

Performance diagnostics are not the same as performance gates.

`FreezeDetect`, frame-gap logs, and per-system timing logs are useful for finding a regression after it appears. They do not by themselves prevent regressions. New performance-sensitive work must be covered by structured metrics, focused scenarios, and explicit budgets.

## Required Coverage

Performance-sensitive changes must identify which flow they affect and run the focused validation for that flow.

Priority flows:

- Boot to main menu.
- Public M01 launch.
- M01 select and move.
- M01 attack and result flow.
- Tactical steady-state simulation soak.
- Any domain-specific stress case changed by the work, such as pathfinding, rendering budget, spawning, AI production, or UI route transition.

## Metrics

Performance validation should collect structured metrics instead of relying on console text.

Required metric families:

- Frame time: average, p95, p99, and max after warmup.
- GC allocation: total, per-frame p95, and any recurring allocation after warmup.
- System timing: p95, p99, and max for named hot systems.
- Entity/runtime counts: units, buildings, projectiles, visible presentation objects, markers, and relevant UI objects.
- Scenario phase markers: warmup, interaction, combat, completion, and steady state.

## Budgets

Budgets must be scenario-specific and platform-aware.

- Editor PlayMode budgets catch large regressions only.
- Android device development builds are the primary mobile-performance gate.
- Android release builds are the acceptance gate for milestone performance.
- Headless or `-nographics` Unity runs may validate logic and rough timing, but they are not rendering-performance acceptance.

Use warmup windows and percentile thresholds. Do not fail a test only because of one expected startup/import spike.

## FreezeDetect Role

FreezeDetect and system timing logs remain useful, but their role is diagnostic.

Allowed:

- Emit slow-frame details after a budget failure.
- Record the last expensive systems in structured samples.
- Write focused performance reports under `Design/AgentReports` when a regression is found.

Not allowed:

- Treat absence of FreezeDetect logs as proof of performance health.
- Add per-frame string formatting or logging in hot paths.
- Add broad console spam as the only performance measurement.

## Hot-Path Code Rules

New runtime hot-path code must avoid:

- Runtime `Find*`, `GameObject.Find`, `Camera.main`, hierarchy path traversal, or broad scene searches.
- Per-frame LINQ in gameplay/runtime systems.
- Per-frame allocations after warmup.
- Per-frame string interpolation or log construction.
- Runtime asset loading during gameplay frames.
- Instantiate/destroy churn during steady-state gameplay outside approved pooling/presentation paths.
- Static service calls for diagnostics or logging from gameplay systems.
- Creating ECS type handles during runtime ticks. `state.GetEntityTypeHandle`, `state.GetComponentTypeHandle`, `state.GetBufferTypeHandle`, and `state.GetSharedComponentTypeHandle` belong in `OnCreate` or a one-time initialization helper; `OnUpdate` and helpers called by `OnUpdate` must refresh cached handles with `_handle.Update(ref state)`.

## ECS System Timing

Known hot ECS systems should expose profiler markers or structured samples when touched.

Examples:

- `UnitPathfindingSystem`
- `UnitGridMovementSystem`
- `UnitRenderBudgetSystem`
- `UnitModelSpawnSystem`
- `InitialUnitsSpawnSystem`
- `AIProductionSystem`
- `AISquadSystem`
- `AITargetingSystem`

When changing a hot system, validation should show that the changed scenario stays within budget or document the blocker.

## Pathfinding Performance Scenario

`UnitPathfindingSystem` and its extracted pathfinding boundaries are hot-path gameplay code. Refactors in this area must preserve the current request budgets, scheduling semantics, allocator lifetimes, traversal costs, and job data layout unless a focused performance report explicitly approves the change.

Focused pathfinding validation should cover:

- Manual group move with enough selected units to exercise request batching.
- Long-distance move that exercises segmentation and hierarchical waypoint planning.
- Mixed infantry and vehicle-like footprint requests when the touched code affects placement or traversal policy.
- Steady-state frames after warmup, excluding scene load/import spikes.

Required pathfinding metrics:

- Frame time average, p95, p99, and max after warmup.
- `UnitPathfindingSystem`, `UnitPathfindingScheduleSystem`, and `UnitPathfindingApplySystem` p95, p99, and max timing when available.
- Request count, adaptive request budget, pending job wall time, completed/retried/abandoned counts, and long-distance/segmented request counts when diagnostics are enabled for validation.
- GC allocation after warmup.

Pathfinding hot-path code must not add direct `Debug.Log*`, per-frame string formatting, LINQ, scene searches, or new mutable static runtime state. Diagnostic messages must go through the ECS diagnostic event boundary and remain disabled unless the validation/debug configuration explicitly enables them.

## Unit Render Budget Performance Scenario

`UnitRenderBudgetSystem` and its extracted render-budget boundaries are hot gameplay/rendering code. Refactors in this area must preserve the current LOD budget caps, update cadence, camera motion thresholds, visible-character detailed-model policy, enemy impostor thresholds, render bounds patching, culling tag semantics, `EntityCommandBuffer` playback order, allocator lifetimes, and query membership unless a focused performance report explicitly approves the change.

Focused render-budget validation should cover:

- Main Game scene after pressing Play with player and enemy units visible.
- Tactical camera pan/zoom while units are visible, so camera-motion update cadence is exercised.
- High-camera view where visible characters must still use the detailed model path and high-camera impostor helpers stay covered by tests.
- Steady-state frames after warmup, excluding scene load/import spikes.

Required render-budget metrics:

- Frame time average, p95, p99, and max after warmup.
- `UnitRenderBudgetSystem` and extracted render-budget boundary timings when available.
- Unit count, visible-character count, visual transition count, far-impostor count, and structural visibility change count when diagnostics are enabled for validation.
- GC allocation after warmup.

Render-budget hot-path code must not add direct ungated `Debug.Log*`, per-frame string formatting, LINQ, scene searches, runtime asset loading, reflection, or new mutable static runtime state. Diagnostic messages must stay gated before string construction and flow through the ECS diagnostic/logging boundary.

## Initial Units Spawn Performance Scenario

`InitialUnitsSpawnSystem` and its extracted initial-spawn boundaries are startup-critical gameplay code. Refactors in this area must preserve initial spawn batch size, blocker batch size, diagnostic cadence, random-state order, building request order, initial resource projection, Custom Game source-key skip behavior, M01 compact roster behavior, air-platform spawn policy, footprint reservation semantics, fail-open timing, and `EntityCommandBuffer` playback order unless a focused performance report explicitly approves the change.

Focused initial-spawn validation should cover:

- Main Game scene after pressing Play until the loading gate clears.
- Custom Game startup with unresolved source-key unit entries and converted prefab-backed unit entries.
- Initial faction bases/configured buildings enabled, including building request completion and base-core camera focus.
- Air units with helipad/airport platform spawn-point read models.
- Steady-state frames after initial spawn completes, excluding scene load/import spikes.

Required initial-spawn metrics:

- Loading-gate duration and frame count until `InitialUnitsSpawnInitialized`.
- `InitialUnitsSpawnSystem` and extracted initial-spawn boundary p95, p99, and max timing during startup when available.
- Spawned unit count, blocker count, initial building request count, pending request count, and fail-open count.
- GC allocation during active initial spawning and after warmup.

Initial-spawn startup code must not add direct ungated `Debug.Log*`, per-frame string formatting, LINQ, scene searches, runtime asset loading, reflection, or new mutable static runtime state. Diagnostic messages must stay gated before string construction and migrate to an ECS diagnostic/logging boundary during the refactor.

## Regression Workflow

1. Establish or reuse a focused scenario.
2. Warm up the scene.
3. Run fixed-seed gameplay interactions.
4. Capture structured frame/system/GC metrics.
5. Assert against the current budget.
6. If a budget fails, write the offender, scenario, system sample, likely owner, and next fix path in the handoff report.

## Ratchet Policy

Initial budgets may be lenient while baselines are collected. Once a budget is stable, tighten it gradually. Do not loosen a budget without a report explaining the product tradeoff and owner approval.

## Refactor Direction

Move current ad hoc logs toward:

- `PerformanceBudgetConfig`
- `PerformanceMetricsRecorder`
- `PerformanceSample`
- `PerformanceBudgetValidator`
- `PerformanceReportWriter`

The target is testable structured performance evidence, with FreezeDetect as supporting diagnostics.

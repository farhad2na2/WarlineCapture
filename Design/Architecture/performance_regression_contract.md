# Performance Regression Contract

This contract defines how WarlineCapture prevents new gameplay, UI, and shell code from introducing avoidable performance regressions.

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

## ECS System Timing

Known hot ECS systems should expose profiler markers or structured samples when touched.

Examples:

- `UnitPathfindingSystem`
- `UnitGridMovementSystem`
- `UnitRenderBudgetSystem`
- `UnitModelSpawnSystem`
- `MissionRuntimeSpriteRendererSystem`
- `MissionRuntimeTerrainSurfaceRendererSystem`
- `InitialUnitsSpawnSystem`
- `AIProductionSystem`
- `AISquadSystem`
- `AITargetingSystem`

When changing a hot system, validation should show that the changed scenario stays within budget or document the blocker.

## Regression Workflow

1. Establish or reuse a focused scenario.
2. Warm up the scene.
3. Run deterministic gameplay interactions.
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

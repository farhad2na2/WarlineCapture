# Pathfinding Diagnostics Allocation Removal

Date: 2026-07-12
Baseline: `17846ff929`
Tracker task: `APH-710`

## Change

Disabled move-command diagnostics no longer construct interpolated trace strings while scheduling or applying paths. `UnitPathfindingScheduler` and `UnitPathResultApply` now check the existing `EnableMoveCommandTrace` flag before resolving entity descriptions or formatting trace text. Path scheduling, result application, diagnostics behavior when enabled, ECS ownership, and job ordering are unchanged.

## Result

- Focused manual infantry, vehicle, and mixed-group pathfinding passed `3/3` with zero measured current-thread allocation after warmup.
- The 300-frame Match capture reported `72,944` bytes, down from the preceding accepted `122,248` bytes.
- All `UnitPathfindingScheduler.Schedule` and `UnitPathResultApply.Apply` allocation rows are absent from the candidate capture.
- The recurring `14,352`-byte `UIShellEcsPresentationSystem.Update` row remains exactly 48 bytes across 299 frames while the in-method runtime probe reports zero bytes across 300 updates. Removing its profiler marker, removing the observer probe, and moving the tick to another `Update` owner did not remove the row. It is Unity Editor invocation/call-stack capture overhead, not a runtime UI allocation, so no UI production behavior was changed.

The unchanged global `1,024`-byte capture gate remains red. The next largest actionable rows are one-frame building source-key normalization and intermittent selection read-model refreshes.

## Validation

- `UnitPathfindingFocusedPerformanceValidation`: passed `3/3`; max allocated bytes `0`.
- `ProductionSourceGrowthArchitectureValidation`: passed `15/15`.
- `ScriptArchitectureBoundaryValidation`: passed `31/31`.
- `EcsBurstHotPathArchitectureValidation`: passed `10/10`.
- Full Match capture completed with zero compiler errors.
- `git diff --check`: passed.

Evidence logs:

- `/private/tmp/warline-aph710-path-diagnostics-allocation-focused.log`
- `/private/tmp/warline-aph710-path-diagnostics-match-gc.log`
- `/private/tmp/warline-aph710-path-diagnostics-source-growth.log`
- `/private/tmp/warline-aph710-path-diagnostics-boundary.log`
- `/private/tmp/warline-aph710-path-diagnostics-ecs-burst.log`
- `/private/tmp/warline-aph710-ui-shell-tick-match-gc.log`
- `/private/tmp/warline-aph710-ui-shell-marker-match-gc.log`
- `/private/tmp/warline-aph710-ui-shell-observer-free-match-gc.log`

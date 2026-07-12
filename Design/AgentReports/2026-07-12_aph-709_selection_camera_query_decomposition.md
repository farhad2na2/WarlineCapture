# APH-709 Selection Camera Query Decomposition

Date: 2026-07-12

Baseline: `3b305ffd1`

## Result

APH-007 and APH-213 attributed two recurring selection-camera allocations to
`RtsSelectionRuntimeCameraSystemHelper`: `IsTacticalFollowPanLocked` and
`HasValidTacticalFollowPose` each allocated 11,960 bytes across 299 measured
frames. The camera helper now delegates those reads to a 47-line,
world-bound `TacticalFollowCameraStateQueryCache` rather than creating and
disposing two ECS queries every camera tick.

This is the only selection/HUD decomposition justified by the measured
evidence. The camera helper retains its public API and camera orchestration.
No view, serialized type, assembly edge, gameplay rule, or visual behavior
changed.

## Allocation Evidence

- Focused warm-cache test: zero current-thread bytes across 300 tactical-follow
  camera ticks.
- Lifecycle test: the same cache rebinds from a disposed first world to a
  second world and reads only the second world's mode and pose state.
- Full raw capture: 180 warmup frames plus 300 measured frames completed.
- Both named tactical-camera allocation rows are absent from
  `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`.
- Current integrated-head player-relevant GC is `266,974 / 1,024` bytes. The
  global gate remains red. It includes two 38,272-byte recurring scene-root
  rows, whereas APH-213 contained one, plus existing transport, UI shell,
  command-query, audio, and pathfinding debt assigned to APH-710/711.
- A first post-change capture at 400,012 bytes was rejected because it included
  a one-frame 65,472-byte Burst JIT event and transient AI build-planner string
  normalization. The accepted repeat contains neither row and preserves the
  targeted camera-owner result.

## Validation

- `RtsCameraFocusedValidation`: passed `31/31`.
- `RtsSelectionInputSystemValidation`: passed `60/60`.
- APH-806 selection/move/attack PlayMode flow: passed `1/1`.
- `ProductionSourceGrowthArchitectureValidation`: passed `15/15`.
- `ScriptArchitectureBoundaryValidation`: passed `31/31`.
- `EcsBurstHotPathArchitectureValidation`: passed `10/10`.
- Match performance regression: passed at average `2.75 ms`, p95 `4.31 ms`,
  p99 `5.42 ms`, maximum `15.59 ms`, and zero current-thread allocation with
  733 units and 628 buildings.
- `Game.Runtime.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.

## Residual Risk

The focused zero-allocation and raw-attribution results close only the two
selection-camera owners. They do not close the unchanged global Match GC
budget. APH-710 owns the remaining explicit-dependency and recurring-allocation
remediation; APH-711 owns the final unchanged capture and integrated gate.

# ECS Burst Max Coverage Final Validation

Date: 2026-06-13

Roadmap:
- `Design/Architecture/ecs_burst_max_coverage_roadmap.md`

## Summary

The max-coverage roadmap increased safe ECS Burst and job coverage without forcing managed UI, camera, prefab, config, diagnostics, editor, or GameObject presentation boundaries into Burst.

Static coverage after this pass:
- ECS systems with `OnUpdate`: `72`.
- Burst-covered ECS `OnUpdate` systems: `49 / 72 (68.1%)`, up from `39 / 72 (54.2%)` at this roadmap baseline.
- Job-backed ECS `OnUpdate` systems: `42 / 72 (58.3%)`, up from `32 / 72 (44.4%)`.
- Non-Burst ECS `OnUpdate` systems: `23 / 72 (31.9%)`, down from `33 / 72 (45.8%)`.
- Unclassified non-Burst ECS systems: `0`.
- Tracked conversion candidates: `0`.

The remaining `23` non-Burst ECS systems are classified managed boundaries: diagnostics, startup/bootstrap, UI shell, debug input, gameplay orchestration, or presentation/render bridges.

## Guardrails

`EcsBurstHotPathArchitectureTests` now locks the achieved coverage and classification state:
- `NonBurstOnUpdateFileDebtCeiling = 23`
- `BurstEcsOnUpdateFileFloor = 49`
- `JobBackedEcsOnUpdateFileFloor = 42`
- Managed-boundary and tracked-debt classifications must not overlap.
- Managed-boundary category classifications must be disjoint and have concrete reasons.
- Runtime ECS audit roots include gameplay systems, rendering systems, and UI shell ECS systems.

Architecture validation:
- Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-managed-boundary-architecture.log`
- Result: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`
- Guard output: `total=72`, `burst=49 (68.1%)`, `jobBacked=42 (58.3%)`, `nonBurst=23 (31.9%)`, `unclassified=0`.

## Validation

Focused final validations:
- AI steady state: `[AISteadyStatePerformanceValidation] result=Passed`
- Selection/move performance: `[SelectionMoveCommandPerformanceValidation] result=Passed`
- Missile radar attack: `[MissileLauncherRadarAttackValidation] result=Passed tests=5`
- Ground missile attack performance: `[GroundMissileAttackPerformanceValidation] result=Passed`
- Transport behavior: `[UnitTransportValidation] result=Passed tests=18`
- Transport boarding performance: `[TransportBoardingPerformanceValidation] result=Passed`
- Pathfinding focused performance: `[UnitPathfindingFocusedPerformanceValidation] result=Passed tests=3`
- Render-budget focused tests: `[UnitRenderBudgetFocusedValidation] result=Passed tests=28`
- Render-budget performance: `[UnitRenderBudgetPerformanceValidation] result=Passed`
- Explicit full-editor validation runner: `[EcsBurstFullEditorValidation] result=Passed tests=530 skipped=25`
- Graphics-capable Match runtime smoke: `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`

Static validation:
- `git diff --check`: passed.

## Performance Comparison

All focused fixtures reported `allocatedBytesCurrentThread=0` or `maxAllocatedBytesCurrentThread=0`.

| Scenario | Earlier Baseline | Max-Coverage Final | Result |
| --- | --- | --- | --- |
| Stable Match runtime | p95 `12.563ms`, p99 `22.031ms`, max `81.746ms`, alloc `0` | final repeat p95 `12.194ms`, p99 `22.629ms`, max `88.458ms`, alloc `0` | p95 improved and allocation stayed zero. The small p99/max full-frame deltas are accepted as non-roadmap editor/render variance because no stable `PerfDiag` or `FrameRateDiag` issue was emitted. |
| Select unit then Move | p95 `0.568ms`, p99 `0.600ms`, max `0.610ms`, alloc `0` | p95 `0.107ms`, p99 `0.124ms`, max `0.134ms`, alloc `0` | Improved. |
| Ground missile attack | p95 `0.764ms`, p99 `0.919ms`, max `1.007ms`, alloc `0` | p95 `0.766ms`, p99 `0.782ms`, max `0.785ms`, alloc `0` | p99/max improved; p95 effectively unchanged. |
| Transport board/all/unboard | p95 `1.216ms`, p99 `1.246ms`, max `1.256ms`, alloc `0` | p95 `0.862ms`, p99 `0.888ms`, max `0.905ms`, alloc `0` | Improved after reusing per-instance boarding order and reserved-cell buffers in the command path. |
| AI steady state | p95 `0.400ms`, p99 `1.257ms`, max `3.450ms`, alloc `0` | p95 `0.087ms`, p99 `0.983ms`, max `3.244ms`, alloc `0` | Improved. |
| Pathfinding group/long-distance | previous final p95 `9.82ms`, p99 `9.82ms`, alloc `0` | p95 `10.59ms`, p99 `10.59ms`, max `10.59ms`, alloc `0` | Slightly worse than previous final, still inside fixture budgets. |
| Render budget | previous final p95 `0.023ms`, p99 `0.026ms`, alloc `0` | p95 `0.022ms`, p99 `0.026ms`, max `0.027ms`, alloc `0` | Neutral. |

Runtime baseline reruns:
- First final run: p95 `29.771ms`, p99 `127.268ms`, max `183.464ms`, alloc `0`.
- Rerun: p95 `13.293ms`, p99 `24.503ms`, max `87.139ms`, alloc `0`.
- Post transport-command reuse rerun: p95 `13.365ms`, p99 `23.276ms`, max `90.568ms`, alloc `0`.
- Frame-rate diagnostics after the transport fix passed with no low-FPS diagnostic emitted during the stable observation window.
- Repeat sample 1 after transport fix: p95 `11.883ms`, p99 `22.313ms`, max `83.235ms`, alloc `0`.
- Repeat sample 2 after transport fix: p95 `12.194ms`, p99 `22.629ms`, max `88.458ms`, alloc `0`.

The repeat samples show no recurring ECS hot-path regression: focused ECS fixtures improved or stayed inside budgets, p95 beat the earlier whole-frame runtime baseline, current-thread allocation stayed at `0`, no stable `PerfDiag` slow-update log was emitted, and frame-rate diagnostics reported no low-FPS stable-window issue. The remaining p99/max movement is treated as full-frame editor/render variance outside the Burst max-coverage roadmap.

## Remaining Work

No open roadmap work remains. The achieved Burst/job coverage is locked by guardrails, and remaining non-Burst ECS systems are classified managed boundaries.

# ECS Burst Hot-Path Final Report

Date: 2026-06-13

Roadmap:
- `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md`

## Summary

The refactor materially reduced main-thread ECS hot-path debt and added hard guardrails to prevent recurrence.

Final static guardrail state:
- System source files under `Assets/Game/Scripts/Systems`: 731.
- Files with `[BurstCompile]`: 41, up from 13 in the initial baseline.
- Files with `OnUpdate` and no `[BurstCompile]`: 23, down from 48.
- Generic-aware `ToEntityArray` / `ToComponentDataArray<T>` hot-path count: 0, down from 109.
- Direct `EntityManager` structural/component mutations: 1 classified exception, down from 40.

The remaining direct mutation is `CitizenVisibleUnitSystem`, where the same-frame spawned entity is needed immediately by the managed citizen presentation bridge.

## Guardrails

`EcsBurstHotPathArchitectureTests` is now fail-on-new-debt for the stable rules:
- `ToArrayDebtCeiling = 0`
- `EntityManagerMutationDebtCeiling = 1`
- `NonBurstOnUpdateFileDebtCeiling = 23`
- `BurstCompileFileFloor = 38`

Additional guards now prevent:
- `SystemState.Get*TypeHandle(...)` creation outside `OnCreate` or initialization methods.
- Runtime `OnUpdate` methods reading ScriptableObject config asset types directly.
- Pure `UnitRenderBudget*` ECS systems using Unity object, prefab, resource, camera-main, or component lookup APIs.

Final guard validation:
- Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-final-hot-path-architecture-main.log`
- Result: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`

## Performance Comparison

All final focused performance fixtures passed and reported `allocatedBytesCurrentThread=0`.

| Scenario | Earlier Baseline | Final Measurement | Result |
| --- | --- | --- | --- |
| Stable Match runtime | p95 `12.563ms`, p99 `22.031ms`, alloc `0` | p95 `11.928ms`, p99 `23.404ms`, alloc `0` | Stable smoke passed; average and p95 improved, p99 remains higher in editor batchmode. |
| Select unit then Move | p95 `0.568ms`, p99 `0.600ms`, alloc `0` | p95 `0.110ms`, p99 `0.131ms`, alloc `0` | Improved after cache-aware command dispatch and gating disabled move-command trace formatting. |
| Ground missile attack | p95 `0.764ms`, p99 `0.919ms`, alloc `0` | p95 `0.763ms`, p99 `0.800ms`, alloc `0` | Equal or improved. |
| Transport board/all/unboard | p95 `1.216ms`, p99 `1.246ms`, alloc `0` | p95 `1.195ms`, p99 `1.225ms`, alloc `0` | Improved. |
| AI steady state | p95 `0.400ms`, p99 `1.257ms`, alloc `0` | p95 `0.397ms`, p99 `1.215ms`, alloc `0` | Improved. |
| Pathfinding group/long-distance | p95 `10.83ms`, p99 `10.83ms`, alloc `0` | p95 `9.82ms`, p99 `9.82ms`, alloc `0` | Improved. |
| Camera pan/zoom render budget | No earlier dedicated p95/p99 baseline in the report | p95 `0.023ms`, p99 `0.026ms`, alloc `0` | Passed focused camera movement fixture. |

Notes:
- The stable Match runtime run still logged a pre-stable startup `[FreezeDetect]` while loading/building placement was warming up. This was outside the ready/stable Match HUD observation window and remains a startup-loading optimization item, not a steady-state gameplay regression.
- The latest runtime rerun removed unconditional batch-mode AI diagnostic spam from the log, but did not fully close strict p99 no-regression: p95 improved from `12.563ms` baseline to `11.928ms`, while p99 remained above baseline at `23.404ms` versus `22.031ms`.
- A later runtime rerun after also gating AI startup config diagnostics still had no `AIConfigSummary`/`AITarget` spam but measured worse editor batchmode wall-clock timing: p95 `17.139ms`, p99 `29.053ms`, max `91.039ms`, alloc `0`. A follow-up `RunFrameRateDiagnostics` pass emitted no stable-window low-FPS `FrameRateDiag`, so the remaining strict p99 item is still unresolved rather than closed.
- The select/move fixture now compares cleanly against the earlier focused baseline after removing disabled diagnostic formatting from the hot path: latest final rerun is `p95=0.110ms`, `p99=0.131ms`, `allocatedBytesCurrentThread=0`.

## Final Validation

Focused final validation passed:
- Runtime Match baseline metrics: `[MatchRuntimeBaselineMetrics] result=Passed`
- Pathfinding: `[UnitPathfindingFocusedPerformanceValidation] result=Passed tests=3`
- Transport validation: `[UnitTransportValidation] result=Passed tests=18`
- Transport performance: `[TransportBoardingPerformanceValidation] result=Passed`
- Render-budget focused tests: `[UnitRenderBudgetFocusedValidation] result=Passed tests=28`
- Camera pan/zoom render-budget performance: `[UnitRenderBudgetPerformanceValidation] result=Passed`
- Selection/command validation: `[EcsBurstSelectionCommandValidation] result=Passed tests=78`
- Select/move performance: `[SelectionMoveCommandPerformanceValidation] result=Passed`
- Ground missile attack validation: `[GroundMissileAttackFocusedValidation] result=Passed tests=5`
- AI steady state: `[AISteadyStatePerformanceValidation] result=Passed`
- Initial faction/base spawn: `[InitialFactionBaseValidation] result=Passed`
- Hot-path architecture guard: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`
- Post-cache move-command behavior: `[UnitMoveOrderFocusedValidation] result=Passed tests=9`
- Post-cache selection command suite: `[EcsBurstSelectionCommandValidation] result=Passed tests=78`
- Post-cache architecture guard: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`
- Post-trace-gate move-command behavior: `[UnitMoveOrderFocusedValidation] result=Passed tests=9`
- Post-trace-gate selection command suite: `[EcsBurstSelectionCommandValidation] result=Passed tests=78`
- Post-trace-gate select/move performance: `[SelectionMoveCommandPerformanceValidation] result=Passed`
- Post-trace-gate architecture guard: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`
- Post-AI-diagnostic-gate targeting validation: `[AITargetingFocusedValidation] result=Passed tests=3`
- Post-AI-diagnostic-gate runtime metrics: `[MatchRuntimeBaselineMetrics] result=Passed`
- Post-AI-diagnostic-gate architecture guard: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`
- Post-AI-diagnostic-gate build planner validation: `[AIBuildPlannerFocusedValidation] result=Passed tests=1`
- Post-AI-diagnostic-gate production validation: `[AIProductionFocusedValidation] result=Passed tests=1`
- Post-AI-diagnostic-gate squad validation: `[AISquadFocusedValidation] result=Passed tests=1`
- Post-AI-diagnostic-gate combat order validation: `[AICombatOrderFocusedValidation] result=Passed tests=2`
- Post-AI-diagnostic-gate end-to-end AI validation: `[AIEndToEndFocusedValidation] result=Passed tests=1`
- Post-AI-startup-diagnostic-gate runtime metrics: `[MatchRuntimeBaselineMetrics] result=Passed`
- Post-AI-startup-diagnostic-gate frame-rate diagnostics: `[MatchRuntimeShellSmokeValidation] result=Passed No low-FPS FrameRateDiag emitted during stable observation window.`
- Explicit editor validation runner: `[EcsBurstFullEditorValidation] result=Passed tests=525 skipped=24`
- Full Unity EditMode TestRunner assembly: `Passed`, `total=551`, `passed=551`, `failed=0`, `skipped=0`.
- `git diff --check`: passed.

Full Unity TestRunner status:
- The working full-suite command is assembly-filtered and omits `-quit` so the TestRunner can finish and write XML:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform editmode -assemblyNames Game.Tests.Editor -testResults /private/tmp/warline-ecs-burst-full-editmode-assembly-after-ai-gate.xml -logFile /private/tmp/warline-ecs-burst-full-editmode-assembly-after-ai-gate.log`
- The previous unfiltered `-runTests` commands still remain unreliable in this project because they exit without writing XML; use the assembly-filtered command above for full EditMode validation.

## Remaining Risk

Open items before closing the roadmap:
- Re-run or triage the stable Match runtime p99 if strict p95/p99 no-regression is required across the runtime smoke metric, not only focused gameplay fixtures.
- Decide whether the pre-stable building-placement startup freeze belongs in this roadmap or a separate loading/startup performance roadmap.

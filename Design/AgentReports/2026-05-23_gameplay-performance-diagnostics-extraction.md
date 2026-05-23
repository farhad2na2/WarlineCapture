# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Refactor bootstrap step 1: extract performance diagnostics from `GameBootstrap` into a dedicated boundary.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs`
- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-performance-diagnostics-extraction.md`

## Contracts touched
- Performance diagnostics are now owned by `PerformanceDiagnosticsSystem`.
- `GameBootstrap` may bracket lifecycle work through the diagnostics system, but it must not format or emit `FreezeDetect`, `FrameRateDiag`, or `PerfDiag` diagnostics directly.
- Architecture tests now reject profiler recorder state and diagnostic log formatting drifting back into `GameBootstrap`.

## User-visible behavior
No intended behavior change. Existing performance diagnostics keep the same log categories and message content:
- `FreezeDetect`
- `FrameRateDiag`
- `FrameRateDiag:PreGame`
- `PerfDiag`
- `PerfDiag:PreGame`

Follow-up hot-path fix after runtime FPS regression report:
- Removed delegate/action wrappers from per-frame `Update`, `LateUpdate`, and `OnGUI` diagnostics.
- `GameBootstrap` now calls runtime systems directly and only passes start/end timestamps to `PerformanceDiagnosticsSystem`.
- This keeps diagnostics ownership extracted while avoiding per-frame closure/delegate churn from the extraction boundary.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity PlayMode `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity PlayMode `BootstrapAndMenuPlayModeTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Re-run after hot-path fix: `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- Re-run after hot-path fix: Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Re-run after hot-path fix: Unity PlayMode `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

## Validation result
- `git diff --check`: passed
- `GameplayArchitectureContractTests`: passed, 64/64
- `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest`: passed, 1/1
- `BootstrapAndMenuPlayModeTests`: compiled and ran, 6/7 passed; failed on `MenuView_SelectAllSoldiersButton_CapturesUiClickAndSelectsVisibleSoldiers` because `RTSSelectionSystem` no longer exposes private field `_ignoreUiClickUntilRelease` for the test reflection assertion.
- Re-run after hot-path fix: `git diff --check` passed
- Re-run after hot-path fix: `GameplayArchitectureContractTests` passed, 64/64
- Re-run after hot-path fix: `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest` passed, 1/1

## Known gaps
- `GameBootstrap` still owns the managed runtime update list and feature installation wiring.
- Full `BootstrapAndMenuPlayModeTests` has an unrelated selection private-field reflection failure that should be addressed separately by updating the test to assert public behavior instead of private field shape.

## Cross-lane impacts
- No scene, UI prefab, art, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for Unity test execution.

## Next recommended task
Extract the managed runtime update loop into a dedicated `GameplayRuntimeUpdateSystem`, leaving `GameBootstrap` with only lifecycle calls into that system.

# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Refactor bootstrap step 2: attempted managed gameplay runtime update loop extraction, then rolled it back after runtime FPS stayed near 30 instead of the previous 60 FPS target.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-runtime-update-loop-extraction.md`

## Contracts touched
- Managed gameplay runtime update extraction is paused until a focused FPS regression capture/contract exists.
- `GameBootstrap` temporarily keeps the existing managed runtime update loop as legacy debt to preserve the 60 FPS target.
- Architecture tests now reject restoring `GameplayRuntimeUpdateSystem` without a performance contract.
- Performance diagnostics remain delegated to `PerformanceDiagnosticsSystem`.

## User-visible behavior
No intended behavior change. Runtime update order is preserved:
- menu input sync
- mission elapsed/runtime update
- road build, building placement, and selection update
- M01 camera/framing policy update
- runtime city/blocker/decoration/day-night/population update
- menu sync and gameplay-ready notification
- match-result completion check
- late update and OnGUI forwarding

Follow-up after runtime FPS regression report:
- The `GameplayRuntimeUpdateSystem` extraction was removed from the hot path.
- `GameBootstrap.Update`, `LateUpdate`, and `OnGUI` were restored to the previous direct managed runtime loop shape.
- `PerformanceDiagnosticsSystem` keeps the useful throttling for repeated `FreezeDetect` gap/hitch logs and avoids formatting every sub-1ms step into the per-frame detailed step string.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md Design/AgentReports/2026-05-23_gameplay-runtime-update-loop-extraction.md`
- `rg -n "GameplayRuntimeUpdateSystem" Assets/Game/Scripts`
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity PlayMode `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

## Validation result
- `git diff --check`: passed
- Local source check: no `GameplayRuntimeUpdateSystem` source remains under `Assets/Game/Scripts`
- Unity EditMode `GameplayArchitectureContractTests`: passed, 65/65 after rollback
- Unity PlayMode `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest`: passed, 1/1 after rollback

## Known gaps
- `GameBootstrap` still owns gameplay feature creation and `EnsureGameplaySystemsInitialized`.
- `GameBootstrap` also temporarily owns the managed runtime update loop again as explicit performance debt.
- Latest Editor diagnostics before rollback showed steady-state frame rate around 26-28 FPS with frame time dominated outside gameplay update: `Gfx.WaitForGfxCommandsFromMainThread`/`Semaphore.WaitForSignal` and about 9M triangles. The extraction rollback is intended to restore the exact prior hot-path shape before deeper render-side profiling.

## Cross-lane impacts
- No scene, UI prefab, art, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for Unity test execution.

## Next recommended task
Recheck runtime FPS in the main Unity editor after this rollback. If it returns to 60 FPS, keep the managed runtime loop in `GameBootstrap` until a focused FPS regression contract exists; if it remains at 30 FPS, profile render cost and the `BuildingPlacement`/`Selection` spikes next.

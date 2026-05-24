# WarlineCapture UI Handoff

## Lane

UI

## Task

Implement `GameUI` Step 4 ECS shell boundary data and command sequencing.

## Files changed

- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellComponents.cs.meta`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs.meta`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs.meta`
- `Design/AgentReports/2026-05-24_ui_gameui-scene-step4.md`

## Contracts touched

- `Design/Architecture/ui_runtime_shell_gameui_scene_implementation_plan.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

## User-visible behavior

No direct user-visible UI changes yet.

The ECS shell boundary now defines:

- Shell state, loading progress, route request, popup request, mission result, presentation command, and transition-complete components.
- `UiShellBoundarySystem`, which creates the shell boundary entity and required buffers.
- `UiShellFlowSystem`, which emits presentation commands for startup loading, loading completion to main menu, menu route swaps, match entry, return to main menu, and popup show/hide.

## Validation run

Unity batchmode compile/scene validation in the validation workspace:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.ValidateStep3 -logFile /private/tmp/warlinecapture-gameui-step4-compile-unity2.log`

## Validation result

Passed.

Log confirmed:

- No `error CS` compile failures.
- `WARLINECAPTURE_GAMEUI_SCENE_STEP3_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`

## Known gaps

- The Unity bridge does not consume presentation commands yet.
- No shell command smoke driver exists yet.
- Transition completion events are expected from Step 5 bridge code.
- Main project Unity instance was already open, so validation ran in `WarlineCapture-CodexUnity2`.

## Cross-lane impacts

None. Existing gameplay scene, legacy UI scene objects, and legacy router/controller code were not modified.

## Next recommended task

Proceed to Step 5: add `WarlineCaptureShellView` and `WarlineCaptureShellEcsBridgeView` so Unity can consume ECS presentation commands and write transition completion events.

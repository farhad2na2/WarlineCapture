# WarlineCapture UI Handoff

## Lane

UI

## Task

Implement `GameUI` Step 5 by adding the Unity shell view and ECS bridge view.

## Files changed

- `Assets/Game/Scenes/GameUI.unity`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellView.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellView.cs.meta`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellEcsBridgeView.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureShellEcsBridgeView.cs.meta`
- `Design/AgentReports/2026-05-24_ui_gameui-scene-step5.md`

Step 4 dependency completed in the same work session:

- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Design/AgentReports/2026-05-24_ui_gameui-scene-step4.md`

## Contracts touched

- `Design/Architecture/ui_runtime_shell_gameui_scene_implementation_plan.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

## User-visible behavior

`Assets/Game/Scenes/GameUI.unity` now binds:

- `WarlineCaptureShellView`
- `WarlineCaptureShellEcsBridgeView`
- Existing `WarlineCaptureUiMotionHostView`
- Seven shell region views

The bridge consumes `UiShellPresentationCommandComponent` buffers from the ECS shell boundary, executes shell-region motion through `WarlineCaptureShellView`, then writes `UiShellTransitionCompleteComponent` events back to ECS.

## Validation run

Unity batchmode in the validation workspace:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep5 -logFile /private/tmp/warlinecapture-gameui-step5-unity2.log`

## Validation result

Passed.

Log confirmed:

- `WARLINECAPTURE_GAMEUI_SCENE_STEP1_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP2_VALIDATED scene=Assets/Game/Scenes/GameUI.unity regions=7`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP3_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP5_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP5_BUILT scene=Assets/Game/Scenes/GameUI.unity`

## Known gaps

- No smoke driver exists yet to push route/loading/popup requests through the shell.
- No region content prefabs are assigned yet.
- Bridge completion is sequence-level; per-region completion detail can be added if a later validation needs it.
- Main project Unity instance was already open, so validation ran in `WarlineCapture-CodexUnity2`.

## Cross-lane impacts

None. Existing gameplay scene, legacy UI scene objects, and legacy router/controller code were not modified.

## Next recommended task

Proceed to Step 6: create initial region content prefabs for loading, main menu, match HUD, and mission result popup, then add a Step 7 smoke driver for the full shell sequence.

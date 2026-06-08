# WarlineCapture UI Handoff

## Lane

UI

## Task

Implement `GameUI` scene Step 2 by adding shell region views and the initial shell region hierarchy.

## Files changed

- `Assets/Game/Scenes/GameUI.unity`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellRegionView.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellRegionView.cs.meta`
- `Design/AgentReports/2026-05-24_ui_gameui-scene-step2.md`

## Contracts touched

- `Design/Architecture/ui_runtime_shell_gameui_scene_implementation_plan.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

## User-visible behavior

`Assets/Game/Scenes/GameUI.unity` now contains the Step 2 runtime shell regions under `WarlineCaptureRuntimeShell`:

- `LoadingLayer`
- `HeaderRegion`
- `LeftRegion`
- `MiddleRegion`
- `RightRegion`
- `FooterRegion`
- `PopupLayer`

Each region has a `CanvasGroup`, a `WarlineCaptureShellRegionView`, and a stretched `ContentRoot` child for future region content.

## Validation run

Unity batchmode in the validation workspace:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep2 -logFile /private/tmp/warlinecapture-gameui-step2-unity2.log`

## Validation result

Passed.

Log confirmed:

- `WARLINECAPTURE_GAMEUI_SCENE_STEP1_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP2_VALIDATED scene=Assets/Game/Scenes/GameUI.unity regions=7`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP2_BUILT scene=Assets/Game/Scenes/GameUI.unity`

The main workspace scene was copied from the validated Unity2 output.

## Known gaps

- Regions are empty; no screen content prefabs are assigned yet.
- Motion/tween host is not implemented yet.
- ECS shell boundary and Unity/ECS bridge are not implemented yet.
- Main project Unity instance was already open, so validation ran in `WarlineCapture-CodexUnity2`.

## Cross-lane impacts

None. Existing gameplay scene, legacy UI scene objects, and legacy router/controller code were not modified.

## Next recommended task

Proceed to Step 3: implement `WarlineCaptureUiMotionHostView` with anchored-position, scale, alpha, sequence, parallel, easing, and transition-id cancellation primitives.

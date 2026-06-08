# WarlineCapture UI Handoff

## Lane

UI

## Task

Implement `GameUI` scene Step 3 by adding the runtime UI motion host.

## Files changed

- `Assets/Game/Scenes/GameUI.unity`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs`
- `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs`
- `Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs.meta`
- `Design/AgentReports/2026-05-24_ui_gameui-scene-step3.md`

## Contracts touched

- `Design/Architecture/ui_runtime_shell_gameui_scene_implementation_plan.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

## User-visible behavior

`Assets/Game/Scenes/GameUI.unity` now has one `WarlineCaptureUiMotionHostView` on `WarlineCaptureRuntimeShell`.

The motion host provides:

- Anchored position tweens.
- Scale tweens.
- CanvasGroup alpha tweens.
- Sequence and parallel motion steps.
- Linear, ease-in cubic, ease-out cubic, ease-in-out cubic, and subtle popup overshoot easing.
- Transition-id cancellation guard.

No content prefabs, ECS bridge, router integration, or gameplay scene changes were added in this step.

## Validation run

Unity batchmode in the validation workspace:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep3 -logFile /private/tmp/warlinecapture-gameui-step3-unity2.log`

## Validation result

Passed.

Log confirmed:

- `WARLINECAPTURE_GAMEUI_SCENE_STEP1_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP2_VALIDATED scene=Assets/Game/Scenes/GameUI.unity regions=7`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP3_VALIDATED scene=Assets/Game/Scenes/GameUI.unity`
- `WARLINECAPTURE_GAMEUI_SCENE_STEP3_BUILT scene=Assets/Game/Scenes/GameUI.unity`

The main workspace scene was copied from the validated Unity2 output.

## Known gaps

- Motion host is implemented and bound, but no shell flow calls it yet.
- No ECS shell boundary exists yet.
- No Unity/ECS bridge exists yet.
- No transition smoke sequence exists yet.
- Main project Unity instance was already open, so validation ran in `WarlineCapture-CodexUnity2`.

## Cross-lane impacts

None. Existing gameplay scene, legacy UI scene objects, and legacy router/controller code were not modified.

## Next recommended task

Proceed to Step 4: add the ECS shell boundary data and `UiShellFlowSystem` command sequencing.

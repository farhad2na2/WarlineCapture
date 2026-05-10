# Lane
Gameplay

# Task
Repair the rejected `Game_Legecy` scene split so `Assets/Game/Scenes/Game.unity` is the clean 2D/isometric production scene and `Assets/Game/Scenes/Game_Legecy.unity` is playable as the old legacy prototype through the legacy `UI_Canvas`.

# Files changed
- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scenes/Game_Legecy.unity`
- `Assets/Game/Scenes/Game_Legecy.unity.meta`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameLegecySceneIsolationBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameLegecySceneIsolationBuilder.cs.meta`
- `Assets/Tests/PlayMode/GameLegecySceneIsolationPlayModeTests.cs`
- `Assets/Tests/PlayMode/GameLegecySceneIsolationPlayModeTests.cs.meta`
- `Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation-fix.md`

# Contracts touched
- `Game.unity` no longer contains the old prototype `UI_Canvas`, `Global Volume`, duplicate legacy directional light, `Main Camera_Experiment`, `Global Volume_Experiment`, `SM_Skydome_01`, `Ground`, or `Decorations` roots.
- `Game.unity` keeps production roots: `RuntimeDecorations_Production`, `Bootstrap`, `WarlineCaptureUIBootstrap`, `Main Camera`, `GameSubScene`, `Chapter01_TacticalMissionRuntime`, and a single `Directional Light`.
- `Game_Legecy.unity` no longer contains production-only roots: `WarlineCaptureUIBootstrap`, `Chapter01_TacticalMissionRuntime`, `RuntimeDecorations_Production`, production `Main Camera`, production `Global Volume`, or production `Directional Light`.
- `Game_Legecy.unity` keeps and activates the legacy playable roots: `UI_Canvas`, `Main Camera_Experiment`, `Global Volume_Experiment`, `Directional Light (1)`, `Decorations`, `Ground`, `SM_Skydome_01`, `Bootstrap`, and `GameSubScene`.
- `Game_Legecy.unity` `GameBootstrap` is wired to the legacy canvas/menu, experiment camera, experiment volume, legacy directional light, and legacy decorations. Its production `Chapter01MissionTacticalRuntimeBinder` reference is cleared.

# User-visible behavior
- Opening `Assets/Game/Scenes/Game.unity` should show only the production 2D/isometric scene setup, with no legacy `UI_Canvas`, no old prototype `Global Volume`, and no duplicate legacy directional light.
- Opening and pressing Play in `Assets/Game/Scenes/Game_Legecy.unity` uses the old legacy `UI_Canvas` path and does not instantiate the public 2D/isometric app router or M01 tactical runtime binder.
- The normal public M01 route through `Game.unity` still reaches the production visible slice.

# Validation run
- Scene builder and scene validation in the unlocked Unity mirror:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureGameLegecySceneIsolationBuilder.Build -logFile /private/tmp/warlinecapture-game-legecy-scene-isolation-fix-build.log`
- Focused legacy PlayMode proof:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter GameLegecySceneIsolationPlayModeTests -testResults /private/tmp/warlinecapture-game-legecy-scene-isolation-playmode-results.xml -logFile /private/tmp/warlinecapture-game-legecy-scene-isolation-playmode.log`
- Existing M01 production PlayMode smoke:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-game-legecy-scene-isolation-m01-playmode-results.xml -logFile /private/tmp/warlinecapture-game-legecy-scene-isolation-m01-playmode.log`
- Static scene root checks in `/Users/farhad/Projects/WarlineCapture`:
  - `Game.unity` contains `RuntimeDecorations_Production`, `Bootstrap`, `WarlineCaptureUIBootstrap`, `Main Camera`, `GameSubScene`, `Chapter01_TacticalMissionRuntime`, and one `Directional Light`.
  - `Game.unity` has no exact root-name matches for `UI_Canvas`, `Global Volume`, `Main Camera_Experiment`, `Global Volume_Experiment`, `SM_Skydome_01`, `Ground`, `Decorations`, or `Directional Light (1)`.
  - `Game_Legecy.unity` contains `Directional Light (1)`, `Bootstrap`, `Global Volume_Experiment`, `Main Camera_Experiment`, `Decorations`, `UI_Canvas`, `GameSubScene`, `Ground`, and `SM_Skydome_01`.
  - `Game_Legecy.unity` has no exact root-name matches for `WarlineCaptureUIBootstrap`, `Chapter01_TacticalMissionRuntime`, `RuntimeDecorations_Production`, production `Main Camera`, production `Global Volume`, or production `Directional Light`.

# Validation result
- Scene builder passed and logged `WARLINECAPTURE_GAME_LEGECY_SCENE_ISOLATION_VALIDATED`.
- `GameLegecySceneIsolationPlayModeTests`: passed `1/1`, `0` failed.
- `Chapter01M01PlayModeValidationTests`: passed `8/8`, `0` failed.
- Non-blocking Unity log noise remained: licensing token update warning, Xcode Info.plist probe warnings, preview-scene shutdown leak warning, and debugger/usbmuxd shutdown warnings.

# Known gaps
- Validation was run from `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because it is the unlocked Unity mirror used for batch validation. The final generated scene files were copied back to `/Users/farhad/Projects/WarlineCapture`.
- `Game_Legecy.unity` intentionally keeps `GameSubScene` because it is part of the legacy playable setup currently needed by the old prototype.

# Cross-lane impacts
- PM/user can validate the scene split directly.
- UI does not need to make a separate launcher change for this task.
- Gameplay should return to the selected-readability rejection gate only after this split is accepted.

# Next recommended task
PM/user should validate the two scenes directly. If accepted, Gameplay should resume the selected-readability rejection gate.

# Direct user validation steps
1. Open `Assets/Game/Scenes/Game.unity`.
2. Confirm the Hierarchy does not include `UI_Canvas`, `Global Volume`, `Main Camera_Experiment`, `Global Volume_Experiment`, `SM_Skydome_01`, `Ground`, `Decorations`, or `Directional Light (1)`.
3. Confirm the Hierarchy has production roots `RuntimeDecorations_Production`, `Bootstrap`, `WarlineCaptureUIBootstrap`, `Main Camera`, `GameSubScene`, `Chapter01_TacticalMissionRuntime`, and only one `Directional Light`.
4. Open `Assets/Game/Scenes/Game_Legecy.unity`.
5. Confirm the Hierarchy includes `UI_Canvas`, `Main Camera_Experiment`, `Global Volume_Experiment`, `Directional Light (1)`, `Decorations`, `Ground`, `SM_Skydome_01`, `Bootstrap`, and `GameSubScene`.
6. Press Play in `Game_Legecy.unity`.
7. Confirm it stays on the legacy canvas/prototype path and does not show the public 2D/isometric app loading route.

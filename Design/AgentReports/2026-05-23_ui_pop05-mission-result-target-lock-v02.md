Lane: UI

Task: Convert POP-05 Mission Result target lock into a Unity canvas scene using the current layered result-screen asset pack.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCapturePop05MissionResultSceneBuilder.cs
- Assets/Game/Scripts/Editor/WarlineCapturePop05MissionResultSceneBuilder.cs.meta
- Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01/
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_TargetLock.prefab
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_TargetLock.prefab.meta
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_TargetLock.unity
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_TargetLock.unity.meta
- Design/AgentReports/Captures/POP05_MissionResult_TargetLock_V02_2400x1080.png
- Design/AgentReports/2026-05-23_ui_pop05-mission-result-target-lock-v02.md

Contracts touched:
- Added a design-target-only Unity builder and scene for POP-05 Mission Result.
- Did not modify the production MissionResultPopup prefab or runtime mission result flow.
- Reused WarlineCaptureLayeredUiBuilderUtility visible-bounds icon placement and major-panel overlap validation.

User-visible behavior:
- New design-target result screen scene renders the POP-05 mission result layout with live Unity text, buttons, stars, objective rows, rewards, consequences, performance stats, and background art.
- Capture output is available at Design/AgentReports/Captures/POP05_MissionResult_TargetLock_V02_2400x1080.png.

Validation run:
- Unity 6000.4.0f1 batchmode in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCapturePop05MissionResultSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-pop05-result-v02.log

Validation result:
- Passed. Unity built the prefab and scene, generated the V02 capture, and exited batchmode successfully.
- Builder major-panel overlap validation passed.

Known gaps:
- This is a first converted canvas pass for POP-05. It is clean and organized, but not a pixel-perfect match to the visual lock.
- Some green/gold frame accents are inherited from the generated POP-05 frame assets and may need asset-level cleanup if the target requires a thinner or less saturated edge.
- Bottom action rail proportions are functional but may need a later target-match pass if exact button spacing is required.

Cross-lane impacts:
- None to gameplay or runtime flow. The work is isolated to design-target assets, prefab, scene, and capture.

Next recommended task:
- Compare POP05_MissionResult_TargetLock_V02_2400x1080.png against the POP-05 visual lock and do a focused V03 alignment pass only on spacing, frame accent cleanup, and bottom action rail proportions.

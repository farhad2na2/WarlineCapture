Lane
UI

Task
Implement SCN-08 RTS Battle HUD target-lock canvas from the prepared layered assets.

Files changed
- Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V01_2400x1080.png

Contracts touched
- Added a dedicated design-target prefab and scene for SCN-08; the existing production Screen_MatchOverlay prefab was not replaced.
- Uses WarlineCaptureRoute.Match on the generated screen controller.

User-visible behavior
- New 2400x1080 match HUD target-lock scene composed from SCN-08 layered assets: battlefield, objective panel, selected squad panel, squad tray, command rail, top resource strip, right quick buttons, minimap, world command markers, and invalid command toast.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v01c-unity2.log

Validation result
- Passed. Unity generated the prefab, scene, and capture without missing sprite errors or major panel overlap validation failures.
- Capture output: Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V01_2400x1080.png

Known gaps
- This is V01 target-lock composition, not yet wired as the production runtime MatchOverlay replacement.
- It uses live TMP labels and simple dark fill backplates under frame-only sprites; a later pass can bind real match state and replace any fill-only areas with authored fill layers if desired.

Cross-lane impacts
- Gameplay can continue using the existing production Screen_MatchOverlay because it was not modified.
- Design/PM can compare the fresh SCN-08 capture against the layered target lock before deciding whether to promote it into production HUD flow.

Next recommended task
- Review SCN08_RTSBattleHUD_TargetLock_V01_2400x1080.png against the target lock, then run one focused V02 alignment pass for any obvious target-match deviations before production binding.

Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V05 refinement.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V05_2400x1080.png

Contracts touched
- No production MatchOverlay replacement.
- Regenerated the dedicated SCN-08 design-target prefab and scene.

User-visible behavior
- V05 keeps the V04 selected-panel and right-rail cleanup.
- V05 retunes world path, selection ring, focus brackets, and civilian marker placement between V03 and V04 so the markers are less aggressive while still closer to the central target read.
- V05 gives the selected MOVE command stronger selected emphasis.
- V05 softens minimap content tint and nudges objective row spacing.
- V05 reduces selected-panel ability chip height to improve bottom padding.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v05-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V05 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V05 is still a design-target canvas, not runtime-bound production HUD.
- Remaining target-match risk is exact world marker placement and minimap crop because both depend on the generated battlefield/minimap art.

Cross-lane impacts
- Existing production Screen_MatchOverlay remains untouched.

Next recommended task
- Review V05 against the SCN-08 target lock and decide whether to stop visual iteration or promote the design-target structure into the runtime MatchOverlay flow.

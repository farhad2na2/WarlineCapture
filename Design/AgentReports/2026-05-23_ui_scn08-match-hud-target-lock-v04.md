Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V04 refinement.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V04_2400x1080.png

Contracts touched
- No production MatchOverlay replacement.
- Regenerated the dedicated SCN-08 design-target prefab and scene.

User-visible behavior
- V04 tightens right quick rail spacing so the support action no longer crowds the bottom of its rail.
- V04 slightly separates selected-panel order text and action chips.
- V04 moves world command path, selection ring, focus brackets, and civilian marker placement closer to the target lock's central gameplay read.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v04-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V04 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V04 remains a design-target canvas, not a runtime-bound production HUD.
- World marker placement is still the main target-match risk because it depends on the generated battlefield art crop.

Cross-lane impacts
- Existing production Screen_MatchOverlay remains untouched.

Next recommended task
- Compare V04 against the target lock and choose whether to keep the adjusted world marker placement or tune markers back toward V03 while preserving the right-rail and selected-panel fixes.

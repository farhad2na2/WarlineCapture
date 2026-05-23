Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V08 panel correction.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V08_2400x1080.png

Contracts touched
- No production MatchOverlay replacement.
- Regenerated the dedicated SCN-08 design-target prefab and scene.

User-visible behavior
- V08 fixes the hostile spotted panel by replacing the incorrectly stretched jump-button frame with the dedicated threat-row frame.
- V08 makes JUMP a nested right-side action button instead of using the same sprite for the whole alert row.
- V08 reduces heavy squad tray and right quick rail frame intensity to make those panels read less overpowering.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v08-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V08 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V08 is still a design-target canvas, not runtime-bound production HUD.
- Remaining mismatch is mostly selected squad panel density, minimap crop, and exact generated-frame chrome parity.

Cross-lane impacts
- Existing production Screen_MatchOverlay remains untouched.

Next recommended task
- Review V08 as the corrected panel baseline. If accepted, promote the design-target structure toward runtime binding; if not, continue only with targeted selected-panel/minimap cleanup.

Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V06 refinement.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V06_2400x1080.png

Contracts touched
- No production MatchOverlay replacement.
- Regenerated the dedicated SCN-08 design-target prefab and scene.

User-visible behavior
- V06 refines the right threat/jump panel spacing and keeps the JUMP action aligned in its own safe area.
- V06 enlarges and recenters the right navigation buttons, improving pause/settings/build/support readability.
- V06 softens selected squad and squad tray portrait treatment and tightens health/status spacing.
- V06 slightly retunes world command marker opacity/position and minimap tint.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v06-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V06 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V06 is still a design-target canvas, not runtime-bound production HUD.
- Remaining target-match risk is the selected squad panel density and exact world marker placement.

Cross-lane impacts
- Existing production Screen_MatchOverlay remains untouched.

Next recommended task
- Review V06 against the target lock and decide whether to continue visual iteration or begin promoting the SCN-08 design-target structure toward runtime binding.

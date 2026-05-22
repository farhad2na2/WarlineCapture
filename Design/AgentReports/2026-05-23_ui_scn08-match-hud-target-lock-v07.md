Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V07 panel match refinement.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V07_2400x1080.png

Contracts touched
- No production MatchOverlay replacement.
- Regenerated the dedicated SCN-08 design-target prefab and scene.

User-visible behavior
- V07 moves the right quick rail buttons upward into a rhythm closer to the target lock.
- V07 reduces selected squad portrait harshness and slightly reduces selected squad card intensity.
- V07 tightens minimap marker sizing/opacity so markers feel less pasted on.
- V07 preserves the stronger selected MOVE command and V06 panel cleanup.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v07-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V07 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V07 is still a design-target canvas, not runtime-bound production HUD.
- Remaining mismatch is mostly exact art parity: selected panel frame/portrait treatment, minimap content crop, and generated frame chrome differences.

Cross-lane impacts
- Existing production Screen_MatchOverlay remains untouched.

Next recommended task
- Review V07 as the current best match and decide whether to do one more micro-pass on selected panel/minimap or start runtime binding from this design-target layout.

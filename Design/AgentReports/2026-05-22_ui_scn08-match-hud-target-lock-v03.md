Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V03 layout refinement.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V03_2400x1080.png

Contracts touched
- No production MatchOverlay replacement.
- Regenerated the dedicated SCN-08 design-target prefab and scene.

User-visible behavior
- V03 fixes the truncated Civilian Risk resource label, adjusts the selected squad portrait/order/action spacing, and expands the minimap image crop closer to the target.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v03-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V03 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V03 is still a design-target implementation, not runtime-bound production HUD.
- Remaining refinement candidates are exact world marker placement, selected squad panel density, and right quick rail proportions.

Cross-lane impacts
- Existing production Screen_MatchOverlay remains untouched.

Next recommended task
- Review V03 against the SCN-08 layered target lock and decide whether to do one more target-match pass or promote the design-target structure toward runtime binding.

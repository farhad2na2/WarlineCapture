Lane
UI

Task
SCN-08 RTS Battle HUD target-lock V02 layout cleanup.

Files changed
- Assets/Game/Scripts/Editor/WarlineCaptureScn08RtsBattleHudSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity
- Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V02_2400x1080.png

Contracts touched
- No production MatchOverlay replacement yet.
- Existing generated SCN-08 design-target prefab and scene were regenerated.

User-visible behavior
- V02 improves selected squad panel containment, command rail centering, squad tray padding, right quick rail spacing, and minimap safe crop/marker density.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn08RtsBattleHudSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn08-targetlock-v02b-unity2.log

Validation result
- Passed. Unity regenerated the prefab, scene, and V02 2400x1080 capture without missing sprite errors or major panel overlap failures.

Known gaps
- V02 remains a design-target canvas, not runtime-bound production HUD.
- Further target-match improvement should focus on exact battlefield marker positions, right rail proportions, minimap crop, and selected-panel art/text density.

Cross-lane impacts
- No Gameplay changes required. Existing production Screen_MatchOverlay was not edited.

Next recommended task
- Compare V02 against SCN-08 target lock and decide whether to promote this design-target structure toward runtime binding or do a V03 visual match pass first.

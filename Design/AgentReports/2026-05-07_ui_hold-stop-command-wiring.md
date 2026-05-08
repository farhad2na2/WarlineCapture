Lane:
UI

Task:
Wire visible Screen_MatchOverlay Hold and Stop controls to the gameplay bridge methods implemented by the gameplay lane.

Files changed:
- Assets/Game/Scripts/UI/Screens/MatchOverlayCommandControlsController.cs
- Assets/Game/Scripts/UI/Screens/MatchOverlayCommandControlsController.cs.meta
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab
- Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs
- Assets/Tests/Editor/BattleHudGameplayBridgeConnectionTests.cs
- Design/AgentTasks/ui_current.md
- Design/AgentReports/2026-05-07_ui_hold-stop-command-wiring.md

Contracts touched:
- Screen_MatchOverlay now exposes MatchOverlayCommandControlsController.
- CommandBar/HoldButton invokes RTSSelectionSystem.IssueHoldPositionOrder.
- CommandBar/StopButton invokes RTSSelectionSystem.IssueStopOrder.
- CommandWheelCanvas/RadialCommandRoot/StopSegment closes the command wheel and invokes RTSSelectionSystem.IssueStopOrder.
- Existing BattleHudGameplayBridge tactical feedback text is preserved for Hold and Stop.

User-visible behavior:
Players can press the visible Hold and Stop command controls on the battle HUD and receive the matching HOLD POSITION or STOP ORDER tactical feedback while active move/attack/path orders are cleared by the gameplay selection system.

Validation run:
- Unity batch prefab regeneration: WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen
- Unity EditMode: WarlineCaptureUiMatchOverlayTests
- Unity EditMode: BattleHudGameplayBridgeConnectionTests

Validation result:
Passed. WarlineCaptureUiMatchOverlayTests passed 15/15. BattleHudGameplayBridgeConnectionTests passed 6/6.

Known gaps:
No PlayMode/device tap validation was run for physical Android input. The focused EditMode coverage verifies the serialized prefab contract and button-click wiring seam.

Cross-lane impacts:
Gameplay lane can keep RTSSelectionSystem as the source of order execution. UI now depends on the existing IssueHoldPositionOrder and IssueStopOrder methods and does not duplicate gameplay logic.

Next recommended task:
Run an Android smoke test for Screen_MatchOverlay command input after the broader gameplay scene is stable, then continue the UI plan with the next target-matched tactical screen or popup.

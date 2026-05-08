Lane: Gameplay agent
Task: Finish BattleHudGameplayBridge handoff for remaining command modes: Hold, Stop, Build, and Special.
Files changed:
- Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/RoadBuildSystem.cs
- Assets/Game/Scripts/UI/Screens/BuildDrawerPanelController.cs
- Assets/Game/Scripts/UI/Screens/CommandWheelPanelController.cs
- Assets/Tests/Editor/BattleHudGameplayBridgeConnectionTests.cs
- Design/AgentReports/2026-05-07_gameplay_battlehud-bridge-remaining-modes.md
Contracts touched:
- BattleHudGameplayBridge.ResolveActive
- BattleHudGameplayBridge.ApplyCommandMode
- BattleHudGameplayBridge.ClearCommandMode
- BattleHudGameplayBridge.ApplyCommandResult
- TacticalCommandMode.Hold
- TacticalCommandMode.Stop
- TacticalCommandMode.Build
- TacticalCommandMode.Special
- TacticalCommandReasonCode.NoSelection
- RTSSelectionSystem.IssueHoldPositionOrder
- RTSSelectionSystem.IssueStopOrder
- BuildDrawerPanelController Open/Close command-mode behavior
- CommandWheelPanelController Open/Close command-mode behavior
- BuildingPlacementSystem build placement enter/exit command-mode behavior
- RoadBuildSystem road/build placement enter/exit command-mode behavior
User-visible behavior:
Hold and Stop now publish command feedback through the BattleHud bridge and clear active selected-unit orders. Hold/Stop failures with no selection produce the shared NoSelection rejection result. Build placement, road build mode, and the build drawer publish BUILD MODE and clear it on exit/cancel/close. The command wheel publishes SPECIAL ORDER and clears it on close.
Validation run:
- git diff --check -- Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs Assets/Game/Scripts/UI/RTSSelectionSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/UI/RoadBuildSystem.cs Assets/Game/Scripts/UI/Screens/BuildDrawerPanelController.cs Assets/Game/Scripts/UI/Screens/CommandWheelPanelController.cs Assets/Tests/Editor/BattleHudGameplayBridgeConnectionTests.cs
- Unity EditMode: BattleHudGameplayBridgeConnectionTests
- Unity EditMode: WarlineCaptureUiMatchOverlayTests
Validation result:
Passed. git diff --check passed. BattleHudGameplayBridgeConnectionTests passed 4/4. WarlineCaptureUiMatchOverlayTests passed 15/15.
Known gaps:
Manual scene validation was not run. Hold/Stop buttons on Screen_MatchOverlay still need UI-side invocation if the UI agent has not already connected those visual buttons to RTSSelectionSystem.IssueHoldPositionOrder and RTSSelectionSystem.IssueStopOrder.
Cross-lane impacts:
UI can wire the existing Hold and Stop buttons to the new RTSSelectionSystem methods. FTUE can plan typed Hold/Stop/Build/Special tutorial steps against the shared BattleHud command-mode contract.
Next recommended task:
UI should connect the visual Hold and Stop controls to RTSSelectionSystem.IssueHoldPositionOrder and RTSSelectionSystem.IssueStopOrder, then FTUE can reference those command surfaces in the M01 teaching flow.

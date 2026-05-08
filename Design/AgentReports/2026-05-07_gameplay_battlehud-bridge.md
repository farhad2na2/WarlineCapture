Lane: Gameplay agent
Task: Wire real gameplay selection and command events into BattleHudGameplayBridge.
Files changed:
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Tests/Editor/BattleHudGameplayBridgeConnectionTests.cs
Contracts touched:
- BattleHudGameplayBridge.ApplySelection
- BattleHudGameplayBridge.ClearSelection
- BattleHudGameplayBridge.ApplyCommandMode
- BattleHudGameplayBridge.ClearCommandMode
- BattleHudGameplayBridge.ApplyCommandResult
- BattleHudGameplayBridge.SetWorldMarkersVisible
- TacticalCommandMode.Move
- TacticalCommandMode.Attack
- TacticalCommandReasonCode.NoSelection
- TacticalCommandReasonCode.InvalidTarget
- TacticalCommandReasonCode.BlockedRoute
User-visible behavior:
Selecting units or squads updates the match HUD selected entity panel. Deselect clears it. Attack target mode shows the attack command banner. Move and attack success clear invalid-command feedback and enable marker visibility. Invalid target and no-selection command attempts now show the HUD invalid-command toast through the bridge.
Validation run:
- BattleHudGameplayBridgeConnectionTests
- Chapter01TacticalRuntimeBindingTests
- WarlineCaptureUiQuickCustomTests
- WarlineCaptureUiMatchOverlayTests
Validation result:
Passed. BattleHudGameplayBridgeConnectionTests 2/2, Chapter01TacticalRuntimeBindingTests 4/4, WarlineCaptureUiQuickCustomTests 16/16, WarlineCaptureUiMatchOverlayTests 15/15.
Known gaps:
Hold, stop, build, and special command modes are not yet emitted from gameplay in this pass; they need wiring at their actual command entry points. Manual scene validation was not run.
Cross-lane impacts:
The UI bridge is now driven by gameplay events, so the UI agent can validate the HUD against real selection and command state instead of placeholders. FTUE can begin relying on these command feedback surfaces for select, move, attack, and invalid-command teaching.
Next recommended task:
Wire build, hold, stop, and special command entry points into the same bridge contract, then continue the Chapter 1 gameplay implementation plan.

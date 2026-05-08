Status: accepted
Reason: UI created the assistant panel prefab shell, then aligned it to the support/FTUE M01 assistant contract. The prefab exposes live TMP labels, tabs, chips, Show Me / Do It / Stop action availability, and focused tests passed.
Validation accepted:
- `WarlineCaptureUiAssistantPanelTests` passed 6/6 in the latest UI report.
- Prefab generation via `WarlineCaptureUiPhase1PrefabBuilder.BuildAssistantPanelPrefab` passed after stale lock cleanup.
Validation still needed:
- Screenshot/capture validation remains unrun for the assistant panel.
- Runtime ARIA service/controller is not connected yet.
Cross-lane notices:
- Support/FTUE can bind recommendation text/chips/action availability through `AssistantPanelView`.
- Gameplay/FTUE still need a future controller/service to produce live M01 recommendation state.
- UI has one higher-priority bridge follow-up from gameplay: connect visual Hold/Stop controls to `RTSSelectionSystem.IssueHoldPositionOrder` and `RTSSelectionSystem.IssueStopOrder`.
Tracking updates:
- No dashboard update yet.
Next task:
- UI should connect the visible SCN-08 Hold and Stop controls to the new gameplay methods, with focused tests, before starting the assistant presentation controller.

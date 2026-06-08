Lane:
UI

Task:
Implement the assistant panel presentation controller shell for `PREFAB-05_AssistantPanel`.

Files changed:
- Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs
- Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs.meta
- Assets/Tests/Editor/WarlineCaptureUiAssistantPanelControllerTests.cs
- Assets/Tests/Editor/WarlineCaptureUiAssistantPanelControllerTests.cs.meta
- Design/AgentTasks/ui_current.md
- Design/AgentReports/2026-05-07_ui_assistant-panel-presentation-controller.md

Contracts touched:
- `PREFAB-05_AssistantPanel`
- `AssistantPanelView.BindRecommendation(title, body, chips, canShow, canExecute, canStop)`
- `Design/AssistantPanel_M01_Implementation_Contract.md`
- `Design/AssistantRuntime_M01_Wiring_Plan.md`

User-visible behavior:
The project now has an `AssistantPanelController` presentation shell that can instantiate, show, hide, and bind the ARIA assistant panel with contract-safe recommendation data. Show Me, Do It, and Stop are exposed as callback seams that pass the active recommendation id, but they do not execute gameplay or final ARIA typed intents yet.

Validation run:
- Unity EditMode: `WarlineCaptureUiAssistantPanelTests`
- Unity EditMode: `WarlineCaptureUiAssistantPanelControllerTests`

Validation result:
Passed. `WarlineCaptureUiAssistantPanelTests` passed 6/6. `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4.

Known gaps:
- The controller intentionally does not implement final M01 recommendation production, typed intent dispatch, highlight behavior, takeover ownership, or save/session state.
- No runtime scene prefab has been updated to mount the assistant panel controller yet.
- No PlayMode or Android validation was run for assistant panel input.

Cross-lane impacts:
Support/FTUE can wire future `WarlineCaptureAssistantService` recommendations into `AssistantPanelController` without writing child paths or executing gameplay from UI. Gameplay remains the owner of selection, move, attack, and command-result execution through the future typed intent executor.

Next recommended task:
After the support/FTUE runtime plan is accepted, implement the first `WarlineCaptureAssistantService` / `M01AssistantRecommendationProvider` slice and mount `AssistantPanelController` in the match HUD or app shell behind the accepted assistant entry point.

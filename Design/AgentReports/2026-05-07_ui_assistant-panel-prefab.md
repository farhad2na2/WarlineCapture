Lane:
UI

Task:
Create the `PREFAB-05_AssistantPanel` Canvas prefab shell with live TMP labels, ARIA tab/chip/action controls, and focused validation.

Files changed:
- `Assets/Game/Scripts/UI/Components/AssistantPanelView.cs`
- `Assets/Game/Scripts/UI/Components/AssistantPanelView.cs.meta`
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab`
- `Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab.meta`
- `Assets/Tests/Editor/WarlineCaptureUiAssistantPanelTests.cs`
- `Assets/Tests/Editor/WarlineCaptureUiAssistantPanelTests.cs.meta`
- `Design/AgentReports/2026-05-07_ui_assistant-panel-prefab.md`

Contracts touched:
- Added `AssistantPanelView` runtime binding surface for title/status text, recommendation title/body text, assistant tabs, recommendation chips, and `ShowMeButton`/`DoItButton`/`StopButton`.
- Added `WarlineCapture/UI/Build Assistant Panel Prefab` builder command.
- Added focused EditMode tests for assistant panel hierarchy, references, chrome, animated buttons, typography, and simple recommendation binding.

User-visible behavior:
The project now has a reusable ARIA assistant panel prefab at `Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab`. It is not yet wired into runtime FTUE/assistant flows.

Validation run:
- Unity batch prefab generation: `WarlineCaptureUiPhase1PrefabBuilder.BuildAssistantPanelPrefab`
- Unity EditMode: `WarlineCaptureUiAssistantPanelTests`

Validation result:
Passed. `WarlineCaptureUiAssistantPanelTests` ran 5 tests with 5 passed, 0 failed.

Known gaps:
- No runtime assistant controller or FTUE data source is connected yet.
- No prefab screenshot capture was produced; this task validated the component prefab contract through EditMode tests.
- The flat target reference is implemented as a reusable shell with existing WarlineCapture chrome, not as final runtime ARIA behavior.

Cross-lane impacts:
Gameplay/FTUE lanes can bind future ARIA recommendations and approved actions through `AssistantPanelView` without replacing the prefab hierarchy.

Next recommended task:
Implement the assistant button and assistant panel runtime presentation path, then connect the first FTUE recommendation data source.

Lane:
UI

Task:
Align `PREFAB-05_AssistantPanel` with the new M01 assistant implementation contract from Support/FTUE.

Files changed:
- `Assets/Game/Scripts/UI/Components/AssistantPanelView.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab`
- `Assets/Tests/Editor/WarlineCaptureUiAssistantPanelTests.cs`
- `Design/AgentReports/2026-05-07_ui_assistant-panel-contract-alignment.md`

Contracts touched:
- `Design/AssistantPanel_M01_Implementation_Contract.md`
- `PREFAB-05_AssistantPanel`
- `AssistantPanelView.BindRecommendation`
- `AssistantPanelView.SetActionAvailability`

User-visible behavior:
The assistant panel prefab now exposes the contract tab set `Next`, `Why`, `Plan`, and `Goals`; the recommendation chips now expose `Primary`, `Tactical`, and `Risk`; `ShowMeButton`, `DoItButton`, and `StopButton` can be enabled or disabled from the view binding surface.

Validation run:
- Unity batch prefab generation: `WarlineCaptureUiPhase1PrefabBuilder.BuildAssistantPanelPrefab`
- Unity EditMode: `WarlineCaptureUiAssistantPanelTests`

Validation result:
Passed. `WarlineCaptureUiAssistantPanelTests` ran 6 tests with 6 passed, 0 failed. The first batch attempt hit a stale `WarlineCapture-CodexUnity/Temp/UnityLockfile`; process inspection showed live Unity was using `/Users/farhad/Projects/WarlineCapture`, not `WarlineCapture-CodexUnity`, so the stale CodexUnity lock was removed and the rerun passed.

Known gaps:
- No runtime ARIA service/controller is connected yet.
- No screenshot capture was produced for this prefab-only pass.
- `CloseButton` and `Portrait` remain optional first-pass fields per the M01 contract and are not exposed on `AssistantPanelView`.

Cross-lane impacts:
Support/FTUE can now bind M01 recommendation text, chips, and action availability without relying on child-path writes or baked text. Gameplay/FTUE still need the future controller/service to translate live M01 context into these view calls.

Next recommended task:
Implement the assistant panel presentation controller and first M01 recommendation producer using typed ids from `AssistantPanel_M01_Implementation_Contract.md`.

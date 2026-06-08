Lane:
UI

Task:
P0 assistant runtime binding fix pass for takeover ownership visibility, player-input release, and result-flow Stop validation.

Files changed:
- Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs
- Assets/Game/Scripts/UI/Screens/AssistantRuntimeBinding.cs
- Assets/Game/Scripts/UI/Screens/AssistantRuntimeBinding.cs.meta
- Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs
- Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs.meta
- Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md

Contracts touched:
- Design/AssistantPanel_M01_Implementation_Contract.md: preserved the typed assistant boundary; Show Me remains preview/focus state, Do It remains routed through CommandIntentExecutor, and Stop is bounded to assistant-owned state.
- Design/AssistantRuntime_M01_Wiring_Plan.md: completed the UI handoff validation for WarlineCaptureAssistantService presentation data, AssistantContextProvider-style context evaluation, CommandIntentExecutor execution, visible ownership state, and player-input release.
- M01 assistant ids: added focused validation for M01.ResultExplain so ARIA Stop dismisses assistant explanation/control only and does not close or acknowledge POP-05_MissionResult.
- Asset register rows were not advanced; no separated asset row was marked complete.

User-visible behavior:
The mounted assistant panel now opens from WarlineCaptureAssistantService.CreatePresentationData() through a presentation provider instead of falling back to placeholder content. The panel status line exposes visible ARIA ownership state such as guidance, preview, takeover/control, and player override release. Do It briefly enters assistant takeover ownership for a bounded typed command, executes through CommandIntentExecutor, then returns control to the player. Player input routed through the outside-panel release hook clears assistant preview/takeover state and returns the assistant button to the recommendation state. Stop during M01.ResultExplain dismisses only the ARIA explanation/control state and leaves POP-05_MissionResult active.

Validation run:
- Scoped source check: `git diff --check -- Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs Assets/Game/Scripts/UI/Screens/AssistantRuntimeBinding.cs Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs`
- Source grep: no `.Find(`, `FindObject`, `GetComponentInChildren`, `Screen.`, `mousePosition`, `anchoredPosition`, `NameText`, or `SelectedEntityPanel` in AssistantRuntimeBinding.cs / AssistantPanelController.cs.
- Unity EditMode: `WarlineCaptureUiAssistantRuntimeBindingTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- Unity EditMode: `WarlineCaptureUiAssistantPanelControllerTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- Unity EditMode: `WarlineCaptureUiMatchOverlayTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- Unity graphics capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayAssistantValidationSet`
- Capture asset check: PNG dimensions, RGBA mode, and pixel variance for closed/open 16:9 and 20:9 captures.

Validation result:
Passed. `WarlineCaptureUiAssistantRuntimeBindingTests` passed 7/7, including the new M01.ResultExplain Stop test and takeover/player-input release test. `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4. `WarlineCaptureUiMatchOverlayTests` passed 18/18. Captures were regenerated at `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`, `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`, `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`, and `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`; the assistant-open captures show live objective recommendation data, not placeholder chips. Capture sizes are 1672x941 and 2400x1080 RGBA with nonzero RGB variance. UI-scoped `git diff --check` passed.

Known gaps:
No blocker remains for the current UI runtime-binding fix pass. The outside-panel input release is exposed and validated through `AssistantRuntimeBinding.NotifyPlayerInputOutsideAssistant()` so the runtime input layer can route real player input into the assistant-owned-state release without UI hierarchy or screen-coordinate execution. World highlight rendering for Show Me remains outside this pass and should stay behind the existing typed preview/focus contract.

Cross-lane impacts:
Support/FTUE can rely on the UI panel using live WarlineCaptureAssistantService presentation data, typed command intents, and a validated result-flow Stop boundary. QA/HCI can include M01.ResultExplain in the assistant smoke pass and verify that ARIA Stop does not close POP-05_MissionResult. Gameplay/input integration can call `NotifyPlayerInputOutsideAssistant()` when player input outside the assistant panel should pause/cancel assistant takeover.

Next recommended task:
PM review of this fix report, then QA/HCI M01 smoke once the gameplay sprite-renderer evidence is available.

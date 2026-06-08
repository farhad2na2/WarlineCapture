Lane:
UI

Task:
P0 assistant runtime binding to live M01 context and typed command executor.

Files changed:
- Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs
- Assets/Game/Scripts/UI/Screens/AssistantRuntimeBinding.cs
- Assets/Game/Scripts/UI/Screens/AssistantRuntimeBinding.cs.meta
- Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs
- Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs.meta
- Design/AgentReports/2026-05-07_ui_assistant-runtime-binding.md

Contracts touched:
- Design/AssistantPanel_M01_Implementation_Contract.md: preserved the typed assistant boundary; UI does not execute gameplay through hierarchy, HUD text, or screen coordinates.
- Design/AssistantRuntime_M01_Wiring_Plan.md: consumed `WarlineCaptureAssistantService`, `AssistantContextProvider`, and `CommandIntentExecutor` through the accepted runtime handoff.
- Design/Art_Asset_Requirements_Register.csv: not advanced; asset rows remain pending until runtime binding and final integration are PM-reviewed.

User-visible behavior:
The match HUD assistant entry now evaluates live M01 assistant context, drives `AssistantButtonView.SetState(...)` from typed recommendation/readiness/control state, and opens the assistant panel with `WarlineCaptureAssistantService.CreatePresentationData()` instead of the old placeholder. `Show Me` sets assistant-owned preview state only, `Do It` routes through `CommandIntentExecutor`, and `Stop` clears assistant-owned preview/takeover state.

Validation run:
- Unity builder: `WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- EditMode: `WarlineCaptureUiAssistantRuntimeBindingTests`
- EditMode: `WarlineCaptureUiAssistantPanelControllerTests`
- EditMode: `WarlineCaptureUiMatchOverlayTests`
- Graphics capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayAssistantValidationSet`
- Source grep: no `.Find(`, `FindObject`, `GetComponentInChildren`, `Screen.`, `mousePosition`, `anchoredPosition`, `NameText`, or `SelectedEntityPanel` in the runtime binding/controller sources.
- Scoped `git diff --check` on the UI files changed by this pass.

Validation result:
Passed. `WarlineCaptureUiAssistantRuntimeBindingTests` passed 5/5. `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4. `WarlineCaptureUiMatchOverlayTests` passed 18/18. Captures were regenerated and visually inspected at `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`, `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`, `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`, and `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`; the assistant-open captures now show live objective recommendation data without the placeholder chip. UI-scoped `git diff --check` passed.

Known gaps:
Full-worktree `git diff --check` was not used as final evidence because unrelated dirty gameplay/ISO files currently contain Unity-generated whitespace and untracked work from another lane. The UI binding uses typed preview state but does not implement world highlight rendering for `Show Me`; that remains bounded to assistant-owned state until the highlight/anchor presentation contract is assigned. Asset-register rows remain unapproved pending PM review.

Cross-lane impacts:
Support/FTUE can now treat the mounted assistant panel and button as connected to `WarlineCaptureAssistantService`, `AssistantContextProvider`, and `CommandIntentExecutor`. QA/HCI can smoke the M01 assistant flow through visible button states, live panel data, and typed `Do It` command execution. Gameplay command authority remains with the accepted typed runtime hooks.

Next recommended task:
PM review of the runtime binding report and refreshed 16:9/20:9 assistant-open captures, then QA/HCI M01 smoke for select, move, attack, assistant recommendation, invalid-command recovery, and result flow.

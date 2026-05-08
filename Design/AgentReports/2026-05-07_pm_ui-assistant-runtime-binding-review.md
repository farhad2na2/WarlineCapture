# PM Review - UI Assistant Runtime Binding

Date: 2026-05-07
Lane reviewed: UI
Report reviewed: `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding.md`

## Status

Needs fixes.

## Reason

The core runtime binding is directionally correct: the assistant panel now reads live `WarlineCaptureAssistantService` presentation data, the button state is driven from typed assistant/runtime context, and `Do It` routes through `CommandIntentExecutor` instead of UI hierarchy or screen-coordinate execution. The 16:9 and 20:9 assistant-open captures also show live objective recommendation data rather than the old placeholder chip.

The handoff is not complete against the active assistant contracts because it does not validate result-flow `Stop` behavior against `POP-05_MissionResult`, and it does not provide or validate `POP-10 Assistant Takeover` or an equivalent visible ownership/takeover state with player-input cancellation/pause behavior.

## Validation Accepted

- `WarlineCaptureUiAssistantRuntimeBindingTests`: 5/5 passed.
- `WarlineCaptureUiAssistantPanelControllerTests`: 4/4 passed.
- `WarlineCaptureUiMatchOverlayTests`: 18/18 passed.
- Source grep found no banned screen-coordinate, child-name execution, or selected-entity-panel coupling in `AssistantRuntimeBinding.cs` / `AssistantPanelController.cs`.
- Refreshed assistant-open captures exist for 16:9 and 20:9 at `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png` and `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`.

## Validation Still Needed

- Add focused validation proving `Stop` during `M01.ResultExplain` dismisses assistant explanation/control state only and does not close or acknowledge `POP-05_MissionResult`.
- Provide `POP-10 Assistant Takeover` or a clearly named equivalent visible ownership state, then validate it appears when control owner state is takeover/control.
- Validate player input outside the assistant panel cancels or pauses takeover ownership and returns control to the player, or report the exact runtime blocker.
- Rerun the affected focused UI/runtime-binding tests and regenerate 16:9 and 20:9 assistant-open captures if visuals change.

## Cross-Lane Notices

- UI: continue with `Design/AgentTasks/ui_current.md` and write `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md`.
- QA/HCI: do not start integrated assistant-route smoke until the UI fix report and Gameplay sprite-renderer evidence land.
- Support/FTUE: no new Support API is required by this report; stay waiting unless UI reports a concrete missing API.
- Gameplay: no change; continue M01 sprite-atlas renderer hookup and close tactical capture evidence.

## Tracking Updates

- Updated `Design/AgentTasks/ui_current.md`.
- Updated `Design/AgentTasks/qa-hci_current.md`.
- Updated `Design/AgentTasks/M01_CRITICAL_PATH.md`.
- No project-state percentage change; the UI binding is not accepted for the gate until the fix report lands.

## Next Task

UI should fix and validate the takeover/result-flow gaps. Gameplay should continue the sprite-renderer handoff. QA/HCI remains blocked from balance conclusions.

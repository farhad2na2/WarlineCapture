# PM Review: UI Assistant Panel Presentation Controller

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_ui_assistant-panel-presentation-controller.md`

## Decision

Accepted as the UI presentation-shell slice.

## Validation Checked

- `/private/tmp/warlinecapture-assistant-panel-tests.xml`: passed 6/6.
- `/private/tmp/warlinecapture-assistant-panel-controller-tests.xml`: passed 4/4.

The new controller keeps the panel passive, binds through `AssistantPanelView`, exposes Show Me / Do It / Stop callback seams by recommendation id, and avoids hard-coded child path or screen-coordinate behavior.

## Cross-Lane Notices

- Support/FTUE can now target `AssistantPanelController` as the panel binding surface for the first `WarlineCaptureAssistantService` / `M01AssistantRecommendationProvider` slice.
- Gameplay still owns typed command execution. The assistant runtime plan's requested wrappers remain the boundary to watch: `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`.
- UI has not mounted the controller into the match HUD/app shell yet. That should be the next UI implementation task after the runtime service entry point is ready.

## Known Gaps Accepted

- No PlayMode or Android validation for assistant panel input yet.
- No runtime scene prefab or HUD shell mount yet.
- No recommendation production, typed intent dispatch, highlight behavior, takeover ownership, or tutorial save/session state yet.

## PM Follow-Up

`Design/AgentTasks/ui_current.md` is now completed and should be refreshed before UI auto-continues, otherwise the UI agent may repeat the accepted presentation-controller task.

`Design/AgentTasks/support-ftue_current.md` is also stale because its runtime wiring report has already been accepted. Support/FTUE should receive a follow-up task to convert runtime-plan open questions into a cross-lane implementation checklist and then start the first assistant service/recommendation-provider slice.

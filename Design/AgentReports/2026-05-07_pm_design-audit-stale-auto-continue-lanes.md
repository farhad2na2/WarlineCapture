Status: needs fixes
Topic:
Stale auto-continue lane files after accepted UI, Support/FTUE, and QA/HCI handoffs.

Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-presentation-review.md`
- `Design/AgentReports/2026-05-07_pm_support-runtime-wiring-review.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-validation-plan-review.md`

Finding:
Three auto-continue lanes are no longer aligned with their accepted PM reviews:

- UI is marked completed, but still points at the already accepted assistant-panel presentation-controller task.
- Support/FTUE is marked active, but still points at the already accepted runtime wiring contract.
- QA/HCI is marked active, but still points at the already accepted validation-plan task.

Gameplay remains current on M01 PlayMode validation.

Why it matters:
With 15-minute agent heartbeats enabled, stale lane files can make agents repeat completed work, continue from outdated context, or stop for approval because the next actionable work is not written in their lane file. This directly weakens the workflow optimization goal: the user should be able to tell agents `continue` without manually pasting task details.

Recommended fix:
Refresh the three stale lane files before their next auto-continue cycle:

- UI next task: mount `AssistantPanelController` behind the accepted match HUD/app-shell assistant entry point, using placeholder/contract-safe recommendations until Support/FTUE service data exists, and add capture/readability validation.
- Support/FTUE next task: implement the first `WarlineCaptureAssistantService` / `M01AssistantRecommendationProvider` slice or, if implementation sequencing needs a smaller step, create the runtime open-question checklist that tracks gameplay wrappers, UI mount, typed intents, and save/session fields.
- QA/HCI next task: switch from plan creation to watcher mode; do not balance yet, monitor gameplay/UI/support handoffs, and re-run third-project smoke checks after gameplay M01 PlayMode validation lands.

Affected lanes:
UI, Support/FTUE, QA/HCI, PM coordination.

Needs user decision:
No design decision is needed. This is a coordination/task-board refresh.

Next task update needed:
Yes. Update `Design/AgentTasks/ui_current.md`, `Design/AgentTasks/support-ftue_current.md`, and `Design/AgentTasks/qa-hci_current.md` before relying on auto-continue for those agents.

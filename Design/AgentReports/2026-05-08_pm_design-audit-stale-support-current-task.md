Status: needs fixes
Topic:
Support/FTUE current task is stale after route-capture ownership change
Docs reviewed:
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_support-ftue-route-capture-watch-review.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`
Finding:
`Design/AgentTasks/support-ftue_current.md` still says Support/FTUE is waiting on the UI assistant runtime-binding handoff and should monitor the old runtime-binding report. That handoff is already accepted, and the latest PM review says Support/FTUE has no implementation task unless the next QA/HCI rerun finds misleading ARIA guidance, ownership state, Stop behavior, or result explanation behavior.
Why it matters:
An auto-continuing Support/FTUE agent may keep rereading stale runtime-binding instructions, rewrite the same watch report, or report waiting against an already accepted UI task. This does not block Gate 4 directly, but it wastes the support lane and can create noise while UI owns the actual route-driven capture/safe-area tooling handoff.
Recommended fix:
Update `Design/AgentTasks/support-ftue_current.md` to wait on the route-capture sequence instead of the old runtime-binding handoff: UI delivers `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`, QA/HCI reruns `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`, and Support/FTUE only re-engages if that rerun reports a concrete assistant guidance/API issue.
Affected lanes:
- Support/FTUE
- UI
- QA/HCI
Needs user decision:
No. This is a task-routing cleanup, not a design change.
Next task update needed:
Yes. Update `Design/AgentTasks/support-ftue_current.md` before the next Support/FTUE auto-continue cycle.

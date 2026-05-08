Lane: Support/FTUE
Task: Updated the stale Support/FTUE current task so auto-continue waits on the current Gate 4 route-capture/safe-area sequence instead of the already accepted UI assistant runtime-binding handoff, then corrected the future completion-report filename after PM review.
Files changed:
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-08_support-ftue_stale-current-task-cleanup.md`
Contracts touched:
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-stale-support-current-task.md`
- `Design/AgentReports/2026-05-08_pm_support-ftue-route-capture-watch-review.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`
- `Design/AgentReports/2026-05-08_pm_support-ftue-stale-current-task-cleanup-review.md`
User-visible behavior:
- No runtime behavior changed.
- Future Support/FTUE auto-continue cycles now wait on UI route-driven capture/safe-area tooling and the QA/HCI rerun, rather than the accepted runtime-binding handoff.
Validation run:
- Documentation/task routing review only.
- No Unity validation required because no runtime code changed.
Validation result:
- `support-ftue_current.md` now names `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` as the UI-owned next handoff and `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` as the QA/HCI rerun.
- Waiting ownership fields are present and identify UI as the next owner, QA/HCI as rerun owner, and no Support/FTUE fallback work.
- Support/FTUE remains waiting unless the rerun finds a concrete assistant guidance/API, ownership, Stop, Show Me, or result-explanation issue.
- Completion report guidance now requires a new specific report filename for any future QA/HCI rerun issue, using `Design/AgentReports/2026-05-08_support-ftue_<specific-qa-rerun-issue>.md`, instead of reusing this cleanup report.
Known gaps:
- UI route-driven capture/safe-area tooling has not landed yet.
- QA/HCI route-driven or device/manual rerun evidence has not landed yet.
- Support/FTUE has no current implementation task.
Cross-lane impacts:
- UI should continue the route-driven capture/safe-area tooling task.
- QA/HCI should rerun Gate 4 after the UI handoff.
- Support/FTUE should re-engage only if the rerun identifies a concrete assistant behavior or contract blocker.
Next recommended task:
- UI should deliver `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`; Support/FTUE should stay waiting.

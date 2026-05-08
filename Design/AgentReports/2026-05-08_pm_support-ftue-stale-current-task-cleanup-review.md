Gate:
Support/FTUE Current Task Cleanup
Status:
needs fixes
Reason:
The cleanup correctly updates Support/FTUE to wait on the current Gate 4 sequence: UI route-driven capture/safe-area tooling, then QA/HCI route/safe-area rerun. It removes the stale dependency on the already accepted UI runtime-binding handoff and keeps Support/FTUE from starting unrelated FTUE mockup or implementation work.
Validation accepted:
- `Design/AgentTasks/support-ftue_current.md` now names `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` as the next UI-owned handoff.
- It names `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` as the QA/HCI rerun.
- It includes waiting ownership fields and correctly states Support/FTUE has no fallback work.
- No runtime code changed; no Unity validation was required.
Validation still needed:
- Fix the Completion Report section so future Support/FTUE review or fix work after QA/HCI rerun writes a new specific report filename instead of reusing `Design/AgentReports/2026-05-08_support-ftue_stale-current-task-cleanup.md`.
Cross-lane notices:
- UI remains the current owner for `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`.
- QA/HCI remains waiting for UI and then reruns `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`.
- Support/FTUE should only re-engage if UI or QA/HCI identifies a concrete assistant guidance/API, ownership, Stop, Show Me, or result-explanation issue.
Next gate/task:
Support/FTUE should make the small report-filename correction in `Design/AgentTasks/support-ftue_current.md`, then return to waiting. The corrected future report pattern should be a new file such as `Design/AgentReports/2026-05-08_support-ftue_<specific-qa-rerun-issue>.md`.

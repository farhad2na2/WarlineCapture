Status: advisory
Topic:
Support/FTUE current task still waits on stale Gate 4 route-capture blocker

Docs reviewed:
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`

Finding:
`support-ftue_current.md` still frames the lane as waiting on `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` and a route-capture/safe-area rerun. The active UI, Gameplay, QA/HCI, and M01 critical-path files have moved the blocker forward: the current issue is public M01 launch visible gameplay readability, not only route capture or safe-area tooling.

Why it matters:
Support/FTUE is mostly not the owner for this blocker, but its current task can make the support agent report stale waiting status or miss a concrete assistant/FTUE issue if QA/HCI later rejects the visible gameplay path because the player cannot understand the first objective, next action, assistant guidance, invalid-command recovery, or result flow. This can create idle noise and weak cross-lane coordination.

Recommended fix:
Update `support-ftue_current.md` after the current Gameplay/UI visible-scene blocker is resolved or if Support/FTUE is reactivated. The smallest task update should replace the stale route-capture/safe-area wait with a wait on the reviewed QA/HCI visible gameplay rerun, and define Support/FTUE re-entry only for concrete assistant guidance, objective comprehension, reason-code, command intent, ownership, `Show Me`, `Do It`, `Stop`, result explanation, or invalid-command recovery failures.

Affected lanes:
Support/FTUE, QA/HCI, PM

Needs user decision:
No.

Next task update needed:
Yes, but not during this heartbeat unless PM explicitly chooses to update current task files. The active implementation owner remains Gameplay/UI until readable public M01 visible gameplay evidence lands.

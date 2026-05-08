Status: needs fixes
Topic: Support/FTUE current task waits on stale Gate 4 route-capture artifact
Docs reviewed:
- `Design/AgentTasks/pm_design-audit.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-path-review.md`

Finding:
`support-ftue_current.md` still says Support/FTUE is waiting for `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` and then the QA/HCI rerun. That route-capture/safe-area phase has moved on: the active reports are now `2026-05-08_ui_m01-public-launch-path.md`, `2026-05-08_gameplay_m01-public-launch-path.md`, and `2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`. The current blockers are Gameplay ECS world-source proof/fix for visible non-Canvas world objects and QA/HCI Unity workspace licensing, not missing Support/FTUE API work.

Why it matters:
The Support/FTUE lane can remain idle for the correct reason, but the task file points it at a stale artifact and stale owner chain. On heartbeat/continue, that agent may keep waiting for an old UI file instead of watching the current QA/HCI rerun boundary for assistant-specific findings. This increases the chance that a real assistant guidance, takeover, `Stop`, `Show Me`, or result explanation issue is missed once Gameplay/QA unblock Gate 4.

Recommended fix:
Refresh `Design/AgentTasks/support-ftue_current.md` to say Support/FTUE is waiting on the current QA/HCI rerun boundary after Gameplay ECS world-source proof and QA workspace validation are resolved. Remove the stale `2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` dependency. Keep Support/FTUE blocked from fallback production work unless QA/HCI reports a concrete assistant contract issue.

Affected lanes:
Support/FTUE, QA/HCI, UI, Gameplay, PM/tooling.

Needs user decision:
No.

Next task update needed:
Yes. PM should update `Design/AgentTasks/support-ftue_current.md` so the support agent can safely continue with a correct waiting/watch instruction.

Status: needs fixes
Topic:
UI route-driven capture deliverable filename conflict
Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`
Finding:
`Design/AgentTasks/ui_current.md` correctly assigns UI to implement or expose route-driven M01 capture and safe-area evidence tooling, and its Completion Report section correctly names `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`. However, the PM Clarification section still says "The required UI deliverable is the integrated capture-matrix report" and names the old accepted file `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md`.
Why it matters:
The old integrated capture-matrix report is already accepted and does not contain route-driven screenshot or safe-area/device evidence. A UI agent following the PM Clarification literally could stop at the old accepted file, report waiting, or update the wrong report instead of producing the new tooling handoff that QA/HCI is waiting on.
Recommended fix:
Update `Design/AgentTasks/ui_current.md` so the PM Clarification names `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` as the required UI deliverable. Keep the old `2026-05-07_ui_m01-integrated-capture-matrix.md` only as accepted reference evidence.
Affected lanes:
- UI
- QA/HCI
Needs user decision:
No. This is a task-file wording correction, not a design change.
Next task update needed:
Yes. Update `Design/AgentTasks/ui_current.md` before the next UI heartbeat/continue so UI does not target the old accepted report.

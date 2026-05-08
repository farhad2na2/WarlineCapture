Status: needs fixes
Topic: Stale lane task files after accepted reports
Docs reviewed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-playable-runtime.md`
- `Design/AgentReports/2026-05-07_ui_hold-stop-command-wiring.md`
- `Design/AgentReports/2026-05-07_pm_heartbeat-review-1354.md`
Finding:
`gameplay_current.md` still points at the M01 playable runtime slice even though `2026-05-07_gameplay_m01-playable-runtime.md` reports that task complete and PM heartbeat review accepted it. `ui_current.md` is marked completed but does not yet provide the next UI assignment. Support/FTUE still has the correct active runtime wiring-plan task.
Why it matters:
If agents auto-continue from `Design/AgentTasks/`, gameplay may repeat completed M01 runtime work and UI may idle because its lane file is completed without a next task.
Recommended fix:
Update `gameplay_current.md` to the next task from the gameplay report: M01 PlayMode/scene validation pass. Update `ui_current.md` to the next safe task after Hold/Stop wiring: assistant panel presentation controller only after, or in parallel with placeholders until, the Support/FTUE runtime wiring plan lands.
Affected lanes:
- Gameplay
- UI
- PM assistant
Needs user decision:
No.
Next task update needed:
Yes. PM should refresh gameplay and UI lane files before the next auto-continue cycle.

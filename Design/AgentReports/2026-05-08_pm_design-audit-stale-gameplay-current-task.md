Status: advisory
Topic:
Gameplay current task names stale Gate 4 blocker
Docs reviewed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`
Finding:
`Design/AgentTasks/gameplay_current.md` correctly keeps Gameplay waiting unless QA/HCI reports a gameplay-owned blocker, but its context still says Gate 4 is blocked by the old integrated 16:9/20:9 UI/QA capture-readiness pass. That matrix is now accepted. The current Gate 4 blocker is UI route-driven capture/safe-area tooling followed by QA/HCI rerun evidence.
Why it matters:
This is not blocking current work because Gameplay is correctly waiting and has no new assigned blocker. It can still cause agent confusion: Gameplay may interpret future "continue" prompts as waiting on the old matrix pass rather than watching only for a concrete gameplay-owned issue from the upcoming QA/HCI rerun.
Recommended fix:
When lane task files are next updated, revise `Design/AgentTasks/gameplay_current.md` to state that Gameplay remains waiting on `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` after UI delivers `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`. Gameplay should only re-engage if the rerun reproduces runtime/gameplay failures, freezes, input stalls, severe FPS drops, gameplay-owned log spam, route behavior defects, or sprite/grounding/readability failures.
Affected lanes:
- Gameplay
- UI
- QA/HCI
Needs user decision:
No. This is stale task wording only.
Next task update needed:
Not urgent. Update alongside the stale Support/FTUE current-task cleanup or the next Gameplay assignment.

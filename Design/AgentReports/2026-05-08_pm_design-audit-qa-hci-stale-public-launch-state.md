Status: advisory
Topic:
QA/HCI current task still references stale Gate 4 blocker state

Docs reviewed:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/Agent_Coordination_Workflow.md`
- `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`
- `Design/AgentReports/2026-05-08_pm_public-launch-ownership-split.md`

Finding:
`qa-hci_current.md` still describes the active wait as route-driven capture/safe-area tooling, reason-code runtime validation, and public launch mismatch against the old 3D prototype. The active PM state has moved forward: reason-code alignment is accepted, the current public-launch blocker is brown/blank gameplay world evidence, and UI/GamePlay ownership is now split between canvas/capture composition and world/map/camera.

Why it matters:
QA/HCI may rerun or report against stale blocker language, especially by treating route/safe-area/tooling as the main wait instead of rejecting or validating the combined full-screen evidence: authored M01 world under the HUD plus live HUD/canvas over it. This increases the risk of another false acceptance or idle report.

Recommended fix:
Update `qa-hci_current.md` to match the current PM state:

- Wait for a revised Gameplay/UI public-launch handoff that explicitly covers the UI/GamePlay ownership split.
- Require full-screen evidence showing authored M01 tactical terrain/map, readable unit/target scale, HUD/objective/assistant context, and no old 3D prototype.
- Treat brown/blank/tiny-world evidence as a blocker even when HUD chrome and route state are present.
- Remove reason-code alignment from active blockers because it has been accepted.
- Keep remaining Gate 4 blockers: final 1920x1080 and 2400x1080 eight-state matrix, touch/camera proof, marker/VFX readiness or waiver, and log-health classification.

Affected lanes:
QA/HCI, Gameplay, UI, PM

Needs user decision:
No.

Next task update needed:
Yes. PM should update `Design/AgentTasks/qa-hci_current.md` when task-file edits are allowed.

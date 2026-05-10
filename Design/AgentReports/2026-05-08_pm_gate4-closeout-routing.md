Status: accepted
Topic:
Gate 4 closeout routing after QA/HCI rerun review

Lane:
PM

Task:
Convert the remaining Gate 4 decision boundary into concrete lane work so agents do not remain idle.

Files changed:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_gate4-closeout-routing.md`

Contracts touched:
- None. This is a task-board and PM routing update only.

User-visible behavior:
- None directly. The update unblocks lane automation by giving QA/HCI a focused closeout task instead of the already completed broad rerun.

Validation run:
- Reviewed current task files and latest PM/QA reports:
  - `Design/AgentTasks/gameplay_current.md`
  - `Design/AgentTasks/ui_current.md`
  - `Design/AgentTasks/qa-hci_current.md`
  - `Design/AgentTasks/support-ftue_current.md`
  - `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-rerun-review.md`
  - `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Validation result:
- Accepted. Gameplay, UI, and Support/FTUE are correctly waiting because their current blockers are closed unless QA/HCI finds a regression.
- QA/HCI was the only active lane, but its task still described the broad rerun whose effective latest sections were already PM-reviewed.
- `Design/AgentTasks/qa-hci_current.md` now assigns the remaining QA/HCI-owned Gate 4 closeout:
  - touch/camera HCI proof or documented substitute
  - warning/log-readiness classification
  - final QA/HCI recommendation excluding PM-owned waivers

Known gaps:
- PM still owns decisions that should not be hidden inside QA/HCI execution:
  - whether to waive temporary marker/VFX evidence for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`
  - whether current public-launch captures plus accepted safe-area matrices satisfy final `1920x1080`/`2400x1080` eight-state packaging, or whether QA/HCI must produce a final consolidated package

Cross-lane impacts:
- QA/HCI can continue immediately from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- Gameplay should stay waiting unless QA/HCI reports a concrete gameplay-owned regression.
- UI should stay waiting unless QA/HCI reports a concrete UI-owned regression.
- Support/FTUE should stay waiting unless QA/HCI reports a concrete assistant/FTUE issue.
- Art/design or an implementing lane may be needed if PM does not waive marker/VFX temporary evidence.

Next recommended task:
- QA/HCI should complete `Design/AgentReports/2026-05-08_qa-hci_gate4-touch-camera-log-closeout.md`.
- PM/user should decide marker/VFX waiver and final eight-state packaging sufficiency after QA/HCI closeout, unless they want to require those artifacts now.

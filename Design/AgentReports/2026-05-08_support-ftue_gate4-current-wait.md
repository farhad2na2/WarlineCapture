Lane:
Support/FTUE

Task:
Heartbeat review of `Design/AgentTasks/support-ftue_current.md` for the current Gate 4 route-capture assistant watch.

Files changed:
- `Design/AgentReports/2026-05-08_support-ftue_gate4-current-wait.md`

Contracts touched:
- None. This pass only reviewed the active Support/FTUE lane task and existing UI, QA/HCI, and PM handoffs.

User-visible behavior:
No runtime behavior changed.

Validation run:
- Read `Design/AgentTasks/support-ftue_current.md`.
- Read `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`.
- Read `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`.
- Read `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-rerun-review.md`.
- Read `Design/AgentReports/2026-05-08_pm_ui-m01-route-driven-capture-safe-area-tooling-review.md`.
- Read `Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`.
- Read `Design/AgentReports/2026-05-08_pm_support-ftue-m01-runtime-reason-code-alignment-final-review.md`.
- Static legacy alias check: `rg -n "InvalidTarget|BlockedRoute|OutOfRange|BuildModeUnavailable|InsufficientResources|AbilityOnCooldown|TransportUnavailable" Assets/Game/Scripts Assets/Tests -g '*.cs'`.

Validation result:
Support/FTUE has no current production-code action from the active lane task. The QA/HCI rerun previously identified `QAHCI-G4-012` as a Support/FTUE-relevant reason-code blocker, but the Support/FTUE runtime reason-code alignment handoff has since been accepted by PM in `Design/AgentReports/2026-05-08_pm_support-ftue-m01-runtime-reason-code-alignment-final-review.md`. The static scan now finds no legacy reason-code aliases in `Assets/Game/Scripts` or `Assets/Tests`; the only broad-text hit is unrelated framework text, `ArgumentOutOfRangeException`, in `Assets/Game/Scripts/UI/MenuView.cs`.

Known gaps:
- Public M01 production launch-path evidence remains outside Support/FTUE ownership.
- Human touch/camera ergonomics remain unverified.
- Marker/VFX assets remain outside Support/FTUE ownership unless PM assigns FTUE copy/contract work.
- Gameplay/UI may still refine broad `TargetNotAttackable` semantics later if runtime context can distinguish more specific canonical reasons.

Cross-lane impacts:
- QA/HCI can treat `QAHCI-G4-012` as ready for recheck/closure against the accepted Support/FTUE evidence.
- Gameplay/UI still own the public launch-path blocker.
- Art-design or the implementing lane still owns marker/VFX readiness.
- Support/FTUE should re-engage only if a later QA/HCI or UI handoff reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, or result-explanation behavior issue.

Next recommended task:
Wait for PM to refresh `Design/AgentTasks/support-ftue_current.md` or for QA/HCI/UI to report a new concrete Support/FTUE blocker.

Waiting on lane:
Gameplay/UI, QA/HCI, and art-design/integration.

Waiting on exact file/report/asset/command:
- Reviewed Gameplay/UI handoff proving a public M01 production launch path no longer enters the legacy 3D prototype, or explicitly labeling legacy paths as sandbox.
- QA/HCI affected-check rerun or blocker-table update that rechecks `QAHCI-G4-012` against `Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the public launch-path blocker. QA/HCI owns the reason-code recheck/closure after the accepted Support/FTUE handoff. Art-design or the implementing lane owns marker/VFX readiness.

Can my lane still continue fallback work? no.

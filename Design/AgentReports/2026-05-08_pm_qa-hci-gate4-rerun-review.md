Gate:
Gate 4: QA/HCI M01 smoke and readability

Status:
needs fixes

Reason:
QA/HCI completed the active rerun report in `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`. The report is append-only and contains older superseded blocker sections, but the latest public-launch ECS terrain validation section closes the former public-launch/ECS terrain blocker. Public Quick Custom and campaign launch can now be treated as reaching the M01 production slice for focused automation/capture scope.

Validation accepted:
- QA/HCI rerun used `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- `Chapter01M01PlayModeValidationTests` passed 5/5 for the public-launch ECS terrain validation.
- Prior focused route/UI smoke remains green: PlayMode route, UI shell, match overlay, and assistant runtime binding tests passed in the QA/HCI rerun record.
- Public-launch captures show authored M01 terrain, readable squads, no old 3D prototype, no flat brown/tiny-world field, and no obvious upside-down tactical plate.
- The named simulated safe-area profile matrix is accepted for simulated evidence:
  - `safe.none_16x9`
  - `safe.rounded_20x9`
  - `safe.cutout_left_20x9`
- Runtime reason-code alignment is accepted as closed in the QA/HCI report.
- Support/FTUE review in `Design/AgentReports/2026-05-08_support-ftue_qa-hci-rerun-no-action.md` is accepted: no Support/FTUE code or contract action is currently required.

Validation still needed:
- Human touch/camera ergonomics or an explicit PM-approved substitute.
- Marker/VFX asset readiness or explicit temporary-evidence waiver for:
  - `marker.selection.ring`
  - `marker.move.destination`
  - `marker.attack.target`
  - `vfx.unit.destroyed.small`
- Owner classification or PM waiver for remaining warning/log noise noted by QA/HCI: Animator warnings, `PerfDiag:ECS:PreGame`, preview-scene leak warnings, persistent allocation warnings, and usbmuxd/editor-tooling noise.
- PM decision on whether the accepted public-launch captures plus safe-area matrices satisfy final 1920x1080/2400x1080 eight-state review packaging, or whether QA/HCI must produce one final consolidated package.

Cross-lane notices:
- Gameplay no longer owns the public-launch/ECS terrain blocker unless a regression appears.
- UI no longer owns public-launch HUD/canvas composition or simulated safe-area matrix blockers unless a regression appears.
- Support/FTUE remains on watch only; no assistant/FTUE issue is assigned.
- QA/HCI owns the next HCI/touch/log-readiness validation work.
- Art-design or the implementing lane owns marker/VFX readiness unless PM grants a temporary-evidence waiver.
- PM owns waiver decisions for log noise, marker/VFX temporary evidence, and final packaging sufficiency.

Next gate/task:
Route the remaining Gate 4 items:
- QA/HCI: run or document touch/camera HCI proof and final log-readiness classification from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- PM: decide whether to waive or require final marker/VFX assets and final eight-state packaging.
- Art-design/implementing lane: provide marker/VFX evidence if PM does not waive temporary evidence.

Notes:
Older sections inside `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` still mention stale blockers such as legacy public launch, ECS terrain contract gaps, and safe-area profile blockers. Treat the later closure sections in that same report, plus this PM review, as the current effective state.

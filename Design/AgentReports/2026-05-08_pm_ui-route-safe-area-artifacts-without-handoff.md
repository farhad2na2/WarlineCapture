Status: blocked
Topic:
UI route/safe-area artifacts exist without the required handoff report

Evidence reviewed:
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-safe-area-profile-ambiguity.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-unrouted-gate4-findings.md`

Finding:
The UI route/safe-area capture source and generated PNG evidence are present, including eight M01 states at 1920x1080 and 2400x1080 plus safe-area manifests. However, the required UI handoff report has not landed:

- Missing: `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`

The current manifests only describe two generic "simulated landscape inset" profiles. They do not yet state the minimum Gate 4 profile ids from the PM safe-area audit:

- `safe.none_16x9`
- `safe.rounded_20x9`
- `safe.cutout_left_20x9`

The handoff also still needs to explicitly state the runtime status of the M01 invalid-command reason codes and the marker/VFX asset status requested by the PM audits.

Why it matters:
QA/HCI is waiting for a reviewable UI handoff, not just raw screenshots. Without the report, PM cannot accept the UI deliverable, QA cannot know which captures and assumptions are authoritative, and Gate 4 can return to the same UI/QA waiting loop.

Required UI handoff content before QA rerun:
- Standard WarlineCapture report fields: lane, files changed, contracts touched, validation run/result, known gaps, cross-lane impacts, next recommended task.
- Capture list and manifest paths for all route-driven states.
- Explicit safe-area profile ids, resolution, inset/cutout rectangles, and pass/fail notes for HUD, minimap, assistant panel, command controls, and result popup.
- Clear statement whether the invalid-command capture still uses legacy runtime reason-code aliases or the canonical M01 reason-code names.
- Clear marker/VFX status for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Affected lanes:
- UI
- QA/HCI
- PM

Needs user decision:
No.

Next task update needed:
No source task edit is required right now. UI should finish the missing handoff report, then QA/HCI should rerun from that report.

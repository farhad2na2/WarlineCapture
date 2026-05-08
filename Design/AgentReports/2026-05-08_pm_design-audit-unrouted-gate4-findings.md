Status: needs fixes
Topic:
Recent Gate 4 audit findings are not reflected in active lane tasks
Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-safe-area-profile-ambiguity.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-command-reason-code-mismatch.md`
- `Design/AgentReports/2026-05-08_pm_support-ftue-m01-reason-code-contract-cleanup-review.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-m01-feedback-marker-assets.md`
Finding:
The active UI and QA/HCI task files still only require generic route-driven screenshots and safe-area/device evidence. They do not yet carry the recent PM audit findings that affect the same Gate 4 evidence: exact simulated safe-area profiles are undefined, runtime `TacticalCommandReasonCode` still uses legacy aliases despite doc cleanup, and the required selection/move/attack/destroyed feedback marker assets are still missing or unapproved.
Why it matters:
UI could produce `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` without addressing these known review criteria, then QA/HCI would either miss the risks or reject the rerun late. That creates another preventable loop on the current critical path.
Recommended fix:
Before accepting the UI tooling handoff, update the UI/QA task routing or require the UI report to explicitly include:
- Safe-area evidence profiles: `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`, or a concrete replacement profile matrix.
- Invalid-command evidence: whether runtime emits canonical M01 reason codes or legacy aliases, with no QA assertion of canonical names until runtime mapping/code lands.
- Feedback marker/VFX status: whether `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small` are absent, placeholder, temporary review evidence, or approved.
Affected lanes:
- UI
- QA/HCI
- Gameplay
- Support/FTUE
Needs user decision:
No for routing the criteria. User approval is still needed later for final marker/VFX art quality.
Next task update needed:
Yes. Add these known Gate 4 criteria to `Design/AgentTasks/ui_current.md` and `Design/AgentTasks/qa-hci_current.md`, or explicitly assign a PM routing update before the next UI continuation.

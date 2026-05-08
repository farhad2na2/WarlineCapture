Status: needs fixes
Reviewed report:
- `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`

Lane:
PM review

Summary:
The UI handoff is directionally correct and includes route-driven editor capture tooling, eight-state PNG evidence at 1920x1080 and 2400x1080, a contact sheet, safe-area manifests, and focused UI test results. The report uses the standard WarlineCapture handoff fields.

Acceptance decision:
Not accepted yet for Gate 4 QA/HCI rerun.

Blocking findings:
- Safe-area evidence does not implement the minimum PM profile matrix from `Design/AgentReports/2026-05-08_pm_design-audit-safe-area-profile-ambiguity.md`. The handoff provides two generic simulated landscape inset profiles, but does not define or capture `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.
- The manifests do not include explicit profile ids, cutout rectangles, or per-surface pass/fail notes for HUD, minimap, assistant panel, command controls, and result popup. QA/HCI still has to infer what "safe" means.
- The handoff does not explicitly state whether the invalid-command capture still uses legacy runtime reason-code aliases or the canonical M01 reason-code names from the PM reason-code audit.
- The handoff does not clearly classify marker/VFX status for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small` as absent, placeholder, temporary, or approved.

Accepted evidence:
- The capture files exist under `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/`.
- PNG dimensions are valid for the expected 1920x1080 and 2400x1080 outputs.
- The editor tooling routes through `WarlineCaptureRouter` to `WarlineCaptureRoute.Match` before configuring the M01 states.
- Reported UI validation is strong enough for the tooling slice once the missing PM audit fields are added.

Required UI fix:
UI should revise the tooling/report before QA/HCI reruns:
- Add or clearly map captures/manifests for `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.
- For each profile, state resolution, inset rectangles, cutout rectangles if any, and per-surface pass/fail clearance notes.
- Add a runtime reason-code status line for the invalid-command capture.
- Add marker/VFX status lines for the four PM-tracked feedback assets.
- Keep the report target as `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` or add a clearly named fix report linked from it.

Cross-lane impact:
- QA/HCI remains waiting. Do not start the Gate 4 rerun from this handoff yet.
- Gameplay has no new action unless UI/QA identifies runtime command-code or gameplay behavior regressions.
- Support/FTUE has no new action unless the reason-code or assistant guidance wording is found to be misleading after UI fixes the report.

Needs user decision:
No.

Next recommended task:
UI should do a short fix pass on the safe-area profile matrix and missing audit-status fields, then report back. QA/HCI should remain blocked until that fix lands.

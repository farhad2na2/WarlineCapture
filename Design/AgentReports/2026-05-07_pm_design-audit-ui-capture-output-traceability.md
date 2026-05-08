Status: advisory
Topic: UI integrated capture matrix lacks output path and naming contract
Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentReports/2026-05-07_pm_design-audit-qa-capture-matrix.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
Finding:
- The active UI task now locks the first capture matrix to `1920x1080` and `2400x1080`, with required states for match start, squad selected, move, attack, invalid recovery, assistant open, assistant takeover/Stop, and result popup.
- It does not specify a destination folder, file naming convention, or contact-sheet/manifest format for the generated evidence.
Why it matters:
- UI can complete the right captures but leave PM/QA guessing which file maps to which state, resolution, safe-area assumption, or fallback route.
- Without stable names, QA cannot compare future capture regressions cleanly, and PM cannot tell at a glance whether all required states landed.
Recommended fix:
- In the UI handoff report, require a capture inventory table with one row per state and resolution.
- Prefer stable names such as:
  - `M01_Integrated_1920x1080_01_MatchStart.png`
  - `M01_Integrated_1920x1080_02_SquadSelected.png`
  - `M01_Integrated_2400x1080_01_MatchStart.png`
- Store captures under a single review folder such as `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/UIIntegrated/` or `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`, and state the chosen path in the report.
- Include safe-area assumption, source scene/route, and whether the capture is integrated route or fallback scene evidence.
Affected lanes:
- UI
- QA/HCI
- PM
Needs user decision:
- No immediate user decision required.
Next task update needed:
- Not urgent during the current UI run, but PM should check the UI handoff for a complete capture inventory before accepting Gate 4 visual evidence.

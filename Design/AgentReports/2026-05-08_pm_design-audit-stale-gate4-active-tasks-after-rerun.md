Status: needs fixes
Topic:
Gate 4 active task files are stale after the QA rerun

Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-route-driven-capture-safe-area-tooling-review.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-rerun-review.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-unreported-reason-code-runtime-edits.md`

Finding:
The active task files still describe the older Gate 4 state:

- `ui_current.md` still frames the work as delivering the first route-driven capture/safe-area tooling report.
- `qa-hci_current.md` still says to wait for `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`, even though that report and the QA rerun report have already landed.

The current PM-reviewed blockers are now narrower:

- UI owns the missing named safe-area profile matrix and per-surface clearance notes for `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.
- Gameplay/Support-FTUE owns the canonical reason-code runtime handoff and validation proof.
- QA/HCI should wait for reviewed fix reports, then rerun only affected checks.

Why it matters:
Agents following the current task files can repeat already-completed work or rerun QA too early from unaccepted evidence. This has already happened once: QA/HCI reran from a UI handoff that PM had marked `needs fixes`.

Recommended fix:
Refresh `Design/AgentTasks/ui_current.md` and `Design/AgentTasks/qa-hci_current.md` to reflect the current narrowed blockers:

- UI task: close `QAHCI-G4-011` with the three named safe-area profiles, manifests, and per-surface pass/fail clearance notes.
- QA/HCI task: wait for reviewed UI safe-area fix and reviewed reason-code runtime handoff before rerunning affected checks.
- Add a note that the existing route-driven capture report is accepted as evidence existence, but not final Gate 4 safe-area acceptance.

Affected lanes:
- UI
- QA/HCI
- Gameplay
- Support/FTUE
- PM

Needs user decision:
No.

Next task update needed:
Yes. PM should update the active task files before the next broad "continue" instruction.

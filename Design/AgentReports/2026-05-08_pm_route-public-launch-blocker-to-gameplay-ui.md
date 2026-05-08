Status: task routed
Topic:
Public M01 launch path routed to Gameplay and UI

Source:
PM review after user manual test feedback and active task audit.

Finding:
The active Gameplay and UI tasks were stale relative to the newest blocker. Gameplay was still effectively framed around waiting for QA/HCI integrated findings, and UI was still framed around already-accepted route/safe-area evidence. The current blocker is simpler and more important: public player launch paths for M01 / First Contact still enter the old 3D prototype.

Manual blocker evidence:
- Quick Custom / Launch shows the old 3D prototype.
- Main Menu -> Saga Map / campaign map -> First Contact also shows the old 3D prototype.

PM action:
- Updated `Design/AgentTasks/gameplay_current.md` to make the public M01 production launch path the active P1 task.
- Updated `Design/AgentTasks/ui_current.md` to make the UI shell/router/button side of the public M01 production launch path the active P1 task.
- Required both lanes to report public launch smoke evidence before asking for manual HCI/balance validation.
- Preserved the rule that legacy Quick Custom may remain only if explicitly labeled sandbox/legacy and a separate production M01 launch path exists.

Expected next lane outputs:
- Gameplay report: `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- UI report: `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Cross-lane impacts:
- QA/HCI remains blocked until a public launch smoke proves the user-facing path opens the current M01 2D/isometric production slice.
- Support/FTUE should not request manual assistant/HCI validation from the player route until the launch path is fixed.
- PM should reject any report that claims Gate 4 manual readiness while the first visible gameplay state is still the legacy 3D prototype.

Validation:
No Unity validation run by PM for this task-routing update. This is a coordination/documentation update only.

Needs user decision:
No immediate decision. PM recommendation stands: M01 public/campaign launch should open the production slice. Any retained old 3D route should be clearly labeled sandbox/legacy.

Next recommended task:
Tell Gameplay and UI to continue. They should read their updated current task files and fix/report the public M01 launch path blocker before starting unrelated work.

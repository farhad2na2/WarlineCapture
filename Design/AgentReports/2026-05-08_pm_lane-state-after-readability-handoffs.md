Status:
accepted routing update; Gate 4 still blocked

Lane:
PM

Task:
Update lane current-task files after accepted Gameplay readability and Art/Atlas temporary-art handoffs so agents do not repeat completed work.

Files changed:
- `Design/AgentReports/2026-05-08_pm_lane-state-after-readability-handoffs.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/support-ftue_current.md`

Contracts touched:
- Lane ownership and waiting-state routing only.
- No runtime, art, UI, gameplay, or QA contract changed.

User-visible behavior:
No runtime behavior changed. This update prevents agents from repeating already delivered Gameplay/Art work while the remaining Gate 4 blockers are pending.

Validation run:
- Read `Design/AgentTasks/pm_heartbeat.md`.
- Reviewed recent reports under `Design/AgentReports/`.
- Reviewed `Design/AgentReports/2026-05-08_art-atlas_post-gameplay-readability-watch.md`.
- Checked whether `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md` exists.
- Updated current lane task files to match accepted handoffs and current blockers.

Validation result:
- Gameplay is now waiting, not active. It delivered `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md` and should continue only if PM/user art decision, Art/Atlas replacement assets, or QA/HCI assigns a concrete Gameplay follow-up.
- Art/Atlas is now waiting, not active. It delivered `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md` and should continue only if PM/user rejects temporary art or requests a specific enemy variant/final VFX package.
- QA/HCI remains waiting for the UI HUD scope report and PM/user art decision before final Gate 4 rerun.
- Support/FTUE remains waiting for a concrete assistant/FTUE issue.
- UI remains active on `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`.

Known gaps:
- PM/user has not approved or rejected the temporary M01 infantry art package.
- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md` does not exist yet.
- `FinalAtlasArtReady` remains `0`.
- Gate 4 final QA/HCI rerun has not run.

Cross-lane impacts:
- UI owns the next lane deliverable.
- PM/user owns the art decision.
- QA/HCI owns the final rerun after those inputs exist.
- Gameplay and Art/Atlas should stay quiet unless a concrete follow-up is assigned.
- Support/FTUE should stay quiet unless QA/HCI reports assistant, Stop, Show Me, result explanation, invalid-command, or FTUE issues.

Next recommended task:
- UI should complete the M01 infantry-only HUD scope report.
- PM/user should approve or reject the temporary M01 infantry art package.
- QA/HCI should rerun Gate 4 after both inputs are available.

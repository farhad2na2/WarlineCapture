# PM Routing - Soldier V2 Pre-Acceptance Audits

Lane: PM
Task: Route Designer and Gameplay audits before accepting Art/Atlas v2 soldier animation package
Files changed:
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-09_pm_soldier-v2-preacceptance-audit-routing.md`
Contracts touched:
- M01 soldier animation package acceptance
- Designer visual/animation quality audit gate
- Gameplay runtime atlas/import audit gate
User-visible behavior:
- No gameplay runtime integration should proceed from the v2 soldier assets until both audits report and PM/user accepts the package.
Validation run:
- Read `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`.
- Read current Designer, Gameplay, Art/Atlas, and QA/HCI task files.
Validation result:
- Routed two pre-acceptance audits. V2 art is acknowledged as delivered but not accepted.
Known gaps:
- Need Designer recommendation on AAA mobile isometric angle/state/animation completeness and VisualLock alignment.
- Need Gameplay recommendation on scale consistency, atlas layout, manifest/import readiness, ECS atlas animator fit, and mobile performance risk.
Cross-lane impacts:
- Designer owns `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`.
- Gameplay owns `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`.
- Art/Atlas waits for audit feedback.
- QA/HCI waits until audits complete, PM/user accepts, and Gameplay provides runtime capture.
Next recommended task:
- PM should review both audit reports together and then either accept v2 for Gameplay integration or route targeted Art/Atlas fixes.

# PM Global Anti-Idle Heartbeat Rule

Lane: PM

Task: Make active-lane idling a formal heartbeat blocker across PM and lane agents.

Files changed:
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/gameplay_heartbeat.md`
- `Design/AgentTasks/ui_heartbeat.md`
- `Design/AgentTasks/art-atlas_heartbeat.md`
- `Design/AgentTasks/designer_heartbeat.md`
- `Design/AgentTasks/qa-hci_heartbeat.md`
- `Design/AgentTasks/support-ftue_heartbeat.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/README.md`
- `Design/AgentReports/2026-05-08_pm_global-anti-idle-heartbeat-rule.md`

Contracts touched:
- PM/lane heartbeat operating contract.
- No runtime implementation contract changed.

User-visible behavior:
- No runtime behavior changed.
- PM/user should now be notified when a lane is active but silent instead of the project appearing idle.

Validation run:
- Reviewed existing heartbeat and auto-continue rules.
- Updated PM heartbeat and all lane heartbeat files with an explicit anti-idle rule.
- Updated the shared auto-continue protocol and task README.

Validation result:
- Active lanes must now either advance work, write the expected handoff, or write a blocker report with exact failed command, workspace, log path, missing dependency, and unblock owner.
- PM must treat active-lane silence as a coordination blocker and notify in-thread instead of sending `DONT_NOTIFY`.

Known gaps:
- This rule cannot force an external agent to finish work, but it prevents silent waiting from being treated as normal progress.
- PM still needs to enforce the rule on every heartbeat.

Cross-lane impacts:
- Gameplay, UI, Art/Atlas, Designer, QA/HCI, and Support/FTUE all get the same active-lane anti-idle expectation.
- PM owns escalation and user notification when the rule is violated.

Next recommended task:
- Continue monitoring current QA/HCI active rerun. If no rerun or blocker report appears by the next heartbeat, PM should notify the user and route the blocker immediately.

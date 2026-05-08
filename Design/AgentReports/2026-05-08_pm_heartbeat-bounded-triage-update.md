# PM Heartbeat Bounded Triage Update

## Lane

PM

## Task

Update PM heartbeat behavior after the user flagged that PM was thinking too long during routine heartbeats.

## Files changed

- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentReports/2026-05-08_pm_heartbeat-bounded-triage-update.md`

## Contracts touched

- PM heartbeat must use a bounded triage:
  - check active `*_current.md` files,
  - check explicitly expected reports,
  - stop unless a report landed, a blocker appears, or a user decision is needed.
- Normal heartbeat target is under two minutes.
- Broad report/repo audits are not allowed during an active implementation/rejection gate unless all active lanes are waiting and no expected implementation/QA report is pending.

## User-visible behavior

PM should stop spending long turns on routine heartbeats. It should notify only for user decisions, blockers, or active-lane idle risks.

## Validation run

- Read `Design/AgentTasks/pm_heartbeat.md`.
- Checked current expected active reports:
  - `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`
  - `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
- Both expected reports are still missing.

## Validation result

Accepted PM process update.

## Known gaps

- Gameplay implementation report is still pending.
- QA/HCI regression-gate report is still pending.
- No user decision is needed right now.

## Cross-lane impacts

- PM should not broaden heartbeat checks while Gameplay and QA/HCI are the active next owners.
- QA/HCI already has a direct PM message to write its regression gate now.
- Gameplay already has the Art/Atlas, Designer, and UI inputs it needs.

## Next recommended task

- Gameplay: `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
- QA/HCI: `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

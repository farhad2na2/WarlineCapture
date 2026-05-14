# PM Designer Status Source Correction

Date: 2026-05-14
Lane: PM
Task: Correct Designer active-status source for M01 step-by-step gameplay spec

## Routing Decision

Current Designer status: active
Owner of next action: Designer
Required Designer output: `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

This PM correction supersedes every previous Designer waiting state and every report summary that says Gameplay owns the next Designer action.

## Reason

The active M01 flow needs Designer to produce the design-owned step-by-step gameplay specification before Art/Atlas creates mockup images. Gameplay and QA/HCI remain blocked until Art/Atlas produces mockups from the Designer spec and the user approves them.

## Designer Continue Instruction

On `continue`, Designer must read:

- `Design/AgentTasks/designer_heartbeat.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`

If `Design/AgentTasks/designer_current.md` says `Status: active`, Designer must not report waiting on Gameplay, missing Designer-routed handoff, missing approval need, or missing blocker. Designer must create/update the required report or write the blocker at that same path with the exact missing source or contradiction and unblock owner.

## Cross-Lane Impacts

- Designer: active owner now.
- Art/Atlas: next lane after Designer report is delivered.
- Gameplay: blocked until user-approved mockup images exist.
- QA/HCI: blocked until implementation exists.

## PM Follow-Up

If Designer still reports waiting, treat it as a Designer heartbeat compliance failure, not a Gameplay dependency.

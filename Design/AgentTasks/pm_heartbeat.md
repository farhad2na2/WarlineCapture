# PM Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/*_current.md` as the only source of current lane priorities.

## On Every Heartbeat

- Check `Design/AgentReports/` for new lane handoffs or blocker reports.
- Check each new report against the standard WarlineCapture handoff format:
  - Lane
  - Task
  - Files changed
  - Contracts touched
  - User-visible behavior
  - Validation run
  - Validation result
  - Known gaps
  - Cross-lane impacts
  - Next recommended task
- Assess each new handoff as accepted, needs fixes, or blocked.
- Identify cross-lane impacts and whether PM/user needs to make a decision.
- Keep Gate 4 blocked unless QA/HCI proves the public M01 golden path and current lane-specific blockers are resolved.
- Write a PM review report under `Design/AgentReports/` only when a concrete acceptance, issue, blocker, routing change, or user decision is found.
- Notify in-thread only when PM/user attention is needed; otherwise stay quiet.

## Guardrails

- Do not assume a specific current task outside the lane current-task files.
- Do not modify source docs or task files unless explicitly asked in the thread or required for PM routing.

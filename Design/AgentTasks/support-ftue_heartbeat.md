# Support/FTUE Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/support-ftue_current.md` as the only source of current Support/FTUE priorities.

## On Every Heartbeat

- Read `Design/AgentTasks/support-ftue_current.md`.
- Check `Design/AgentReports/` for new Support/FTUE-relevant handoffs or blocker reports.
- Assess whether any concrete assistant, Stop, Show Me, result explanation, invalid-command, or FTUE issue is assigned.
- Continue the current Support/FTUE task if actionable.
- Anti-idle rule: if Support/FTUE is `Status: active`, every heartbeat must either advance the task, write the expected handoff, or write a blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention is needed, a blocker appears, or the Support/FTUE handoff is ready.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/support-ftue_current.md`.
- Do not modify source docs or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to Support/FTUE for a named file set.

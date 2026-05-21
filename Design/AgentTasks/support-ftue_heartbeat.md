# Support/FTUE Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/support-ftue_current.md` as the only source of current Support/FTUE priorities.

## 2026-05-22 Reset Guard

If `Design/AgentTasks/support-ftue_current.md` says `Status: held`, stop. Do not scan `Design/AgentReports/` for new work, do not edit support behavior, do not write a report, and do not route another lane. Respond only that Support/FTUE is held for the 3D fresh-start reset and waiting for PM/user dispatch.

## On Every Heartbeat

- Read `Design/AgentTasks/support-ftue_current.md`.
- Check `Design/AgentReports/` for new Support/FTUE-relevant handoffs or blocker reports.
- Assess whether any concrete assistant, Stop, Show Me, result explanation, invalid-command, or FTUE issue is assigned.
- Continue the current Support/FTUE task if actionable.
- Anti-idle rule: if Support/FTUE is `Status: active`, every heartbeat must either advance the task, write the expected handoff, or write a blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
- Unity licensing workaround is mandatory when PM assigns Support/FTUE Unity validation: follow `Design/AgentTasks/AUTO_CONTINUE.md` and use the PM-assigned workspace with Codex escalation/out-of-sandbox execution before reporting a licensing blocker.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention is needed, a blocker appears, or the Support/FTUE handoff is ready.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/support-ftue_current.md`.
- Do not modify source docs or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to Support/FTUE for a named file set.

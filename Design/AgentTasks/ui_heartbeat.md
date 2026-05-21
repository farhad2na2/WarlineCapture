# UI Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/ui_current.md` as the only source of current UI priorities.

## 2026-05-22 Reset Guard

If `Design/AgentTasks/ui_current.md` says `Status: held`, stop. Do not scan `Design/AgentReports/` for new work, do not run Unity, do not write a report, and do not route another lane. Respond only that UI is held for the 3D fresh-start reset and waiting for PM/user dispatch.

## On Every Heartbeat

- Read `Design/AgentTasks/ui_current.md`.
- Check `Design/AgentReports/` for new UI-relevant handoffs or blocker reports.
- Assess new relevant handoffs as accepted, needs fixes, or blocked.
- Continue the current UI task if actionable.
- Anti-idle rule: if UI is `Status: active`, every heartbeat must either advance the task, write the expected handoff, or write a blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
- Unity licensing workaround is mandatory: if required Unity validation/capture hits `LicenseClient-farhad`, unsupported protocol, reconnect, or timeout loops before starting, follow `Design/AgentTasks/AUTO_CONTINUE.md` and rerun in `/Users/farhad/Projects/WarlineCapture-CodexUnity2` with Codex escalation/out-of-sandbox execution before reporting blocked.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention is needed, a blocker appears, or the UI handoff is ready for PM/QA.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/ui_current.md`.
- Do not modify source docs or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to UI for a named file set.

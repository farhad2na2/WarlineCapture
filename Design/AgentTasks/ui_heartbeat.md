# UI Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/ui_current.md` as the only source of current UI priorities.

## On Every Heartbeat

- Read `Design/AgentTasks/ui_current.md`.
- Check `Design/AgentReports/` for new UI-relevant handoffs or blocker reports.
- Assess new relevant handoffs as accepted, needs fixes, or blocked.
- Continue the current UI task if actionable.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention is needed, a blocker appears, or the UI handoff is ready for PM/QA.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/ui_current.md`.
- Do not modify source docs or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to UI for a named file set.

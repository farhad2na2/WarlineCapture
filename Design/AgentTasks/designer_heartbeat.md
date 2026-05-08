# Designer Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/designer_current.md` as the only source of current Designer priorities.

## On Every Heartbeat

- Read `Design/AgentTasks/designer_current.md`.
- Check `Design/AgentReports/` for new Designer-relevant handoffs or blocker reports.
- Assess new relevant handoffs as accepted, needs fixes, or blocked.
- Continue the current Designer task if actionable.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention is needed, a blocker appears, or the Designer handoff is ready for PM review.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/designer_current.md`.
- Do not modify source/runtime files, Unity prefabs, captures, or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to Designer for a named file set.

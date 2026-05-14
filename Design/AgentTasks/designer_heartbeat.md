# Designer Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/designer_current.md` as the only source of current Designer priorities.

If `Design/AgentTasks/designer_current.md` says `Status: active`, the current Designer status answer is:

```text
Designer is active. Designer owns the next action. The required output is Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md.
```

Never answer that Designer is waiting, Gameplay owns the next action, no Designer-routed handoff exists, no approval need exists, or no blocker was found while `designer_current.md` remains active.

## Current Continue Rule

If `Design/AgentTasks/designer_current.md` says `Status: active`, the Designer task is already dispatched by PM/user. Do not report waiting because no separate handoff exists, because older or newer reports mention Gameplay/runtime work, or because no Designer report exists yet. `Design/AgentReports/` files are context only; they do not override `designer_current.md`.

Do not check `Design/AgentReports/` before deciding current Designer status. Check reports only for source conflicts after starting the active task and creating or updating the required report.

For the current M01 assignment, `continue` means read `Design/AgentTasks/designer_pm_message.md` when present, then create or update `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md` using the exact section order in `designer_current.md`. If blocked, write the blocker at that same expected report path and include the missing file or contradiction, command attempted if any, workspace, log path if any, missing dependency, and unblock owner.

## On Every Heartbeat

- Read `Design/AgentTasks/designer_current.md` and, when present, `Design/AgentTasks/designer_pm_message.md`.
- Check `Design/AgentReports/` for new Designer-relevant handoffs or blocker reports.
- Assess new relevant handoffs as accepted, needs fixes, or blocked.
- If `designer_current.md` is active, continue that task before treating old PM reports as current routing.
- Continue the current Designer task if actionable.
- Anti-idle rule: if Designer is `Status: active`, every heartbeat must either advance the task, write the expected handoff, or write a blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if PM/user attention is needed, a blocker appears, or the Designer handoff is ready for PM review.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/designer_current.md`.
- Do not modify source/runtime files, Unity prefabs, captures, or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to Designer for a named file set.

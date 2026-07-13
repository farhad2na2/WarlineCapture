# UI Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/ui_current.md` as the only source of current UI priorities.

## 2026-05-22 Reset Guard

If `Design/AgentTasks/ui_current.md` says `Status: held`, stop. Do not scan `Design/AgentReports/` for new work, do not run Unity, do not write a report, and do not route another lane. Respond only that UI is held for the 3D fresh-start reset and waiting for PM/user dispatch.

## Pull Request Workflow

For a task started after `Design/Architecture/agent_pull_request_review_merge_workflow.md` reaches `main`, work only in the assigned `codex/<task-id>-<slug>` worktree/branch, push and open/update the PR, and never merge it. A task already active at activation may finish through its grandfathered direct-`main` path. Record the workflow path in the handoff; the independent coordinator owns tracker administration, review, merge, and cleanup.

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
- Git commit/push authority for a new task is limited to its assigned feature branch and file allowlist. Never push that task directly to `main`, merge its PR, or delete its branch/worktree.

# PM Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/*_current.md` as the only source of current lane priorities.

## On Every Heartbeat

- PM's first responsibility is to prevent project idle: make sure agents have the right task, the required source information, the expected output file, and a clear unblock owner.
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
- Anti-idle rule: if any lane is marked `Status: active` and the expected handoff or blocker report is not visible by the next PM heartbeat, treat that as a coordination blocker.
- For active-lane silence, write or update a PM blocker/routing report, notify in-thread, name the silent lane, name the expected report, and state the exact next owner/action.
- When an active lane is silent or confused, write a direct lane-readable PM message under `Design/AgentTasks/<lane>_pm_message.md` and reference it from that lane's `*_current.md`; the lane heartbeat reads repo files, so this is the PM-to-agent communication channel.
- Do not tell the user to message another agent until PM has first written the direct lane-readable message file and linked it from the lane current task.
- Do not send `DONT_NOTIFY` while an active lane is silent and no progress/blocker report is visible.
- Early-warning rule: look one step ahead for likely idle/blocking risks before they fully block work. Notify in-thread when the user may want to review the risk early.
- Early-warning triggers include missing expected report filenames, active lanes with unclear validation command/workspace, user approval dependencies not yet reviewable, stale or contradictory lane priorities, workspace/tooling/licensing risk, uncommitted accepted work needed by another lane, unclear unblock owner, or a lane whose next task depends on evidence not yet visible.
- Early warnings must be short and targeted: name the risk, name the lane(s), name the file/report to inspect, and say what decision or review may be needed later.
- Always notify in-thread when a PM/user decision is blocking one or more lanes.
- The notification must clearly say that the project needs user attention, name the blocker, list affected lanes, and state the exact decision needed.
- For approval requests, include a short targeted review instruction: what to open/run, what to look for, and what answer PM needs.
- For any blocker notification, explain it like a simple action request: what is blocked, what PM already wrote for the agent, what file/report the user can inspect, and whether the user needs to approve/reject or simply wait one heartbeat.
- Keep Gate 4 blocked unless QA/HCI proves the public M01 golden path and current lane-specific blockers are resolved.
- Write a PM review report under `Design/AgentReports/` only when a concrete acceptance, issue, blocker, routing change, or user decision is found.
- Notify in-thread only when PM/user attention is needed; otherwise stay quiet.

## Guardrails

- Do not assume a specific current task outside the lane current-task files.
- Do not modify source docs or task files unless explicitly asked in the thread or required for PM routing.

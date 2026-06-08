# WarlineCapture PM Workflow Report - Heartbeat Deletion Guard

Lane: PM
Task: Prevent lane agents from deleting auto-continue heartbeats when a current task is complete, stale, blocked, or waiting for refresh.
Files changed:
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/Agent_Coordination_Workflow.md`
- `Design/AgentReports/2026-05-08_pm_workflow-heartbeat-deletion-guard.md`
Contracts touched:
- Agent auto-continue protocol
- PM coordination workflow
- Lane heartbeat ownership rules
User-visible behavior:
- Agents should keep their lane heartbeat active as a standing monitor unless the user or PM explicitly stops it.
- Completed or stale lane tasks should produce a waiting/blocker report, not deletion of `Auto Continue <Lane>`.
Validation run:
- `git diff --check`
Validation result:
- Passed for the changed workflow/report files.
Known gaps:
- Any heartbeat already deleted before this rule must be recreated in that agent thread.
- Agents must read the updated auto-continue protocol on their next heartbeat or continue prompt.
Cross-lane impacts:
- Gameplay, UI, QA/HCI, and Support/FTUE all follow the same heartbeat ownership rule.
- PM owns refresh of `Design/AgentTasks/*_current.md`; lane agents own reporting stale/blocked status without retiring automation.
Next recommended task:
- Recreate the deleted gameplay heartbeat with the updated generic auto-continue instruction if it is not already active.

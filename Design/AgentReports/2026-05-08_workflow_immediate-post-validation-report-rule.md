Lane:
Workflow

Task:
Add an immediate post-validation reporting rule so agents do not finish validation and then sit idle without a handoff.

Files changed:
- `Design/Agent_Coordination_Workflow.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/README.md`
- `Design/AgentReports/2026-05-08_workflow_immediate-post-validation-report-rule.md`

Contracts touched:
Agent coordination workflow, auto-continue heartbeat protocol, and task-board close-out rules.

User-visible behavior:
Agents reading the shared workflow or `AUTO_CONTINUE.md` now have an explicit rule to write or update the matching `Design/AgentReports/` report immediately after validation, capture, build, log scan, failed validation, or tool-approval blocker.

Validation run:
Not run.

Validation result:
Not run; documentation/workflow-only change.

Known gaps:
Existing agent threads must read the updated files on their next `continue` or heartbeat. This does not force an already-open agent to comply until it reads the updated protocol.

Cross-lane impacts:
All lanes should treat unreported validated work as in-progress. PM should not accept code/capture artifacts that lack a current report and validation result.

Next recommended task:
On each lane's next heartbeat or `continue`, the agent should read `Design/AgentTasks/AUTO_CONTINUE.md` and follow the new post-validation report timing rule.

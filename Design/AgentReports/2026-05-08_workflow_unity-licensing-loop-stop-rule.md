Lane:
Workflow

Task:
Add a Unity licensing-loop stop rule so agents do not repeatedly ask for approval while validation is stuck before tests start.

Files changed:
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentReports/2026-05-08_workflow_unity-licensing-loop-stop-rule.md`

Contracts touched:
Agent validation-permission workflow and auto-continue protocol.

User-visible behavior:
Agents now have explicit instructions to make at most one retry in another WarlineCapture Unity workspace when Unity enters licensing reconnect or unsupported-protocol loops before tests start. If the retry also stalls, they should stop the stuck Unity process, report validation as blocked with the exact command/log path, and wait for Unity licensing to become healthy instead of retrying indefinitely.

Validation run:
Not run.

Validation result:
Not run; documentation/workflow-only change.

Known gaps:
Codex may still require tool approval to stop a stuck process in a separate agent thread. The new rule clarifies that this is tool permission for cleanup, not a product decision.

Cross-lane impacts:
All lanes should stop treating repeated Unity licensing loops as a validation question. They should record the blocked validation result immediately and avoid repeated retries until licensing is healthy.

Next recommended task:
Support/FTUE should stop the currently stuck Unity validation process if it is still running, update its reason-code report as validation blocked by Unity licensing, and wait for a clean Unity validation window.

# PM Early Idle Risk Warning Rule

Lane: PM

Task: Add early warnings for likely future idle/blocking risks.

Files changed:
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/pm_design-audit.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/README.md`
- `Design/AgentReports/2026-05-08_pm_early-idle-risk-warning-rule.md`

Contracts touched:
- PM heartbeat and audit operating contract.
- No runtime implementation contract changed.

User-visible behavior:
- No runtime behavior changed.
- PM should now notify the user earlier when a lane is not blocked yet but is likely to become idle or blocked.

Validation run:
- Reviewed the existing anti-idle heartbeat rules.
- Added early-warning triggers to the PM heartbeat, PM design audit, shared auto-continue protocol, and agent task README.

Validation result:
- PM must now look one step ahead for likely future idle/blocking risks.
- Early warnings should name the risk, affected lane, file/report to inspect, and likely decision or review point.
- Early warning triggers include missing report names, unclear validation/workspace, hidden user approval dependencies, stale lane priorities, tooling/licensing risk, uncommitted accepted work needed by another lane, and unclear unblock owner.

Known gaps:
- This rule improves PM escalation, but PM still needs to apply it every heartbeat.

Cross-lane impacts:
- All lanes remain under the existing active-lane anti-idle rule.
- PM now owns proactive warnings before an issue becomes a hard blocker.

Next recommended task:
- On each PM heartbeat, check active lanes for likely future idle risks before deciding `DONT_NOTIFY`.

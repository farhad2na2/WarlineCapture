# QA/HCI Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/qa-hci_current.md` as the only source of current QA/HCI priorities.

## On Every Heartbeat

- Read `Design/AgentTasks/qa-hci_current.md`.
- Check `Design/AgentReports/` for new QA/HCI-relevant handoffs or blocker reports.
- Assess whether QA/HCI is still waiting, ready to rerun, needs fixes, or blocked.
- Continue the current QA/HCI task only when required input reports exist.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if Gate 4 is ready for user review, blocked, or PM/user attention is needed.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/qa-hci_current.md`.
- Do not modify source docs or other lane task files unless explicitly asked.

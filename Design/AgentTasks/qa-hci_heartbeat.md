# QA/HCI Heartbeat

## Source Of Truth

Treat `Design/AgentTasks/qa-hci_current.md` as the only source of current QA/HCI priorities.

## 2026-05-22 Reset Guard

If `Design/AgentTasks/qa-hci_current.md` says `Status: held`, stop. Do not scan `Design/AgentReports/` for new work, do not run validation, do not write a report, and do not route another lane. Respond only that QA/HCI is held for the 3D fresh-start reset and waiting for PM/user dispatch.

## On Every Heartbeat

- Read `Design/AgentTasks/qa-hci_current.md`.
- Check `Design/AgentReports/` for new QA/HCI-relevant handoffs or blocker reports.
- Assess whether QA/HCI is still waiting, ready to rerun, needs fixes, or blocked.
- Continue the current QA/HCI task only when required input reports exist.
- Anti-idle rule: if QA/HCI is `Status: active`, every heartbeat must either advance the task, write the expected handoff, or write a blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
- Unity licensing workaround is mandatory: if required Unity validation/capture hits `LicenseClient-farhad`, unsupported protocol, reconnect, or timeout loops before starting, follow `Design/AgentTasks/AUTO_CONTINUE.md` and rerun in `/Users/farhad/Projects/WarlineCapture-CodexUnity3` with Codex escalation/out-of-sandbox execution before reporting blocked.
- Write completion, blocker, or approval-needed reports under `Design/AgentReports/` using the standard WarlineCapture handoff format.
- Notify in-thread only if Gate 4 is ready for user review, blocked, or PM/user attention is needed.

## Guardrails

- Do not assume a task outside `Design/AgentTasks/qa-hci_current.md`.
- Do not modify source docs or other lane task files unless explicitly asked.
- Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to QA/HCI for a named file set.

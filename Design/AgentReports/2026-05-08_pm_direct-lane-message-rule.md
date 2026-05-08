# PM Direct Lane Message Rule

Lane: PM

Task: Add explicit PM-to-agent communication rule for silent or confused lanes.

Files changed:
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/README.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentReports/2026-05-08_pm_direct-lane-message-rule.md`

Contracts touched:
- PM/lane heartbeat operating contract.
- No runtime implementation contract changed.

User-visible behavior:
- No runtime behavior changed.
- PM should no longer tell the user to message another agent before PM has first written a direct lane-readable message into the repo.

Validation run:
- Updated PM heartbeat rules.
- Updated shared agent task README.
- Updated shared auto-continue protocol.

Validation result:
- PM's first responsibility is now explicitly documented as preventing idle by making sure agents have the right task, source information, expected output file, and unblock owner.
- When a lane is silent or confused, PM must write `Design/AgentTasks/<lane>_pm_message.md` and link it from that lane's `*_current.md`.
- User intervention is only requested if the lane heartbeat ignores the direct PM message.

Known gaps:
- PM still needs to enforce this on every heartbeat.

Cross-lane impacts:
- All lane heartbeat agents now have a standard place to read direct PM instructions.
- QA/HCI currently has `Design/AgentTasks/qa-hci_pm_message.md` linked from `Design/AgentTasks/qa-hci_current.md`.

Next recommended task:
- Wait one heartbeat for QA/HCI to act on `Design/AgentTasks/qa-hci_pm_message.md`; if ignored, notify user that the QA/HCI heartbeat itself appears broken.

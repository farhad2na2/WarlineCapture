Lane:
PM

Task:
Record QA/HCI's Unity batchmode licensing solution and propagate it to all active lane instructions.

Files changed:
- `Design/Agent_Coordination_Workflow.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-08_pm_unity-batchmode-licensing-escalation-rule.md`

Contracts touched:
- Unity workspace ownership remains unchanged: Gameplay uses `WarlineCapture-CodexUnity`, UI uses `WarlineCapture-CodexUnity2`, QA/HCI uses `WarlineCapture-CodexUnity3`, and Support/FTUE uses a PM-assigned workspace only when needed.
- Unity batchmode licensing rule updated: if sandboxed Codex batchmode cannot reach `LicenseClient-farhad` and stalls before tests start, agents should rerun the same required command with Codex escalation/out-of-sandbox execution in their assigned workspace.

User-visible behavior:
No runtime game behavior changed. Agent validation workflow should stop treating this licensing issue as a broken Unity project or reason to switch workspaces.

Validation run:
- Read QA/HCI's latest licensing resolution in `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`.
- Updated shared workflow and active lane task files.
- `git diff --check`

Validation result:
`git diff --check` passed. The documented solution is that escalated/out-of-sandbox Unity batchmode successfully accessed Unity licensing, and QA/HCI then ran `Chapter01M01PlayModeValidationTests` 5/5 from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.

Known gaps:
- Agents may still need a Codex tool approval click for escalated Unity batchmode commands unless the narrow Unity batchmode permission is remembered.
- Remaining Gate 4 blockers are separate from licensing, especially Gameplay ECS world-source proof/fix.

Cross-lane impacts:
- Gameplay, UI, and QA/HCI should keep using their assigned Unity workspaces and request/run escalated batchmode when licensing loops appear.
- Support/FTUE should use the same rule only when PM assigns Unity validation.

Next recommended task:
Agents should continue their active lane tasks using escalated/out-of-sandbox Unity batchmode for required validation when sandbox licensing loops appear.

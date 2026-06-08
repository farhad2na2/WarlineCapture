Status: advisory
Topic:
Dedicated Unity workspace priority per agent lane

Docs updated:
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/Agent_Coordination_Workflow.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`

Decision:
Use fixed Unity workspace priority by lane to avoid agents sharing one Unity instance, locking each other's Library, or repeatedly asking for permission after collisions.

Workspace map:
- Gameplay: `/Users/farhad/Projects/WarlineCapture-CodexUnity`
- UI: `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- QA/HCI: `/Users/farhad/Projects/WarlineCapture-CodexUnity3`
- Support/FTUE: no default Unity workspace; PM assigns one only if Unity validation becomes necessary.

Rules:
- Each lane should run required focused Unity validation only in its assigned workspace.
- A lane must not take another active lane's primary workspace just because it is available.
- If the assigned workspace is locked by a stale process, the lane may stop only that stale process when safe, then retry once.
- If the retry stalls in licensing/reconnect loops, the lane must stop, report `Validation result: blocked`, include the command/log path, and wait for PM/user to confirm Unity health or reassign a temporary workspace.
- Workspace switching is a PM coordination decision, not a lane-local optimization.

Affected lanes:
Gameplay, UI, QA/HCI, Support/FTUE, PM

Needs user decision:
No.

Next task update needed:
Done.

# WarlineCapture Agent Tasks

This folder is the PM-controlled task board for active WarlineCapture agents.

Agents should read their lane file when the user says `continue`:

- Critical path gate: `Design/AgentTasks/M01_CRITICAL_PATH.md`
- Gameplay agent: `Design/AgentTasks/gameplay_current.md`
- UI agent: `Design/AgentTasks/ui_current.md`
- Art/Atlas agent: `Design/AgentTasks/art-atlas_current.md`
- Designer agent: `Design/AgentTasks/designer_current.md`
- Support/FTUE agent: `Design/AgentTasks/support-ftue_current.md`
- QA/HCI agent: `Design/AgentTasks/qa-hci_current.md`
- PM assistant idle audit: `Design/AgentTasks/pm_design-audit.md`

For agents that should wake up automatically, keep the automation prompt short and route behavior through the lane heartbeat file:

- PM: `Read Design/AgentTasks/pm_heartbeat.md and follow it. Treat Design/AgentTasks/*_current.md as the only source of current lane priorities.`
- Gameplay: `Read Design/AgentTasks/gameplay_heartbeat.md and follow it. Treat Design/AgentTasks/gameplay_current.md as the only source of current Gameplay priorities.`
- UI: `Read Design/AgentTasks/ui_heartbeat.md and follow it. Treat Design/AgentTasks/ui_current.md as the only source of current UI priorities.`
- Art/Atlas: `Read Design/AgentTasks/art-atlas_heartbeat.md and follow it. Treat Design/AgentTasks/art-atlas_current.md as the only source of current Art/Atlas priorities.`
- Designer: `Read Design/AgentTasks/designer_heartbeat.md and follow it. Treat Design/AgentTasks/designer_current.md as the only source of current Designer priorities.`
- QA/HCI: `Read Design/AgentTasks/qa-hci_heartbeat.md and follow it. Treat Design/AgentTasks/qa-hci_current.md as the only source of current QA/HCI priorities.`
- Support/FTUE: `Read Design/AgentTasks/support-ftue_heartbeat.md and follow it. Treat Design/AgentTasks/support-ftue_current.md as the only source of current Support/FTUE priorities.`

`Design/AgentTasks/AUTO_CONTINUE.md` remains the shared detailed protocol for lane monitors and validation/reporting rules.

Rules:

- Treat the lane file as the current assignment unless the user gives a newer direct instruction.
- Before starting new work, read `Design/AgentTasks/M01_CRITICAL_PATH.md` and confirm the task advances the current M01 gate.
- Do not begin M02-M05 implementation, broad polish, or optional systems until the PM assistant marks the M01 critical path ready to expand.
- Do not edit another lane's task file.
- Designer may propose documentation restructuring, but PM owns final acceptance of cross-lane source-of-truth changes.
- Final target mockups and visual locks must meet the AAA WarlineCapture quality gate in `Design/WarlineCapture_Agent_Coordination_Workflow.md`; do not accept state boards, wireframes, generic placeholders, or off-style mockups as final target locks.
- Do not add scene-wide lookup patterns such as `Object.Find*`, `Resources.FindObjectsOfTypeAll`, `FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, name/tag lookup, obsolete scene-search overloads, or `FindObjectsSortMode` usage in production gameplay/UI/FTUE code, editor validation builders, or Unity tests. Use serialized references, explicit runtime services, registries, typed provider APIs, known bootstrap/context references, loaded-scene root references, ECS component objects, or task-owned fixtures instead.
- When finished, write a completion report under `Design/AgentReports/` using `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
- After any validation, capture, build, log scan, or failed validation attempt, immediately write or update the matching report before starting new work, reporting idle/waiting, or handing off to another lane.
- Active lanes must not stay silent across heartbeats. If a lane is `Status: active`, each heartbeat must produce visible progress, the expected handoff, or a blocker report with the exact failed command/workspace/log/dependency and unblock owner. PM treats active-lane silence as a coordination blocker and notifies the user.
- If a lane is silent, PM communicates through the repo first: write `Design/AgentTasks/<lane>_pm_message.md`, link it from `<lane>_current.md`, and only ask the user to intervene if the lane heartbeat still ignores that direct message.
- PM should also warn early about likely future idle risks: missing report names, unclear validation/workspace, hidden approval dependencies, stale lane priorities, tooling/licensing risk, uncommitted accepted work needed by another lane, or unclear unblock ownership.
- The PM assistant updates these task files after reviewing reports and cross-lane dependencies.

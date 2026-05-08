# WarlineCapture Agent Tasks

This folder is the PM-controlled task board for active WarlineCapture agents.

Agents should read their lane file when the user says `continue`:

- Critical path gate: `Design/AgentTasks/M01_CRITICAL_PATH.md`
- Gameplay agent: `Design/AgentTasks/gameplay_current.md`
- UI agent: `Design/AgentTasks/ui_current.md`
- Support/FTUE agent: `Design/AgentTasks/support-ftue_current.md`
- QA/HCI agent: `Design/AgentTasks/qa-hci_current.md`
- PM assistant idle audit: `Design/AgentTasks/pm_design-audit.md`

For agents that should wake up automatically every 15 minutes, use:

- `Design/AgentTasks/AUTO_CONTINUE.md`

Rules:

- Treat the lane file as the current assignment unless the user gives a newer direct instruction.
- Before starting new work, read `Design/AgentTasks/M01_CRITICAL_PATH.md` and confirm the task advances the current M01 gate.
- Do not begin M02-M05 implementation, broad polish, or optional systems until the PM assistant marks the M01 critical path ready to expand.
- Do not edit another lane's task file.
- Final target mockups and visual locks must meet the AAA WarlineCapture quality gate in `Design/WarlineCapture_Agent_Coordination_Workflow.md`; do not accept state boards, wireframes, generic placeholders, or off-style mockups as final target locks.
- Do not add scene-wide lookup patterns such as `Object.Find*`, `Resources.FindObjectsOfTypeAll`, `FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, name/tag lookup, obsolete scene-search overloads, or `FindObjectsSortMode` usage in production gameplay/UI/FTUE code, editor validation builders, or Unity tests. Use serialized references, explicit runtime services, registries, typed provider APIs, known bootstrap/context references, loaded-scene root references, ECS component objects, or task-owned fixtures instead.
- When finished, write a completion report under `Design/AgentReports/` using `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
- After any validation, capture, build, log scan, or failed validation attempt, immediately write or update the matching report before starting new work, reporting idle/waiting, or handing off to another lane.
- The PM assistant updates these task files after reviewing reports and cross-lane dependencies.

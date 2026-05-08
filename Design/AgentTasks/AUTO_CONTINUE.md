# WarlineCapture Agent Auto-Continue Protocol

Use this when an agent should keep checking for work without the user manually typing `continue`.

## Recommended Heartbeat Prompt Pattern

Keep each automation prompt short and load behavior from the repo:

```text
Read Design/AgentTasks/<lane>_heartbeat.md and follow it. Treat Design/AgentTasks/<lane>_current.md as the only source of current <Lane> priorities.
```

For PM:

```text
Read Design/AgentTasks/pm_heartbeat.md and follow it. Treat Design/AgentTasks/*_current.md as the only source of current lane priorities.
```

## Agent Instruction

Set a recurring heartbeat/reminder in your own thread.

On each heartbeat:

1. Read your lane heartbeat file:
   - PM: `Design/AgentTasks/pm_heartbeat.md`
   - Gameplay: `Design/AgentTasks/gameplay_heartbeat.md`
   - UI: `Design/AgentTasks/ui_heartbeat.md`
   - Art/Atlas: `Design/AgentTasks/art-atlas_heartbeat.md`
   - Designer: `Design/AgentTasks/designer_heartbeat.md`
   - QA/HCI: `Design/AgentTasks/qa-hci_heartbeat.md`
   - Support/FTUE: `Design/AgentTasks/support-ftue_heartbeat.md`
2. Read the critical path gate:
   - `Design/AgentTasks/M01_CRITICAL_PATH.md`
3. Read your lane task file:
   - Gameplay: `Design/AgentTasks/gameplay_current.md`
   - UI: `Design/AgentTasks/ui_current.md`
   - Art/Atlas: `Design/AgentTasks/art-atlas_current.md`
   - Designer: `Design/AgentTasks/designer_current.md`
   - Support/FTUE: `Design/AgentTasks/support-ftue_current.md`
   - QA/HCI: `Design/AgentTasks/qa-hci_current.md`
4. Check whether your current task is still active and advances the M01 critical path.
5. If your task is active and not completed, continue it.
6. If your task is active and you cannot make visible progress before the next heartbeat, immediately write the required blocker report with the exact failed command, workspace, log path, missing dependency, and unblock owner.
7. If you have changed files, generated captures, or completed/attempted validation and no matching report exists under `Design/AgentReports/`, stop and write the report before doing anything else.
8. If validation just finished, immediately update the required report with the command/checks, output paths, pass/fail result, known gaps, cross-lane impacts, and next recommended task.
9. If validation failed, was interrupted, or could not run because Codex/tool sandbox approval is required, immediately write the report with the exact command/log path and blocker owner.
10. If your task is completed, verify that you wrote the required report under `Design/AgentReports/`.
11. Before reporting `waiting`, `blocked`, or `idle`, identify who owns the next concrete deliverable.
12. If your lane owns the next deliverable, do not wait. Continue the task or write the exact technical blocker in your required report file.
13. If another lane owns the next deliverable, report the blocker clearly and wait.
14. If no active lane task exists, the task is blocked by another lane, or the next task would drift outside the critical path, report the blocker clearly and wait.

Do not start a task from another lane. Do not invent new tasks. Do not modify `Design/AgentTasks/` unless explicitly assigned by the PM assistant or user.

Do not run `git add`, `git commit`, or `git push` unless PM/user explicitly assigns that git operation to your lane for a named file set. Default lane behavior is to write the required report and leave changed files for PM review and commit/push.

## M01 Golden Playthrough Rule

The current product goal is one playable M01 infantry mission, not disconnected proof artifacts.

No lane may claim Gate 4 readiness, M01 readiness, or M02 unlock based only on screenshots, safe-area matrices, route wiring, isolated unit tests, or editor-only scenes.

Before Gate 4 can pass, the public player path must support this golden playthrough:

`Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy destroyed/neutralized -> objective/result popup`.

Pass/fail requirements:

- one player infantry/rifle squad type only: `unit.player.rifle_squad_01`
- one enemy infantry/patrol type only: `unit.enemy.patrol_01`
- no player-controlled vehicles, vehicle production, transport, base/build mechanics, or additional player unit types
- player survives long enough to understand and issue the first move order
- movement uses tactical walkable/pathing metadata, not a visual-only jump
- blocked/unreachable cells are rejected with visible feedback
- visible units are ECS runtime entities with animated atlas-backed idle/move/attack/death or destroyed states
- no legacy 3D combat units or design-target SpriteRenderer proxies appear in the public M01 path
- attack/objective/result flow is reachable
- UI/assistant supports the flow without blocking player control

If your active task does not advance this golden playthrough or remove a named blocker to it, stop and report that the lane is waiting for PM refresh.

Every report that claims progress toward M01 readiness must include a short `Golden playthrough impact` section stating which step became more playable, which step remains blocked, and whether the report is sufficient or insufficient for Gate 4.

## Heartbeat Ownership

Your heartbeat is a persistent lane monitor, not the current task itself. Do not delete, pause, disable, or stop your own heartbeat because the current lane task is complete, stale, blocked, or waiting for PM/user refresh.

Only the user or PM assistant may explicitly stop, pause, delete, or retire a lane heartbeat. If your current lane task is complete or stale, keep the heartbeat active and report:

```text
My current lane task is complete/stale and I am waiting for PM to refresh the lane file. I will keep the heartbeat active and stay quiet unless new work or a blocker appears.
```

If you believe a heartbeat should be stopped, write that recommendation in your lane report and wait for PM/user direction instead of changing the automation yourself.

## Waiting / Blocked Report Rule

Any waiting, blocked, or idle report must include:

```text
Waiting on lane:
Waiting on exact file/report/asset/command:
Owner of next action:
Can my lane still continue fallback work? yes/no
```

If `Owner of next action` is your own lane, continue instead of reporting waiting. Only report a technical blocker when you cannot perform the lane-owned task, and write that blocker to the required report file.

## Active-Lane Anti-Idle Rule

`Status: active` means the lane is expected to move. An active lane must not stay silent across heartbeats.

On every heartbeat, an active lane must do one of these:

- continue the task and produce visible progress,
- write the expected completion/handoff report,
- write a blocker report with exact command/workspace/log/dependency details and the unblock owner.

If an active lane cannot do one of these before the next heartbeat, the lane must report blocked. PM will treat active-lane silence as a coordination blocker and notify the user.

Example:

- QA/HCI can wait for UI's integrated capture matrix.
- UI cannot wait for QA/HCI before delivering UI's integrated capture matrix. UI must produce the matrix or report the UI-owned route/capture tooling blocker.

## Validation Permission

Agents are authorized to run focused Unity EditMode/PlayMode validation, prefab builders, capture builders, graphics-enabled Unity capture passes, and report-generation commands required by their current lane task.

Do not pause to ask the user whether required validation should run.

Use the dedicated Unity workspace assigned to your lane before asking the user to resolve an open/locked clone:

| Lane | Primary Unity workspace | Fallback rule |
|---|---|---|
| Gameplay | `/Users/farhad/Projects/WarlineCapture-CodexUnity1` | If locked by a stale process, stop only that stale process when safe or report blocked. Do not take UI/QA workspaces unless PM explicitly reassigns. |
| UI | `/Users/farhad/Projects/WarlineCapture-CodexUnity2` | If locked by a stale process, stop only that stale process when safe or report blocked. Do not take Gameplay/QA workspaces unless PM explicitly reassigns. |
| QA/HCI | `/Users/farhad/Projects/WarlineCapture-CodexUnity3` | If locked by a stale process, stop only that stale process when safe or report blocked. Do not take Gameplay/UI workspaces unless PM explicitly reassigns. |
| Art/Atlas | No default Unity workspace. | Produce/review assets and approval packages from project files. If Unity validation becomes necessary, ask PM to assign a temporary workspace before running it. |
| Designer | No default Unity workspace. | Optimize design docs, README structure, source-of-truth maps, and product-design clarity. Do not touch runtime/source files unless PM explicitly assigns it. |
| Support/FTUE | No default Unity workspace. | Run docs/tests that do not need Unity. If Unity validation becomes necessary, ask PM to assign a temporary workspace before running it. |

Agents are authorized to use their assigned workspace for focused validation without asking for product permission. Do not run validation in another active lane's primary workspace just because it is available; workspace switching is a PM coordination decision, not a lane-local optimization.

Only pause when the Codex/tool sandbox itself requires an approval click, the command would need to write outside the allowed workspace, network access is required, or the action is destructive.

Unity licensing note: QA/HCI confirmed on 2026-05-08 that the project license is healthy, but sandboxed Codex batchmode can fail to reach the user's Unity Licensing Client/session services. If Unity batchmode enters `LicenseClient-farhad` reconnect/time-out loops before tests start, rerun the same required Unity batchmode command with Codex escalation/out-of-sandbox execution in your assigned lane workspace. Treat this as the standard batchmode licensing fix, not a product decision and not a reason to switch workspaces.

If Codex asks for approval because a graphics-enabled Unity capture pass opens Unity with GPU/window access, treat that as a tool permission prompt. The answer is product-approved when the capture is required by the active lane task and uses a dedicated WarlineCapture Unity workspace.

When Codex/tool approval is required for a Unity validation command, request persistent approval for the Unity batchmode command prefix if the tool UI offers that option. Do not ask the user whether the validation should run; ask only for the sandbox to remember the Unity tool permission for this lane's required validation commands. Prefer the narrow Unity executable + `-batchmode` prefix, not a broad shell or arbitrary script rule.

If Unity enters repeated licensing reconnect, unsupported protocol, or license-client loops before tests start from a sandboxed command, do not keep retrying indefinitely. Rerun the same required Unity batchmode command once with Codex escalation/out-of-sandbox execution in your assigned lane workspace. If the escalated rerun still stalls before tests start, stop the stuck Unity process, immediately write or update the required `Design/AgentReports/` report with `Validation result: blocked`, include the exact command/log path and licensing-loop symptom, and wait for PM/user to confirm Unity licensing is healthy.

Stopping a Unity validation process that is stuck in licensing reconnect loops is product-approved cleanup. If Codex asks for approval, ask only for tool permission to stop the stuck process.

## No Scene-Wide Lookup Warnings

Do not add `Object.Find*`, `Resources.FindObjectsOfTypeAll`, `FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, name/tag lookup, obsolete scene-search overloads, or `FindObjectsSortMode` usage in production runtime code, editor validation builders, or Unity tests.

For validation code, use explicit serialized references, known bootstrap/context references, loaded-scene root references, typed runtime registries, ECS component objects, or task-owned fixtures. Every completion report that touches C# must include a scan result for new scene-wide lookup warnings across the touched C# files, including tests and builders.

When pausing for tool approval, phrase the request as a sandbox/tool permission, not a product decision. Example:

```text
The task requires running the focused Unity prefab builder, validation, or graphics-enabled capture pass. Codex needs tool approval to run Unity batchmode outside the sandbox so licensing/session services are reachable. Please approve and remember the Unity batchmode tool permission for this lane's required WarlineCapture validation commands so future focused validation can continue automatically.
```

Stuck validation cleanup wording:

```text
Codex needs tool permission to stop the stuck Unity validation process after repeated licensing reconnect loops. This is cleanup for a blocked required validation, not a product decision.
```

## Required Close-Out

Every completed pass must write a report under `Design/AgentReports/` using the format in:

`Design/WarlineCapture_Agent_Coordination_Workflow.md`

Required timing:

- Draft the report as soon as the task has a stable report filename.
- Update the report immediately after every required validation pass, failed validation attempt, capture pass, build, or log scan.
- Do not begin the next task, report idle/waiting, or ask another lane to continue until the report reflects the latest validation result.
- If a heartbeat finds unreported code edits or capture artifacts from your lane, writing the report is the heartbeat task.

## One-Time User Prompt

The user can send one of these once to an agent:

```text
Set up a recurring heartbeat in this thread. On each heartbeat, read Design/AgentTasks/<lane>_heartbeat.md and follow it. Treat Design/AgentTasks/<lane>_current.md as the only source of current <Lane> priorities.
```

For PM:

```text
Set up a recurring heartbeat in this thread. On each heartbeat, read Design/AgentTasks/pm_heartbeat.md and follow it. Treat Design/AgentTasks/*_current.md as the only source of current lane priorities.
```

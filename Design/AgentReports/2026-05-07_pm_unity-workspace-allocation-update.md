# PM Dispatch: Unity Workspace Allocation Update

Date: 2026-05-07

## Trigger

The user clarified that agents may use `WarlineCapture-CodexUnity` and `WarlineCapture-CodexUnity2`, and that `WarlineCapture-CodexUnity3` should be created if needed so agents can avoid asking for permission when another clone is open.

## Change

Created:

- `/Users/farhad/Projects/WarlineCapture-CodexUnity3`

Linked into the new workspace:

- `Assets -> /Users/farhad/Projects/WarlineCapture/Assets`
- `Packages -> /Users/farhad/Projects/WarlineCapture/Packages`
- `ProjectSettings -> /Users/farhad/Projects/WarlineCapture/ProjectSettings`

Updated:

- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`

## Policy

Agents are authorized to use these Unity workspaces for focused validation without asking for product permission:

- `/Users/farhad/Projects/WarlineCapture-CodexUnity`
- `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- `/Users/farhad/Projects/WarlineCapture-CodexUnity3`

If one workspace is locked/open in another agent thread, agents should switch to another available workspace where practical.

Codex/tool sandbox prompts may still require a user approval click. Those should be phrased as tool permission, not as a question about whether validation should run.

## User Decision Needed

No.

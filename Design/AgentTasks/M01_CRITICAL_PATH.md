# Critical Path

Date: 2026-05-22
Status: held for 3D fresh-start reset
Goal: no active critical path until PM/user dispatches the new 3D direction.

## PM Reset

Read:

- `Design/AgentReports/2026-05-22_pm_agent-task-reset-3d-fresh-start.md`

## Current Gate

No lane is the active owner.

The previous 2D M01 step-by-step mockup, target-lock, soldier-atlas, UI matching, and QA validation sequence is superseded for now.

## Rule

Do not route work from this file while it is held.

Do not continue old 2D M01 or target-lock tasks from `Design/AgentReports/` history.

Do not start Gameplay, UI, Art/Atlas, Designer, QA/HCI, Support/FTUE, or Visual Target work until PM/user creates a new lane assignment in the relevant `Design/AgentTasks/<lane>_current.md` file.

## Next PM/User Action

Create a new critical path and lane task for the fresh 3D direction when ready.

The new task should name:

- lane owner;
- 3D scene/screen/system;
- source-of-truth design doc or reference;
- expected deliverable path;
- validation/proof requirement;
- whether old 2D assets are allowed as references only or must be ignored.

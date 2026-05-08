# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: P0 create Game_Legecy scene and keep Game.unity clean for ISO production

## Assignment

Implement the user's requested scene isolation task.

Create a separate legacy scene:

`Assets/Game/Scenes/Game_Legecy.unity`

Use the exact scene name `Game_Legecy` unless the user later asks to rename it.

Move or copy the old legacy 3D prototype content and its legacy gameplay canvas into `Game_Legecy.unity`, then keep `Assets/Game/Scenes/Game.unity` clean for the 2D/isometric production version.

Read first:

- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-isolation-routing.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_M01_Legacy_Runtime_Guardrails.md`

Do not start M02-M05, vehicles, base/build mechanics, broad combat rebalance, or unrelated polish.

## Required Behavior

- `Game_Legecy.unity` contains the old 3D prototype world/content and the legacy gameplay canvas.
- `Game.unity` remains the clean production scene for the 2D/isometric version.
- The M01 public/isometric path must still launch through the production `Game.unity` route.
- Do not delete legacy assets/prefabs outright; preserve them in the legacy scene.
- Do not reintroduce old 3D prototype visuals into the M01 production/isometric default path.
- Do not require QA/HCI approval for this task; user will validate it directly.

## Validation Required

Gameplay should validate locally and report exact steps for user validation:

- Open `Assets/Game/Scenes/Game_Legecy.unity` and confirm the old 3D prototype plus its canvas are present.
- Open `Assets/Game/Scenes/Game.unity` and confirm it is clean for the isometric production path.
- Run a focused scene/load or PlayMode smoke if available to ensure `Game.unity` still supports M01.
- If Unity validation cannot run, write a blocker report with exact reason and unblock owner.

## Waiting On

Waiting on lane:
none

Owner of next action:
Gameplay

Can my lane still continue fallback work? yes, only the required scene isolation task above.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation.md`

Use the standard WarlineCapture handoff format and include:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task
- exact user validation steps

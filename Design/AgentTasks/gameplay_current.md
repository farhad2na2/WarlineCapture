# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: P0 complete Game_Legecy scene isolation before returning to selected-readability fixes

## Assignment

The user rejected the current `Game_Legecy` scene split. Finish the legacy scene isolation first.

Read first:

- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-rejection-routing.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_M01_Legacy_Runtime_Guardrails.md`

## Required Behavior

- `Assets/Game/Scenes/Game.unity` must be clean for the 2D/isometric production version.
- Remove old prototype/legacy roots from `Game.unity`, including `UI_Canvas`, old legacy menu/canvas setup, duplicate old prototype directional lights, and old prototype `Global Volume`.
- `Game.unity` should not show two directional lights from the legacy prototype setup.
- `Game.unity` should not include the old prototype `Global Volume`.
- `Assets/Game/Scenes/Game_Legecy.unity` must be playable as the legacy prototype.
- Opening and pressing Play in `Game_Legecy.unity` must not load the new 2D/isometric game.
- `Game_Legecy.unity` should use only the legacy playable setup and legacy `UI_Canvas` needed for the old prototype.
- Do not delete shared assets/prefabs outright. Isolate scene roots and launch behavior.
- After this is accepted, Gameplay can return to the selected-readability rejection gate.

## Validation Required

Gameplay must provide direct user-validation steps and evidence:

- Open `Assets/Game/Scenes/Game.unity`.
- Prove the legacy `UI_Canvas`, old prototype duplicate directional lights, and old prototype `Global Volume` are absent.
- Open `Assets/Game/Scenes/Game_Legecy.unity`.
- Press Play.
- Prove the legacy scene runs the old playable prototype through legacy `UI_Canvas`, not the new 2D/isometric loading game.
- Include exact Unity command(s), project path, result, and log/result paths if automated validation is run.

## Waiting On

Waiting on lane:
none

Owner of next action:
Gameplay

Can my lane still continue fallback work? no. Complete this legacy scene split first.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation-fix.md`

Use the standard WarlineCapture handoff format and include short user validation steps.

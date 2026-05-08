# PM Routing: Game_Legecy Scene Isolation

Date: 2026-05-08
Status: routed to Gameplay

## Lane

PM

## Task

Route the user's new priority request to Gameplay: isolate the old 3D prototype and its canvas into a separate `Game_Legecy` scene so `Game.unity` stays clean for the 2D/isometric production version.

## Files changed

- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-isolation-routing.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`

## Contracts touched

- New Gameplay priority supersedes the parked selected-readability wait for this lane.
- Scene name is intentionally `Game_Legecy` because the user requested that exact name.
- User validation is sufficient for this task; no QA/HCI approval is required.
- `Assets/Game/Scenes/Game.unity` should remain the clean production 2D/isometric scene.
- Legacy 3D prototype content and its legacy canvas should be moved/copied into `Assets/Game/Scenes/Game_Legecy.unity`.

## User-visible behavior

After Gameplay completes:

- Opening `Assets/Game/Scenes/Game_Legecy.unity` should show the old 3D prototype and its legacy canvas.
- Opening `Assets/Game/Scenes/Game.unity` should not show the old 3D prototype or legacy gameplay canvas as the default production composition.
- The production/isometric route should keep using `Game.unity`.

## Validation run

PM routing only. No Unity validation run by PM.

## Validation result

Routed.

## Known gaps

- Gameplay must inspect current scene contents before editing.
- Gameplay must avoid deleting legacy prototype content outright; it should preserve it in the new scene.
- Gameplay must avoid breaking M01 isometric public path while cleaning `Game.unity`.

## Cross-lane impacts

- Gameplay owns implementation.
- User owns validation and approval for this task.
- QA/HCI should not block or approve this task.
- UI only re-engages if Gameplay discovers the canvas move requires a concrete UI owner action.

## Next recommended task

Gameplay should deliver:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation.md`

The report must include files changed, validation steps, and clear user validation instructions.

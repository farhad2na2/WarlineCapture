# PM Game_Legecy Scene Rejection Routing

## Lane

PM

## Task

Record the user's rejection of the `Game_Legecy` scene-isolation pass and route Gameplay back to completing that task before continuing selected-readability/ECS visual fixes.

## Files changed

- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-rejection-routing.md`

## Contracts touched

- `Game.unity` is the clean 2D/isometric production scene.
- `Game.unity` must not keep old prototype `UI_Canvas`, old legacy menu/canvas setup, duplicate old prototype directional lights, or old prototype `Global Volume`.
- `Game_Legecy.unity` is the legacy playable prototype scene.
- Pressing Play in `Game_Legecy.unity` must run the legacy prototype through legacy `UI_Canvas`, not load the new 2D/isometric game.
- Gameplay must finish this before returning to selected-readability/ECS visual fixes.

## User-visible behavior

The user rejected the prior scene split because:

| ID | Feedback | Owner |
| --- | --- | --- |
| LEGACY-2026-05-08-01 | `Game.unity` still showed old `UI_Canvas` / legacy setup. | Gameplay |
| LEGACY-2026-05-08-02 | `Game.unity` still showed legacy-style lighting setup, including two directional lights. | Gameplay |
| LEGACY-2026-05-08-03 | `Game.unity` still showed `Global Volume` from the old prototype setup. | Gameplay |
| LEGACY-2026-05-08-04 | Opening `Game_Legecy.unity` loaded the new 2D game instead of the legacy playable scene. | Gameplay |
| LEGACY-2026-05-08-05 | `Game_Legecy.unity` should contain only the legacy playable setup and legacy `UI_Canvas`. | Gameplay |

## Validation run

PM routing/process update only. No Unity validation was run for this report.

## Validation result

Rejected/blocking.

The prior gameplay handoff is superseded where it allowed production bootstrap objects inside `Game_Legecy.unity` and required `Game.unity` to retain `Global Volume` / `Directional Light`.

## Known gaps

- Gameplay must repair the scene split and provide direct validation steps.
- PM must not ask the user to review again until the exact rejected scene items are proven fixed.

## Cross-lane impacts

- Gameplay owns the fix.
- QA/HCI approval is not required for this legacy-scene user validation task unless the user later asks for QA involvement.
- Selected-readability/ECS visual work remains important but is behind this Gameplay priority.

## Next recommended task

Gameplay should write:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation-fix.md`

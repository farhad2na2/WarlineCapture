# User Feedback Review Gate

## Purpose

User rejection feedback is a release blocker until every point is owned, fixed, validated, or explicitly waived by the user.

## Rules

- PM must turn every user rejection into a numbered feedback matrix in a PM report under `Design/AgentReports/`.
- Each feedback item must have an owner lane, expected fix report, validation evidence, and status: `open`, `fixed`, `blocked`, or `waived-by-user`.
- Repeated feedback from an earlier review is automatically `P0` and cannot be treated as polish.
- QA/HCI must include a user feedback regression matrix in its next validation report for any rejected review.
- PM must not ask the user to review again until all feedback items are `fixed` with evidence or `waived-by-user`.
- Screenshots alone are not enough for animation, movement speed, attack timing, or selection feel. Use a video, frame sequence, automated measurement, or clear step-by-step playthrough evidence.
- If the user needs to approve art, PM notification must include short validation steps: what to open/run, what to look for, and what answer PM needs.
- If any lane is blocked because it lacks approval, source files, validation setup, or ownership clarity, PM must write a lane message file and notify the user on the next heartbeat.

## Current Rejection Gates

Active rejection gates:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-rejection-routing.md`

Do not request another selected-readability review until its report's feedback matrix is closed by Gameplay, Art/Atlas, UI if applicable, and QA/HCI.

Do not request another legacy scene review until `Game.unity` is proven clean and `Game_Legecy.unity` is proven to play the legacy prototype instead of the new 2D/isometric loading game.

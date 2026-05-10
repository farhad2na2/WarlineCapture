Gate:
M01 squad readability, selected state, and projectile/VFX scale

Status:
accepted for Gameplay handoff; QA/HCI validation still required

Reason:
Gameplay updated `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` after the PM blocker report for four-soldier readability, selected state, and projectile scale. The updated handoff now reports implementation and automated validation for all three issues.

Validation accepted:
- Player rifle squad now reports as four distinct soldier quads in formation while preserving one squad identity for selection, commands, combat, objective, and HUD state.
- Selecting the rifle squad now enables a visible world selection marker under the squad and keeps the existing HUD selected state active.
- M01 infantry attack tracer values are clamped at mission binding to tactical scale: narrow trace width, brief visible lifetime, and higher dash density.
- Updated validation requires four distinct player soldier renderers under one squad entity.
- Updated validation verifies selected marker visibility after selection.
- Updated validation asserts tactical projectile trace width/lifetime/dash density.
- Existing public Campaign golden path through result popup remains reported as passed.

Validation still needed:
- QA/HCI must visually and interactively verify that the four-soldier squad reads well to a human, not just that four renderers exist.
- QA/HCI must verify selected state is obvious enough during actual play.
- QA/HCI must verify projectile/impact scale feels AAA/tactical and not oversized.
- Final multi-frame infantry atlas art is still review-dependent; `FinalAtlasArtReady` remains `0`.

Cross-lane notices:
- Gameplay has responded to the PM/user-observed blocker and can move out of active blocker ownership if QA/HCI accepts the result.
- QA/HCI should rerun focused Gate 4 from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- UI only re-engages if QA/HCI finds HUD selected-state or UI feedback issues.
- Support/FTUE only re-engages if selection/move/attack guidance becomes misleading.
- User may still be needed for final art approval if QA/HCI finds the current atlas source art below the AAA bar.

Next gate/task:
QA/HCI should rerun final Gate 4 focusing on public golden playthrough, four-soldier readability, selected-state clarity, projectile/impact scale, ECS atlas presentation, no old Model/Destroyed dependency, log classification, and whether the current art is ready for user review or needs a dedicated art approval package.

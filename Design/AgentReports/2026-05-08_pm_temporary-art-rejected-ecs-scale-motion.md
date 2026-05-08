# PM Review: Temporary Gate 4 Art Rejected

Date: 2026-05-08
Status: blocked; user rejected temporary art

## Lane

PM

## Task

Record the user's temporary Gate 4 art rejection and route required fixes before any new Gate 4 approval request.

## Files changed

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/qa-hci_pm_message.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`

## Contracts touched

- Gate 4 remains blocked.
- M01 remains infantry-only.
- Public M01 visible units must be ECS entity / atlas-backed presentation, not Unity `SpriteRenderer` public unit presentation or a `SpriteRenderer`-named proxy/runtime path.
- M01 scale must be metric/contract-driven, not hand-tuned tiny sprite scales.
- M01 selected state must be tactical and per-soldier/readable, not a huge screen-covering marker.
- M01 movement speed and run animation must match realistic infantry motion.

## User-visible behavior

The user rejected the temporary art review after seeing:

1. `M01RuntimeSpriteRenderers` / SpriteRenderer-related runtime naming or presentation, despite the agreed ECS entity animated atlas direction.
2. Soldiers around `0.1505` scale and building around `0.14` scale, both too small. User expects automated realistic sizing using map/road context and human/building anchors, for example soldier height about `1.8m` and building door height about `2.3m`, with building scale closer to `0.8` than `0.14`.
3. An unclear ugly blue marker on soldiers.
4. A huge green selected marker covering the screen. Selection should be small and under each soldier.
5. Unrealistically fast movement that reads like teleporting. Unit config speed must be calibrated to a realistic soldier run.
6. No run animation while moving.

## Validation run

PM documentation/routing only. No Unity validation was run by PM.

## Validation result

Rejected. Do not ask the user to approve temporary Gate 4 art again until Art/Atlas, Gameplay, and QA/HCI provide new evidence against the acceptance criteria below.

## Known gaps

- Art/Atlas must provide a concrete scale/readability package for M01 infantry and visible buildings/decor using metric anchors.
- Gameplay must remove/rename SpriteRenderer-era runtime presentation path and prove ECS atlas presentation without public `SpriteRenderer` unit visuals.
- Gameplay must replace huge selection marker with subtle per-soldier grounding/selection treatment.
- Gameplay must calibrate movement speed and prove visible run animation during movement.
- Designer must codify a short metric scale/readability contract so implementation lanes do not guess.
- QA/HCI must rerun after the fixes land.

## Cross-lane impacts

- Art/Atlas owns scale/readability art package and marker art treatment source.
- Gameplay owns runtime ECS-only presentation, marker runtime behavior, movement speed, pathing proof, and atlas run animation state.
- Designer owns concise scale/readability contract documentation.
- QA/HCI waits for Art/Atlas + Gameplay + Designer handoffs before rerun.
- UI and Support/FTUE have no immediate action unless QA/HCI later finds a concrete HUD, assistant, or FTUE regression.

## Next recommended task

1. Art/Atlas: deliver `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`.
2. Designer: deliver `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`.
3. Gameplay: deliver `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`.
4. QA/HCI: rerun only after all three reports are present and the public M01 route is validated.

# PM Selected-Readability Rejection And Process Failure

## Lane

PM

## Task

Record the user's rejected selected-readability review, route each rejection item to an owner lane, and update the PM process so repeated user feedback cannot be dropped again.

## Files changed

- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/qa-hci_pm_message.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`

## Contracts touched

- Gate 4 remains blocked.
- Public M01 visible units/buildings must be ECS entity visuals, not scene/runtime GameObject renderer wrappers using `MeshRenderer`, `MeshFilter`, or `SpriteRenderer`.
- User rejection feedback is a hard release gate until each item is fixed with evidence, blocked with an owner, or waived by the user.
- M01 remains infantry-only: one player rifle squad type, one enemy patrol type, no player vehicles.
- User review must not be requested again until the rejection matrix is closed.

## User-visible behavior

The user rejected the review for these visible issues:

| ID | Feedback | Owner |
| --- | --- | --- |
| UFB-2026-05-08-01 | Units/buildings appeared as `MeshRenderer`s in the scene, not ECS entity visuals. This was repeated feedback and should not have passed PM/QA. | Gameplay, QA/HCI |
| UFB-2026-05-08-02 | Huge green target marker still appears and must be about two soldier footsteps wide. | Gameplay, UI if overlay-owned, Art/Atlas |
| UFB-2026-05-08-03 | Soldiers animate incorrectly: crouched/sitting while moving, no idle animation, foot artifact at top. | Gameplay, Art/Atlas, QA/HCI |
| UFB-2026-05-08-04 | Soldiers are too big/squashed. Current visual reads better around `0.15` scale than `0.2`. | Gameplay, Art/Atlas, QA/HCI |
| UFB-2026-05-08-05 | Red flashing sitting enemy/object on the right is unclear. | Gameplay, Art/Atlas, QA/HCI |
| UFB-2026-05-08-06 | Soldier selection requires clicking the foot; yellow placeholder square is visible. | Gameplay, UI if overlay-owned, Art/Atlas, QA/HCI |
| UFB-2026-05-08-07 | Obvious issues were not prevented before user review. | PM, QA/HCI |
| UFB-2026-05-08-08 | Process must improve so repeated feedback cannot be ignored. | PM, QA/HCI |

## Validation run

PM routing/process update only. No Unity validation was run for this report.

## Validation result

Rejected/blocking.

The apparent validation gap is that prior checks could block `SpriteRenderer` while still allowing a runtime GameObject `MeshRenderer` presentation path. That is no longer acceptable for the public M01 visible unit/building path.

## Known gaps

- Gameplay still needs to implement and prove a true ECS entity visual path for public M01 units/buildings.
- Art/Atlas still needs to provide marker, animation-frame, scale/aspect, and artifact guidance/assets.
- UI still needs to confirm whether target/selection marker ownership is UI or gameplay/world-overlay.
- Designer still needs to refresh the scale/marker/selection/animation design contract based on the rejection.
- QA/HCI still needs to create and run the rejection-aware validation matrix.

## Cross-lane impacts

- Gameplay owns the ECS visual path, runtime marker behavior, movement/idle animation integration, scale/aspect application, selection hit targeting, and red artifact fix.
- Art/Atlas owns source frames/assets/guidance for idle/run, selected marker, target marker, scale/aspect, and enemy/patrol artifact visuals.
- UI owns any HUD/world-overlay marker or selection affordance if those are implemented in UI code/prefabs.
- Designer owns the rejection-informed visual scale/readability contract.
- QA/HCI owns the feedback regression matrix and must not pass by checking a narrower proxy.
- PM owns enforcement of `Design/AgentTasks/user_feedback_review_gate.md` before asking for another user review.

## Next recommended task

- Gameplay: `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
- Art/Atlas: `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`
- UI: `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`
- Designer: `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
- QA/HCI: `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

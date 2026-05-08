# PM Review: Gameplay Soldier Readability Selection Fix

Date: 2026-05-08
Status: accepted for QA/HCI rerun; not final Gate 4 acceptance

## Lane

PM

## Task

Review Gameplay's M01 individual-soldier source/layout/selection fix and route the next validation owner.

## Files changed

- `Design/AgentReports/2026-05-08_pm_gameplay-soldier-readability-selection-review.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/qa-hci_pm_message.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/designer_current.md`

Accepted upstream reports:

- `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`
- `Design/AgentReports/2026-05-08_art-atlas_gameplay-soldier-readability-selection-review.md`

## Contracts touched

- Gate 4 remains blocked until QA/HCI reruns and PM/user reviews the final evidence.
- Gameplay's individual-soldier source/layout fix is accepted for QA/HCI rerun.
- Art/Atlas accepted the Gameplay fix for its scope.

## User-visible behavior

PM reviewed the refreshed selected first-control captures:

- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`

The world squad now reads as four separate soldier quads instead of four duplicated mini-squad sprites. Selection markers are visible as small grounded warm markers under/near each soldier. This is ready for QA/HCI validation.

## Validation run

PM did not rerun Unity. Gameplay reported:

- PlayMode `Chapter01M01PlayModeValidationTests`: passed `8/8`
- EditMode `Chapter01M01AtlasQuadPresentationTests`: passed `4/4`

PM visually inspected the refreshed 16:9 and 20:9 selected first-control captures.

## Validation result

Accepted for QA/HCI rerun.

## Known gaps

- Not final Gate 4 acceptance.
- Final art remains temporary: final multi-frame run/walk loops, final enemy variant, final impact VFX, and final destroyed/death VFX remain open.
- Unit-card/icon still appears to use squad-style card art. QA/HCI should flag whether this is acceptable for temporary Gate 4 or needs a separate UI/Art polish task before user review.

## Cross-lane impacts

- QA/HCI is now the active owner.
- Gameplay waits unless QA/HCI finds a concrete regression.
- Art/Atlas waits unless QA/HCI finds a sprite/art blocker.
- UI waits unless QA/HCI flags the unit-card/icon as a blocking user-review issue.
- Support/FTUE and Designer have no current action.
- User should not be asked to review until QA/HCI completes the selected-readability rerun.

## Next recommended task

QA/HCI should rerun focused selected-readability validation and write:

`Design/AgentReports/2026-05-08_qa-hci_gate4-selected-readability-rerun.md`

The rerun must include fresh 16:9 and 20:9 selected first-control capture assessment and say whether PM/user should review.

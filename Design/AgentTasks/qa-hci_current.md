# QA/HCI Current Task

Date: 2026-05-08
Status: active
Priority: rerun selected-readability validation after Gameplay individual-soldier fix

## Assignment

Rerun focused selected-readability validation after Gameplay replaced the duplicated group sprite with individual soldier cells and refreshed selected first-control captures.

Read first:

- `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-individual-soldier-frame-review.md`
- `Design/AgentReports/2026-05-08_pm_art-atlas-individual-soldier-frame-review.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`
- `Design/AgentReports/2026-05-08_art-atlas_gameplay-soldier-readability-selection-review.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-soldier-readability-selection-review.md`

## Required Validation

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity3` unless PM routes otherwise.

Validate:

- Public M01 golden path still reaches result popup.
- Opening-control window still allows relaxed first control.
- Player squad now reads as four distinct individual soldiers, not four duplicated group/mini-squad sprites.
- Selected markers are visible as small grounded markers under/near each soldier.
- No huge marker, no unclear blue/green UI-like overlay.
- Public player/enemy unit visuals still have no Unity `SpriteRenderer` components.
- Public player/enemy units still have no `MissionRuntimeSpriteRendererRuntime` component.
- Movement speed and move/run animation proof remain valid.
- M01 remains infantry-only.
- Unit-card/icon: flag whether the squad card/icon is acceptable for temporary Gate 4 review or should be a separate blocking UI/Art polish task.

Generate or review fresh public captures:

- 16:9 selected first-control
- 20:9 selected first-control

## Waiting On

Waiting on lane:
none

Owner of next action:
QA/HCI

Can QA/HCI continue fallback work? yes, only the validation above.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_qa-hci_gate4-selected-readability-rerun.md`

Use the standard WarlineCapture handoff format and include capture paths plus whether PM/user should review.

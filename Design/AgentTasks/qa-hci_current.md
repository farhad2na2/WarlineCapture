# QA/HCI Current Task

Date: 2026-05-08
Status: active
Priority: P0 user-feedback regression gate; previous validation missed repeated ECS/marker/animation issues

## Assignment

Create the rejection-aware QA gate for the latest user feedback and use it for the next validation pass after Gameplay/Art fixes land.

Read first:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentReports/2026-05-08_pm_selected-readability-lane-handoffs-review.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`
- `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
- `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`
- `Design/AgentTasks/qa-hci_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Required Behavior

QA/HCI must not pass the next selected-readability or Gate 4 review unless every user rejection item has direct evidence.

The next QA validation must explicitly prove:

- public M01 visible units/buildings are not scene/runtime `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` GameObject presentation wrappers,
- target marker is small, about two soldier footsteps wide, and not screen-covering,
- selected-state marker is small and under each soldier/footprint, not a placeholder yellow square,
- player rifle squad idle and moving states animate correctly,
- moving soldiers are not crouched/sitting and do not show stray foot artifacts,
- scale/aspect is readable and not vertically squashed,
- selection is easy on the soldier/body/formation footprint,
- red flashing sitting object/enemy is identified and no longer appears as an unexplained artifact,
- M01 remains infantry-only with one player rifle squad and one enemy patrol.

## Validation Required

- Prepare the user-feedback regression matrix now.
- After Gameplay/Art reports land, run focused QA against that matrix.
- Use video/frame sequence or automated measurement for animation and movement. Do not rely on a single screenshot for motion issues.
- Include exact user review steps in the QA report if QA believes the build is ready for PM/user review.

## Waiting On

Waiting on lane:
Gameplay for final implementation validation. Do not wait to write the regression gate/checklist report.

Owner of next action:
QA/HCI, to prepare the gate now and validate when fixes land.

Can QA/HCI continue fallback work? yes, only the gate/checklist and later focused validation.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

Use the standard WarlineCapture handoff format and include the full rejection matrix.

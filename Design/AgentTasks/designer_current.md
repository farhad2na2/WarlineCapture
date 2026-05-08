# Designer Current Task

Date: 2026-05-08
Status: active
Priority: P0 rejection-informed M01 visual/scale/readability contract refresh

## Assignment

The user rejected the selected-readability review and specifically called out scale, marker readability, selection affordance, animation pose, and repeated feedback getting missed.

Refresh the concise M01 design contract so Art/Atlas, Gameplay, UI, and QA/HCI have no room to guess.

Read first:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Required Behavior

- Define tactical visual scale anchors using the user's rules: soldier about `1.8m`, building door about `2.3m`, roads/building footprints as calibration.
- State how to decide soldier/building atlas scale without hand-tuned tiny/huge values.
- Define acceptable selected-state marker size and placement: subtle, grounded, under each soldier/footprint, not screen-covering.
- Define acceptable target/move/attack marker size: about two soldier footsteps wide.
- Define animation expectations: idle animates while idle, run/move animates while moving, no crouched/sitting movement frames unless the unit is intentionally crouching.
- Define selection usability expectation: selection should work on the body/formation footprint, not only exact foot pixels.
- Include QA-readable rejection checks for the repeated issues.

## Waiting On

Waiting on lane:
none

Owner of next action:
Designer

Can my lane still continue fallback work? yes, only this contract refresh.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`

Use the standard WarlineCapture handoff format.

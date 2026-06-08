# PM Rule: Visual Target Quality Gate

Date: 2026-05-07

## Decision

Added a project-wide visual target quality gate.

## Rule

All final target mockups, visual locks, popup targets, HUD targets, prefab targets, and regenerated UI target images must be AAA-quality WarlineCapture mockups aligned with the existing approved visual language.

They must not be accepted if they are state boards, wireframes, deterministic placeholders, generic sci-fi UI sheets, flat layout diagrams, or off-style images.

## Files Updated

- `Design/Agent_Coordination_Workflow.md`
- `Design/AgentTasks/README.md`

## Cross-Lane Impact

- UI must apply this before submitting any target-lock or visual-lock work.
- Support/FTUE must not mark asset rows complete based on placeholder target boards.
- QA/HCI should flag off-style or low-quality target locks as visual-readability/quality findings.
- PM review should mark visual target handoffs `needs fixes` when they fail this gate.

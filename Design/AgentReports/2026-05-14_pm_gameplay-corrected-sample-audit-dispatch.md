# PM Dispatch: Gameplay Audit Corrected M01 Art Sample

Date: 2026-05-14
Lane: Gameplay
Topic: Second implementation-readiness audit for corrected M01 two-frame sample

## Reason

Art/Atlas submitted a corrected M01 two-frame AAA layered sample after Designer and Gameplay feedback. The package now includes revised sample images, a shared camera lock, and asset-prep metadata. Gameplay should audit it again before PM/user approval and before any runtime implementation.

## Assignment

Gameplay audit is active. Do not implement runtime behavior, import assets, change code, claim visual completion, or hand off to QA/HCI.

User approval/fix note:

- User approves the corrected sample quality direction.
- User found a required fix: the selected marker blue circle under each soldier is missing in `M01-02_SquadSelected`.
- Gameplay must audit now and include this as an Art fix, not stop with another blocker-only report.

## Expected Output

`Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`

## Required Decision

Gameplay must state whether the corrected package is ready for Designer/PM/user approval, needs Art fixes, or is blocked.

The audit must specifically verify:

- shared camera/zoom
- player/enemy unit scale
- no-selection state
- selected-but-no-command-mode state
- Build disabled/hidden state
- enemy affiliation/health treatment
- selected marker treatment
- whether camera/asset-prep metadata is sufficient for later pixel-perfect implementation of `M01-01_TacticalStart`

## Routing

Runtime Gameplay implementation and QA/HCI remain blocked. After the audit, PM routes the result to Designer/PM/user for approval or back to Art/Atlas for fixes.

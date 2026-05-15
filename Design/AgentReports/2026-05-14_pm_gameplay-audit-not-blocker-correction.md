# PM Correction: Gameplay Must Audit, Not Stop At Selected-Marker Blocker

Date: 2026-05-14
Lane: Gameplay
Topic: Corrected M01 sample audit handling

## Correction

Gameplay should perform the second implementation-readiness audit now. The missing selected marker blue circles are an audit finding, not a reason to stop with a blocker-only report.

## Required Output

`Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`

## Audit Must Include

- camera/zoom consistency
- player/enemy unit scale
- M01-01 no-selection state
- M01-02 selected-but-no-command-mode state
- Build disabled/hidden state
- enemy affiliation/health treatment
- selected marker treatment, including the missing per-soldier blue/cyan circles
- camera/asset-prep metadata sufficiency for later pixel-perfect implementation of `M01-01_TacticalStart`

## Routing

Gameplay is the current owner for the audit. Runtime implementation and QA/HCI remain blocked.

# POP-08 Intel Reveal V3 Review Record

Canonical target:

- `reference/POP-08_IntelRevealV3_Final_Target.png`

Current review-frozen candidate:

- `iterations/iteration_02/`

Iteration 2 replaces the inherited top-cropped popup with a centered responsive
1672x941 composition. The three evidence illustrations are stored once in
`Assets/Game/Art/UI/V3Shared/IntelReveal/POP08_EvidenceAtlas_V3.png`; each card
uses a separate UV rectangle and aspect-fill viewport. Text, frames, dividers,
meters, and gradients remain procedural Unity UI.

The candidate passed deterministic and live Play Mode captures at exact
1920x1080 and 4800x2160, plus focused checks for the 3 px border contract,
single-atlas reuse, aspect preservation, and Close / View Intel / card-inspect
bindings.

This is review-frozen evidence, not user acceptance. Explicit acceptance is
still pending.

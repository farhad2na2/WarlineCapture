# Campaign visual refinement — 2026-10-10

Owner approved all six follow-up recommendations and requested merge/pull. Work starts from main `de0242d23` in the T7 checkout.

## Changes to complete

1. Repair unsupported decorative attachments and unfinished visible canal edges after native support/bounds inspection.
2. Reduce excessive yellow grading, bloom dirt and flare; retain a warm desert sun and readable tactical shadows.
3. Break up repeated vehicle/storage presentation using existing verified render-only scenery, with checked clearances.
4. Add roof-specific surface variation and supported utility details within existing building footprints and destruction ownership.
5. Bake use-driven surface wear beneath machinery, at entrances, along roads and around rock/building bases.
6. Finish existing water edges and cliff bases with restrained visual detail; preserve the water/land navigation footprint.

## Boundaries and evidence

- Edit existing Campaign sources through native Unity APIs. No new mission maps, mission budgets, routes, collision geometry or functional building placements.
- Keep source and binding fingerprints, native before/after views, recorded decorative moves, protected component state, geometry/material cost and shader diagnostics.
- Rebuild the dense-city virtualized render cache from its exact final source. Keep the source-parity and deterministic-build requirements.
- Native visual inspection is separate from normal-input mission and Android GPU/player acceptance. Retain failures and state remaining gates explicitly.
- Commit and push the scoped verified work, merge the PR and fast-forward main while preserving concurrent changes.

## Findings and completion

All six visual changes are implemented. See [RESULTS.md](RESULTS.md), [gallery.html](gallery.html), and `final-validation.json` for native counts, evidence, corrected failures and pending player/device gates.

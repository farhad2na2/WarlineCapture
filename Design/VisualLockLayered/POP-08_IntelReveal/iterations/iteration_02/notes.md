# POP-08 Intel Reveal — Iteration 2

Status: review-frozen; explicit user acceptance pending.

## Rejected states

- The inherited prefab was rejected because its upper content rendered above
  the viewport, leaving only a small footer fragment visible in Play Mode.
- Iteration 1 corrected the layout and card art, but its scrim was too light
  and the progress-row icon read as a handheld radio instead of intel scanning.

## Iteration 2 corrections

- Centered the complete 1100x756 modal inside the responsive 1672x941 frame.
- Rebuilt header, three evidence cards, progress row, and footer actions to the
  canonical POP-08 hierarchy.
- Replaced resource-placeholder thumbnails with the target Supply Ledger,
  Cargo Manifest, and Radio Intercept evidence content.
- Packed those three unique illustrations into one 1024x288 runtime atlas; no
  individual duplicate card textures are imported.
- Added aspect-fill masked viewports for all three atlas regions.
- Uses procedural directional gradients and one 3 px border thickness on every
  visible framed surface.
- Darkened the modal scrim and changed the progress symbol to the shared V3
  scan icon.
- Preserved header Close and added functional footer Close, View Intel, and
  whole-card inspect bindings.

## Validation evidence

- `intel_reveal_static_1920x1080.png`
- `intel_reveal_static_4800x2160.png`
- `intel_reveal_live_1920x1080.png`
- `intel_reveal_live_4800x2160.png`
- `focused_validation.log`: `[IntelRevealV3Validation] result=Passed tests=3`
- `live_1920x1080.log`: exact Play Mode size and route validation passed.
- `live_4800x2160.log`: exact Play Mode size and route validation passed.

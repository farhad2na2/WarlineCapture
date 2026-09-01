# POP-03 Build Placement — Iteration 3

Status: review-frozen; explicit user acceptance pending.

## Target locks

- `../../reference/POP-03_BuildPlacementV3_Final_Target.png`
- `../../reference/POP-03_BuildPlacementV3_MetadataValidity_Final_Target.png`

## Evidence

- `build_placement_valid_v3_16x9.png`
- `build_placement_valid_v3_20x9.png`
- `build_placement_invalid_v3_16x9.png`
- `build_placement_invalid_v3_20x9.png`
- `build_placement_valid_v3_live_16x9.png`
- `build_placement_valid_v3_live_20x9.png`
- `build_placement_invalid_v3_live_16x9.png`
- `build_placement_invalid_v3_live_20x9.png`
- `build-and-capture.log`
- `focused-validation.log`
- `live-valid-16x9.log`
- `live-valid-20x9.log`
- `live-invalid-16x9.log`
- `live-invalid-20x9.log`

The deterministic renders include the gameplay comparison plate. The live Menu
to Match route intentionally has no battlefield world loaded, so its center is
black; it is retained as runtime mounting, state, and exact-size evidence rather
than as a background-composition comparison.

## Rejected passes

- Pass 1 retained the obsolete lower-right footer footprint and did not expose
  the metadata-validity target as a reusable runtime state.
- Pass 2 corrected the full-width footer, but ARIA/minimap still touched the
  footer, the minimap left a blank aspect strip, Rotate had an extra nested
  frame, and invalid Place Building remained green.
- Iteration 3 removes those defects: ARIA/minimap end before the footer, map art
  aspect-fills without stretching, Rotate has one border, invalid confirmation
  is neutral gray, and valid/invalid panels stay right-pinned at both aspects.

## Frozen checks

- The confirmation bar is a full 1672 x 941 responsive section with a 1664 x
  310 full-width visible footer.
- The validity and minimap surfaces replace ARIA only while placement is
  invalid; ARIA is restored on valid or closed state.
- The unrelated threat cue is suppressed while placement is active and restored
  when placement closes.
- Existing building portrait and minimap art are aspect-preserved; shared V3
  resource, status, and map-marker icons are reused without screen-local copies.
- All V3 gradient surfaces use visible directional color changes and every
  nonzero frame border uses 3 px.
- Four focused structure/state/restore checks passed.
- Valid and invalid live captures passed at exact 1920 x 1080 and 4800 x 2160.

The world-space footprint grid and building ghost remain gameplay-owned. They
are not duplicated or baked into these UI prefabs.

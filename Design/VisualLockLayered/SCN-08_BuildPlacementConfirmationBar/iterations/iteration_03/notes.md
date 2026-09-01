# SCN-08 Build Placement Confirmation Bar — Iteration 3

Status: review-frozen; user acceptance pending.

Target lock:

- `../../reference/SCN-08_BuildPlacementConfirmationBarV3_Final_Target.png`

Accepted review evidence:

- `build_placement_v3_16x9.png`
- `build_placement_v3_20x9.png`
- `build_placement_v3_live_16x9.png`
- `build_placement_v3_live_20x9.png`
- `build-and-capture.log`
- `focused-validation.log`
- `live-16x9.log`
- `live-20x9.log`

Rejected iterations:

- Iteration 1 retained a legacy-width gap, leaked the old command rail, and stretched the building portrait.
- Iteration 2 mounted the content at a tiny local scale inside an oversized root because its Canvas and section scaling disagreed.
- Iteration 3 moves the bar into the shared 1672 x 941 responsive Match section, exposes only the visible panel as the hit-test root, and keeps a clean gap from the squad tray.

Frozen checks:

- Eight procedural gradient surfaces are present and visibly graded.
- Primary chrome borders use a constant 3 px width.
- The existing refinery/building portrait is cropped with aspect preservation; no duplicate building art was generated.
- Materials, oil, and fuel use the shared V3 icon assets.
- Rotate, Cancel, and Place Building remain functional `Button` controls.
- Three focused prefab/runtime checks passed.
- Live Play Mode captures passed at exact 1920 x 1080 and 4800 x 2160 output sizes.

The placement grid and world ghost remain runtime-owned world presentation. They are intentionally not baked into or duplicated by this UI prefab.

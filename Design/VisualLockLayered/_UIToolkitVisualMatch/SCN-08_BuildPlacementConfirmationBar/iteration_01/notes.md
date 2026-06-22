# SCN-08 Build Placement Confirmation Bar - Iteration 01

Status: Satisfied for current pass.

## Slice 01

Implemented a Target Lock-style confirmation rail over the existing runtime-bound UI Toolkit structure.

Changed:

- Replaced the old thin centered bar with a wide bottom rail matching the reference proportions.
- Preserved runtime-bound element names: `Title`, `Status`, `Cost`, `Duration`, `Instruction`, `CancelButton`, `RotateButton`, and `ConfirmButton`.
- Added passive visual-only elements for a building preview card, cost/time metric chips, section dividers, secondary instruction text, and progress pips.
- Added large cancel, rotate, and confirm button treatments with hover/focus lift and press impact states.
- Removed unsupported `pointer-events` USS declarations.
- Moved button slice values onto the concrete button classes so secondary and gold sprites use their matching imported borders.

## Slice 02

Raised the 4800x2160 authoring-scale typography after the first usable shadow UI Builder capture showed tiny labels:

- Increased title, status, metric, instruction, and action-button font sizes.
- Enlarged metric icons and balanced cost/time chip positions.
- Enlarged button labels and instruction pips for readability.

## Slice 03

Fixed clipping defects found in the second capture:

- Shortened the static instruction copy to `TAP TO PLACE BUILDING` so the center panel does not clip.
- Reduced action-button label size slightly and widened the instruction module.

## Slice 04

Fixed action-button overlap:

- Tightened cancel/rotate/confirm button widths and positions.
- Confirm no longer covers the rotate label.
- Rotate, cancel, and confirm labels are visible in the focused rail crop.

Validation:

- Synced `SCN08_BuildPlacementConfirmationBar.uxml` and `.uss` to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `git diff --check` passes.
- Shadow Unity batch import/compile passed in `/private/tmp/warline-build-placement-shadow-batch.log`.
- Visible shadow UI Builder preview opened from `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `playMode=False`, Match Game View enabled, and Fit Viewport at 19%.
- Latest shadow UI Builder log section has no `error CS`, exception, `Unknown property`, or sprite-border override warning for the current preview.

Artifacts:

- Invalid capture evidence: `shadow_ui_builder_scn08_build_placement_slice01_fullscreen.png`.
- Invalid capture evidence: `shadow_ui_builder_scn08_build_placement_slice01_display1.png`.
- Usable window capture before final spacing: `shadow_ui_builder_scn08_build_placement_slice05_window.png`.
- Final window capture: `shadow_ui_builder_scn08_build_placement_slice08_window.png`.
- Final canvas crop: `shadow_ui_builder_scn08_build_placement_slice08_canvas.png`.
- Final focused rail crop: `shadow_ui_builder_scn08_build_placement_slice08_rail_crop.png`.

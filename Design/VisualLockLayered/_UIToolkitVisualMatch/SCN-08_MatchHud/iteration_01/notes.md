# SCN-08 Match HUD Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Satisfied for the current readability/visibility pass from the baseline capture.

Scope:

- Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for UI Builder validation only.
- Do not open or validate with the main project.
- Match HUD is allowed to keep its own gameplay HUD header.
- Enable `Match Game View`, click `Fit Viewport`, capture, then switch focus back to Codex.

Validation:

- Full baseline capture: `shadow_ui_builder_scn08_baseline.png`.
- Header crop: `shadow_ui_builder_scn08_header_valid_baseline_crop.png`.
- Left objectives/selected-unit crop: `shadow_ui_builder_scn08_left_panels_valid_baseline_crop.png`.
- Bottom squad tray and command rail crop: `shadow_ui_builder_scn08_bottom_valid_baseline_crop.png`.
- Right threat/quick-rail crop: `shadow_ui_builder_scn08_right_valid_baseline_crop.png`.
- Minimap crop: `shadow_ui_builder_scn08_minimap_valid_baseline_crop.png`.

Findings:

- Header, resources, current order, and menu control are readable.
- Objectives, selected squad labels, selected-unit action buttons, and health text are visible.
- Squad tray cards and command rail buttons are visible with readable labels.
- Right threat banner and quick rail controls are visible.
- No visual-only USS change was required for this pass.

Next loop:

- Continue to SCN-09 Build Drawer Popup.
- If a later precision pass is requested, compare SCN-08 against the saved HUD mockup with focused crops, especially button labels and squad-card density.

# SCN-08 Match HUD Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Satisfied for current pass after slice 08 squad-tray correction. The earlier `Satisfied for current pass` status was incorrect because it accepted the squad tray from a weak broad crop and did not compare each repeated squad card against the reference. User review identified the five squad panels as visually poor, with the health/slider area not cleanly separated from the cards, a non-reference yellow strip pinned to the top of each card, and card 1 being treated as a one-off design instead of the selected-state example.

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
- Squad tray full slice 08 capture: `shadow_ui_builder_scn08_squad_tray_slice08_full.png`.
- Squad tray focused slice 08 crop: `shadow_ui_builder_scn08_squad_tray_slice08_crop.png`.

Findings:

- Header, resources, current order, and menu control are readable.
- Objectives, selected squad labels, selected-unit action buttons, and health text are visible.
- The earlier claim that squad tray cards passed was too weak. Visibility/readability is not enough for Target Lock acceptance.
- Slice 08 restyles the five squad cards to better match the reference hierarchy: taller cards, clearer portrait area, separated health bar/value text, and segmented status pips.
- Slice 08 fixes the inherited `top: 0` layout bug on `.squad-health-frame`, which had incorrectly pinned the yellow health/progress strip to the top border of each squad card.
- Slice 08 adds the same `SelectedGlow` layer to all five cards and styles `squad-card-selected`, `:hover`, and `:focus` as reusable state treatments without changing card geometry.
- Runtime already moves `squad-card-selected` based on `UiMatchHudSquadTrayModel.SelectedSlot` in `UiToolkitShellView.ApplyMatchHudSquadTray`, so the selected visual state can apply to any squad card when selected.
- The focused slice 08 crop shows all five repeated cards visible with no top progress strip, no health/slider border overlap, readable titles and health values, consistent card chrome, and card 1 serving only as the selected-state example.
- Right threat banner and quick rail controls are visible.
- Visual-only UXML/USS changes were required for the squad tray. Runtime bindings were preserved: `Title`, `HealthFrame`, `HealthFill`, `HealthText`, and `Portrait`.

Next loop:

- Continue the remaining UI Toolkit surfaces with the same focused repeated-template crop gate.
- Do not mark future screens satisfied from broad screenshots when a card, row, button, or slider family has not been reviewed in a focused crop.

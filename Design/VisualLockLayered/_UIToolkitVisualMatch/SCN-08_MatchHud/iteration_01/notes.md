# SCN-08 Match HUD Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Satisfied for current pass after slice 11 panel-by-panel cleanup. The earlier `Satisfied for current pass` status was incorrect because it accepted the squad tray from a weak broad crop and did not compare each repeated squad card against the reference. User review identified the five squad panels as visually poor, with the health/slider area not cleanly separated from the cards, a non-reference yellow strip pinned to the top of each card, card 1 being treated as a one-off design instead of the selected-state example, a weak inner selected overlay, and visibly asymmetric left/right squad-tray padding. The next user review also correctly identified a broader process issue: improving the bottom squad/command rail is not enough while the selected-unit, threat/quick rail, minimap, feedback, and header panel groups still lack focused cleanup.

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
- Squad tray slice 09 capture: pending. Shadow Unity batch import/compile passed, but the controllable GUI preview was not available long enough to capture a fresh UI Builder crop in this pass.
- Slice 10 full screenshots: `shadow_ui_builder_scn08_slice10_fullscreen.png`, `shadow_ui_builder_scn08_slice10_zoomed_fullscreen.png`.
- Slice 11 final full screenshot: `shadow_ui_builder_scn08_slice11b_zoomed_fullscreen.png`.
- Slice 11 focused left stack crop: `shadow_ui_builder_scn08_slice11_left_stack_crop_retina.png`.
- Slice 11 focused right-edge controls crop: `shadow_ui_builder_scn08_slice11_right_edge_controls_wide_crop_retina.png`.
- Slice 11 focused minimap controls crop: `shadow_ui_builder_scn08_slice11_minimap_controls_wide_crop_retina.png`.
- Slice 11 focused footer crop: `shadow_ui_builder_scn08_slice11_footer_crop_retina.png`.
- Slice 11 focused header crop: `shadow_ui_builder_scn08_slice11_header_crop_retina.png`.

Findings:

- Header, resources, current order, and menu control are readable.
- Objectives, selected squad labels, selected-unit action buttons, and health text are visible.
- The earlier claim that squad tray cards passed was too weak. Visibility/readability is not enough for Target Lock acceptance.
- Slice 08 restyles the five squad cards to better match the reference hierarchy: taller cards, clearer portrait area, separated health bar/value text, and segmented status pips.
- Slice 08 fixes the inherited `top: 0` layout bug on `.squad-health-frame`, which had incorrectly pinned the yellow health/progress strip to the top border of each squad card.
- Slice 08 added a `SelectedGlow` layer to all five cards, but user review correctly rejected it as a cheap inner overlay that did not cover the chrome like the mockup.
- Slice 09 removes the `SelectedGlow` elements entirely and changes `squad-card-selected`, `:hover`, and `:focus` to replace the card chrome with `scn08_v02_squad_card_selected_frame.png`.
- Slice 09 balances the five-card tray margins: the cards remain `18.6%` wide, with `1.2%` left and right outer padding and consistent internal gaps.
- Slice 09 squad-card follow-up adds the same professional impact language as the command rail: selected cards sit slightly raised and scaled, hover/focus lifts and scales a little more, and the selected squad-card chrome expands beyond the normal frame.
- Runtime already moves `squad-card-selected` based on `UiMatchHudSquadTrayModel.SelectedSlot` in `UiToolkitShellView.ApplyMatchHudSquadTray`, so the selected visual state can apply to any squad card when selected.
- Slice 09 keeps card 1 as only the static selected-state example in UI Builder; runtime selection remains class-driven.
- Slice 09 also updates the command rail selected-state example shown by `MoveCommand`: `command-button-selected`, `:hover`, and `:focus` now replace the command button chrome with `scn08_v02_square_button_selected_frame.png` instead of tinting the normal frame.
- Slice 09 command-button follow-up adds professional impact states: selected buttons sit slightly raised and scaled, hover/focus lifts and scales a little more, the selected chrome expands beyond the normal frame, and the icon/label brighten together.
- Runtime already moves `command-button-selected` based on the active tactical command mode in `UiToolkitShellView.ApplyMatchHudCommandState`, so the selected visual state can apply to Select, Move, Attack, Hold, Stop, Build, Scan, and Support.
- Validation slice 09: `git diff --check` passed.
- Validation slice 09: shadow Unity batch import/compile passed with no SCN-08 asset/import/compile errors in `/private/tmp/warline-scn08-slice09-shadow-batch.log`.
- Validation slice 09 command-button follow-up: shadow Unity batch import/compile passed with no command selected-frame import/compile errors in `/private/tmp/warline-scn08-command-selected-shadow-batch.log`.
- Validation slice 09 command-button effects follow-up: shadow Unity batch import/compile passed with no new transform/transition USS warnings in `/private/tmp/warline-scn08-command-button-effects-shadow-batch.log`; the only USS warning remains the existing line-36 `picking-mode` warning.
- Validation slice 09 squad-card effects follow-up: shadow Unity batch import/compile passed with no new transform/transition USS warnings in `/private/tmp/warline-scn08-squad-button-effects-shadow-batch.log`; the only USS warning remains the existing line-36 `picking-mode` warning.
- Right threat banner and quick rail controls are visible.
- Visual-only UXML/USS changes were required for the squad tray. Runtime bindings were preserved: `Title`, `HealthFrame`, `HealthFill`, `HealthText`, and `Portrait`.
- Slice 10 replaces the selected-unit portrait background with `scn08_v02_selected_squad_group_portrait.png`, a reference-derived squad portrait crop, so the selected panel no longer shows the wrong landscape/table image.
- Slice 10 adds chrome-level hover/focus/selected treatment and restrained lift/scale impact outside the bottom rail: header menu, selected-unit action buttons, passenger chip/drawer controls, threat jump button, right quick rail buttons, minimap zoom/focus controls, and feedback actions.
- Slice 10 preserves runtime-bound UXML control names such as `ReturnButton`, `DestroyButton`, `BoardButton`, `JumpButton`, `RightBuildCommand`, and `RightSupportCommand`. The mockup action labels/icons differ from the live selection actions, so this pass does not rename controls or change behavior.
- Slice 10 local validation: `git diff --check` passed before shadow sync/import validation.
- Slice 11 raises objective text/readability, adds subtle objective row separators, removes the unsupported `picking-mode` USS property, and switches minimap side controls to `scn08_v02_square_panel_frame.png` with matching 96px slices instead of overriding the square button sprite border.
- Slice 11 final shadow UI Builder screenshot shows the selected-unit portrait, objective panel, right quick rail, minimap controls, feedback rail, command rail, and squad tray clean enough for the current pass.
- Slice 11 validation: `git diff --check` passed.
- Slice 11 validation: shadow Unity batch import/compile passed in `/private/tmp/warline-scn08-panel-cleanup-shadow-batch-v2.log`.
- Slice 11 validation: shadow GUI static preview opened from `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `playMode=False`; final log section after line 1159 has no new errors, Unknown property warnings, or sprite-border override warnings.

Next loop:

- Continue to `SCN-08 Build Placement Bar` using the same shadow UI Builder workflow.
- Keep the panel-by-panel gate active for the next screens: do not accept a broad screenshot when a panel group, button family, card family, progress row, or side control stack is still visibly weak.

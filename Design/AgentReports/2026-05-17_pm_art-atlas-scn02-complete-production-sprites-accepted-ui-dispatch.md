# PM Acceptance - SCN-02 Complete Production Sprites Accepted, UI Dispatched

Date: 2026-05-17
Owner: PM
Status: accepted for UI implementation
Priority: P0

## Decision

Art/Atlas handoff is accepted for the next SCN-02 Main Menu UI implementation pass:

- `Design/AgentReports/2026-05-17_art-atlas_scn02-mainmenu-complete-production-sprites.md`

UI is explicitly re-enabled for one scoped implementation task only: build `SCN-02_MainMenu` from the accepted production sprite manifest and prove the runtime UI matches the target region by region.

Art/Atlas is held unless UI reports an exact missing or faulty accepted layer that PM/user routes back to Art.

## PM Checks

PM reviewed the Art/Atlas report and package enough to route implementation:

- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` parses as valid JSON.
- Manifest status is `ReadyForReview_CompleteProductionSprites_SCN02`.
- Manifest contains `49` layers.
- Required dispatch layers are present, including `main_menu_background_tactical_map`, `brand_logo_panel_frame`, `top_resource_bar_frame_full`, `commander_profile_panel_frame`, `left_nav_row_frame`, `mode_card_frame_large`, and `deploy_command_button_frame`.
- Every manifest-declared layer file exists.
- No `target_slice_*` file is referenced by the manifest.
- `Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py` dry-run maps the new sprites to Unity destinations.
- Contact sheet exists: `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_complete_production_sprites_contact_sheet.png`.
- Full-screen background layer exists at target size: `Design/VisualLockLayered/SCN-02_MainMenu/layers/main_menu_background_tactical_map.png`.

Important note: old `target_slice_*` files may still exist in the design folder from rejected work. They are not accepted runtime assets. UI must ignore them because they are not in the accepted manifest.

## UI Assignment

Required UI output:

- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-production-sprite-implementation.md`

Scope:

- Implement `SCN-02_MainMenu` only.
- Do not start `POP-05_MissionResult`, `SCN-08_RTSBattleHUD`, or any new screen until PM/user reviews the SCN-02 result.

Required implementation source:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- all manifest-declared layer files under `Design/VisualLockLayered/SCN-02_MainMenu/layers/`

Import step:

- Run `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply`.
- Configure Unity sprite import settings from the manifest, including `Sliced` image types and borders.

Runtime construction rules:

- Use only accepted manifest-declared SCN-02 layers and live TMP/data bindings.
- Do not use placeholders, old shell art, generated substitute art, deterministic programmer art, target composites, contact sheets, screenshots, comparison images, or `target_slice_*` files.
- Do not use a baked full-screen target image or any target-reference panel crop as runtime UI.
- If a needed accepted manifest layer is missing, corrupt, too low quality, wrong size, or cannot be imported, stop that region and report the exact layer id/path as an Art/Atlas blocker. Do not substitute.
- Preserve real buttons/routes/interactions while replacing the visual shell where the old shell conflicts with the target.
- Existing WIP composition may be replaced as needed. The target and accepted manifest are the authority.

Panel-by-panel requirements:

- Background: use `main_menu_background_tactical_map` as the target-scale tactical map backdrop.
- Brand/masthead: compose `brand_logo_panel_frame`, `brand_emblem`, and live TMP for the Warline Capture branding unless the manifest names a final brand visual.
- Top resource bar: use `top_resource_bar_frame_full`, `resource_counter_slot_frame`, resource icons, and live TMP counters.
- Settings: use `settings_button_frame` and `settings_gear_icon`.
- Commander profile: use `commander_profile_panel_frame`, `commander_profile_portrait`, and live TMP labels/data.
- Left navigation: use `left_nav_row_frame`, the five `left_nav_icon_*` sprites, `designed_unavailable_badge`, and live route labels/states.
- Mode cards: use `mode_card_frame_large`, the three `mode_card_art_*` sprites, the three `mode_card_header_emblem_*` sprites, footer icons, and live TMP card text.
- Persistent Operation details: use `operation_warning_icon`, `operation_pressure_meter_segments`, `operation_risk_meter_segments`, and `operation_row_divider_chrome`.
- Deploy CTA: use `deploy_command_button_frame`, `deploy_command_chevrons`, `deploy_command_glow_overlay`, and live button text/state.
- 20:9 command feed: use `command_feed_panel_frame` and `command_feed_icon` only in the 20:9 layout where the target requires it.
- Depth/state polish: use the manifest trim and shadow overlays for hover, selected, pressed, disabled, and depth states. Do not invent deterministic visual effects.

Required proof:

- Fresh runtime/editor capture at target 16:9 size `1672x941`.
- Fresh runtime/editor capture for the 20:9 target variant.
- Direct comparison images against both target references.
- Region-by-region mismatch table covering background, masthead, top bar, settings, commander profile, left nav, three mode cards, operation detail rows, deploy CTA, and 20:9 command feed.
- Files changed and validation commands/tests.
- Exact blocker owner and layer id/path for any remaining unmatched region.

Acceptance rule:

UI must not claim complete because the code compiles, the layout is "mostly" close, the target was used as an overlay, or accepted assets are merely present. A valid complete claim requires runtime visual proof that the implemented UI matches the target region by region using the accepted layered sprites and live UI components.

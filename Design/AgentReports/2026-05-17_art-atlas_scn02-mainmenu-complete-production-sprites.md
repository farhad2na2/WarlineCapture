# Art/Atlas SCN-02 Main Menu Complete Production Sprites

Date: 2026-05-17
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Deliver a complete SCN-02 Main Menu production sprite/layer package so UI can build the screen from reusable layered assets and live TMP without placeholders, old shell art, deterministic substitutes, flattened target composites, or target-reference panel slices.

Scope was limited to:

- `Design/VisualLockLayered/SCN-02_MainMenu/`
- this handoff report under `Design/AgentReports/`

No runtime code, Unity prefabs, `Assets/` imports, source docs, or other lane task files were modified.

## Handoff Assessment

- `Design/AgentReports/2026-05-17_pm_art-atlas-scn02-missing-production-sprites-dispatch.md`: accepted as current P0 Art/Atlas routing.
- `Design/AgentReports/2026-05-17_pm_scn02-mainmenu-target-slice-implementation-rejected.md`: accepted; target-reference panel slices are rejected as runtime UI surfaces.
- `Design/AgentReports/2026-05-17_pm_scn02-mainmenu-layered-canvas-wip.md`: accepted as WIP evidence showing missing production sprite coverage.

## Result

SCN-02 now has a complete production sprite package with 49 manifest layers.

Manifest status:

- `ReadyForReview_CompleteProductionSprites_SCN02`

Contact evidence:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_complete_production_sprites_contact_sheet.png`

## New Production Sprite Groups

Added imagegen-sourced sprites for:

- full-screen `main_menu_background_tactical_map`
- `brand_logo_panel_frame`
- `brand_emblem`
- `top_resource_bar_frame_full`
- `resource_counter_slot_frame`
- `settings_button_frame`
- target-scale `commander_profile_panel_frame`
- target-scale `left_nav_row_frame`
- `left_nav_icon_inbox`
- `left_nav_icon_store`
- `left_nav_icon_events`
- `left_nav_icon_ranking`
- `left_nav_icon_command_feed`
- `mode_card_frame_large`
- `mode_card_header_emblem_saga`
- `mode_card_header_emblem_operation`
- `mode_card_header_emblem_quick_custom`
- `operation_warning_icon`
- `operation_pressure_meter_segments`
- `operation_risk_meter_segments`
- `operation_row_divider_chrome`
- `card_footer_icon_saga`
- `card_footer_icon_operation`
- `card_footer_icon_quick_custom`
- `deploy_command_button_frame`
- `deploy_command_chevrons`
- `deploy_command_glow_overlay`
- `command_feed_panel_frame`
- `command_feed_icon`
- cyan, amber, and shadow trim overlays for state/depth polish

Existing SCN-02 production layers remain available, including commander portrait, resource icons, mode card art, route badges, screen shell, and legacy frame variants.

## Imagegen Provenance

New sources:

- Built-in imagegen root: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`
- Complete production sprite atlas: `ig_0ae68e52b07447a2016a096790a71481988bf5676c3ddcd7d8.png`
- Tactical map background: `ig_0ae68e52b07447a2016a096826613081988d41978bf27c19d9.png`

Project source copies:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_complete_production_sprite_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_complete_production_sprite_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_tactical_map_background.png`

Mode-card art source:

- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_layers_contact_sheet_source.png`

Deterministic tooling was used only after imagegen source selection for chroma-key alpha removal, crop extraction, resizing to package dimensions, contact-sheet packaging, manifest metadata, inspection, and validation.

No final runtime art in this handoff was created as deterministic vector/HTML/CSS/scripted output or as a flattened target-reference panel.

## Manifest Updates

Updated `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with:

- source entries for the new imagegen atlas, alpha atlas, tactical map background, and contact sheet
- `sourceGeneration` provenance for the production sprite atlas and background
- 49 total layer entries
- Unity destinations for all new sprites
- roles for frames, button frames, icons, meters, overlays, trims, background, and 20:9 command feed assets
- sprite slicing hints for frame/button/overlay assets
- target rect guidance for primary 16:9 placements and 20:9 command feed usage
- live TMP/binding notes so UI does not bake runtime labels, counters, body copy, or route copy into reusable sprites
- state guidance using base frames plus cyan/amber glow/trim overlays for hover/pressed/selected/disabled polish

## Files Changed

- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_complete_production_sprites_contact_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_complete_production_sprite_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_complete_production_sprite_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_scn02_tactical_map_background.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/main_menu_background_tactical_map.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/brand_logo_panel_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/brand_emblem.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/top_resource_bar_frame_full.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/resource_counter_slot_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/settings_button_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/commander_profile_panel_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_row_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_inbox.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_store.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_events.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_ranking.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/left_nav_icon_command_feed.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_frame_large.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_header_emblem_saga.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_header_emblem_operation.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_header_emblem_quick_custom.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/operation_warning_icon.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/operation_pressure_meter_segments.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/operation_risk_meter_segments.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/operation_row_divider_chrome.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/card_footer_icon_saga.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/card_footer_icon_operation.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/card_footer_icon_quick_custom.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/deploy_command_button_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/deploy_command_chevrons.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/deploy_command_glow_overlay.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/command_feed_panel_frame.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/command_feed_icon.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/trim_overlay_cyan_long.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/trim_overlay_cyan_short.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/trim_overlay_amber_short.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/shadow_trim_overlay_dark.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_saga.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_operation.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/mode_card_art_quick_custom.png`

## Validation Run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read the May 17 PM dispatch, rejection report, and WIP report.
- Generated imagegen production sprite atlas and tactical map background.
- Removed chroma-key background from the sprite atlas.
- Extracted reusable sprites from the selected imagegen atlas.
- Updated README and manifest.
- Built complete production sprite contact sheet.
- Parsed `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with `python3 -m json.tool`: passed.
- Verified manifest layer count: `49`.
- Verified every manifest layer file exists: `missing 0`.
- Scanned `Design/VisualLockLayered/SCN-02_MainMenu/layers/*.png` for opaque chroma-green pixels: `GREEN_REMAINING 0`.
- Ran `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py`: dry-run passed and mapped new sprites to Unity destinations.

## Validation Result

Ready for PM/user review.

- Complete SCN-02 production sprite package delivered: yes
- Full-screen tactical map/background layer included: yes
- Brand/top bar/profile/nav/card/deploy/command-feed production sprites included: yes
- 16:9 and 20:9 usage metadata included: yes
- Button state sprite/overlay rules included: yes
- New/replacement visual assets imagegen-sourced: yes
- Target-reference panel slices used as final runtime art: no
- Deterministic final art created: no
- Runtime code changed: no
- Unity prefabs changed: no
- `Assets/` imports changed: no
- Other packages changed: no

## Next Owner

After PM/user accepts this Art/Atlas handoff, UI can retry SCN-02 Main Menu implementation using the complete production sprite package.

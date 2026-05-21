# SCN-02 Main Menu 3840 Layer Pack Request

Use the existing Warline Capture main menu target image as the only visual authority. Generate a production layer pack for Unity Canvas reconstruction.

Reference image:
`Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`

Layer contract:
`Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json`

## Required Delivery

Provide:

1. One complete full-screen target-lock preview at `3840x2160`.
2. Separate PNG files for every asset listed in `layer_request_3840.json`.
3. Transparent PNGs for chrome frames, buttons, icons, meters, overlays, and chevrons.
4. Opaque PNGs only for full background and content art images.
5. If native transparent PNG output is not possible, use a perfectly flat `#00ff00` chroma-key background for non-opaque layers.

Do not return only one flattened layer sheet. A contact sheet is useful for review, but each asset must also be available as its own file.

## Style Target

Match the original target exactly:

- Minimal clean dark military sci-fi UI.
- Black and charcoal base.
- Subtle cyan tactical grid accents.
- Amber only for operation warnings and deploy command.
- Thin beveled chrome edges.
- Narrow corner cuts.
- Clear spacing between panels.
- No chunky corners.
- No thick borders.
- No generic rounded sci-fi panels.
- No broad yellow/gold recolor.
- No blue full-screen background.
- Bottom world map continents must be visible under the three game mode cards.

## Unity Rules

- Live text must not be baked into reusable frames or shell sprites.
- Frames must be suitable for Unity sliced sprites.
- Content art should be generated larger than its final visible crop so Unity can mask/crop without stretching.
- Icons must be centered with generous padding and no cropped edges.
- The settings frame and gear must be separate assets so the gear can be centered in the Canvas.
- The deploy button frame, chevrons, glow, and text must be separate layers.
- The Persistent Operation warning rows, warning icons, and meter segments must be separate layers.

## Asset Groups

Generate these separate files:

- `full_visual_lock_preview.png`
- `main_menu_background_tactical_map.png`
- `screen_shell_frame.png`
- `brand_logo_panel_frame.png`
- `brand_emblem.png`
- `top_resource_bar_frame_full.png`
- `resource_counter_slot_frame.png`
- `settings_button_frame.png`
- `settings_gear_icon.png`
- `commander_profile_panel_frame.png`
- `commander_profile_portrait.png`
- `profile_block_frame.png`
- `left_nav_row_frame.png`
- `designed_unavailable_badge.png`
- `lock_badge_icon.png`
- `left_nav_icon_inbox.png`
- `left_nav_icon_store.png`
- `left_nav_icon_events.png`
- `left_nav_icon_ranking.png`
- `left_nav_icon_command_feed.png`
- `mode_card_frame.png`
- `mode_card_art_saga.png`
- `mode_card_art_operation.png`
- `mode_card_art_quick_custom.png`
- `mode_card_header_emblem_saga.png`
- `mode_card_header_emblem_operation.png`
- `mode_card_header_emblem_quick_custom.png`
- `operation_warning_icon.png`
- `operation_warning_row_frame.png`
- `operation_pressure_meter_segments.png`
- `operation_risk_meter_segments.png`
- `card_footer_icon_saga.png`
- `card_footer_icon_operation.png`
- `card_footer_icon_quick_custom.png`
- `deploy_command_button_frame.png`
- `deploy_command_chevrons.png`
- `deploy_command_glow_overlay.png`

## Approval Criteria

The pack is accepted only if:

- The full-screen preview visually matches the original target layout and chrome language.
- Panel corners are slim and clean, not bulky.
- Settings button matches the target and gear is centered.
- Left nav buttons are tightly stacked.
- Icons are not cut off.
- Card artwork is not stretched.
- The bottom world map is visible.
- The deploy button matches the target silhouette, color, and low long proportions.
- All listed assets exist as separate files.

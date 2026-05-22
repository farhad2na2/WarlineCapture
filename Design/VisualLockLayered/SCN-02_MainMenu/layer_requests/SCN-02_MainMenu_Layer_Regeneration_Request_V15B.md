# SCN-02 Main Menu Layer Regeneration Request V15B

Date: 2026-05-22
Status: Rejected / superseded by V15C
Owner: Designer / UI Agent

## Rejection Notes

V15B failed the asset gate:

- The top bar was not clean enough for implementation.
- Several images and UI parts touched each other, making extraction unreliable.
- The logo was presented inside a dark box instead of as a clean isolated asset on green.
- The mode images were packed into the same sheet and were visually cut by sheet constraints.
- The mode images must be requested and generated separately so each one has enough width and clean overscan for 20:9 / 21:9 layouts.

Do not use `generated_one_go/source/SCN-02_MainMenu_LayerSourceSheet_Green_V15B.png` for Unity implementation.

## Correction

Do not cut, crop, mask, or reuse pixels from `reference/SCN-02_MainMenu_Landscape_Target.png` as implementation layers.

The reference image is a target-lock mockup only. It is used to judge composition, proportions, material language, mood, and visual hierarchy. The implementation pack must be generated as clean source assets on a solid green background and then extracted through the VisualLockLayered V15 workflow.

## Output Needed

Generate a new clean layer source sheet:

- File target: `generated_one_go/source/SCN-02_MainMenu_LayerSourceSheet_Green_V15B.png`
- Background: flat solid `#00ff00`
- No shadows or gradients touching the green background.
- No baked readable dynamic text, except the logo lockup if generated as logo art.
- All UI pieces must have generous spacing so chroma extraction does not leave green edge spill.
- Style must match the approved SCN-02 main menu reference: dark military command-base UI, worn black metal, gold bevels, olive selected states, compact AAA mobile strategy presentation.

## Required Layers

Header:

- `scn02_header_logo_panel_bg`: left top header background only, no logo baked into the panel.
- `scn02_brand_logo_lockup`: separate Warline Capture logo matching the approved reference scale, white metallic WARLINE, gold CAPTURE, emblem at left.
- `scn02_header_resource_panel_bg`: center resource bar background with separators and bevels, no resource text/icons baked unless separately requested.
- `scn02_header_command_panel_bg`: command-resource panel background with bevels, no value text baked.
- `scn02_header_right_actions_bg`: top-right background with two action button wells matching the approved reference proportions.
- `scn02_icon_inbox_envelope`: separate inbox icon.
- `scn02_icon_settings_gear`: separate settings icon.
- `scn02_notification_dot`: separate gold notification badge.

Left navigation:

- `scn02_nav_button_selected_frame`
- `scn02_nav_button_inactive_frame`
- Route icons for Campaign, Operations, Skirmish, Store, Commander, Settings.

Mode cards:

- `scn02_mode_card_frame`: reusable frame for Campaign, Operations, Skirmish.
- `scn02_mode_card_thumbnail_mask_frame`: thumbnail window/mask frame.
- `scn02_mode_progress_meter_frame`
- `scn02_readiness_segments`
- `scn02_campaign_thumbnail_art`: wide 3D operation-town image with horizontal overscan.
- `scn02_operations_thumbnail_art`: wide command/intel operation image with horizontal overscan.
- `scn02_skirmish_thumbnail_art`: wide forward-base combat setup image with horizontal overscan.

Right commander panel:

- `scn02_commander_panel_frame`
- `scn02_commander_portrait_frame`
- `scn02_commander_portrait_art`: generated silhouette/portrait art, not cut from the reference.
- `scn02_locked_row_frame`
- `scn02_resource_command_shield`

Bottom and CTA:

- `scn02_deploy_cta_frame`
- `scn02_comms_status_panel_frame`
- `scn02_resource_coin_badge`
- `scn02_resource_supplies_crate`
- `scn02_icon_lock`
- `scn02_trim_corner_brackets`
- `scn02_trim_slashes_and_bolts`

Background:

- `scn02_background_art`: separate 21:9-safe no-UI command-base background, wide enough for 20:9 and 21:9 cover/crop.

## Prompt For Image Generation

Use case: ui-mockup
Asset type: mobile game UI layer source sheet for Unity Canvas extraction

Create a clean VisualLockLayered V15 source sheet for a AAA mobile 3D military strategy main menu. The visual target is a forward command base menu for Warline Capture: dark worn metal panels, gold beveled trims, olive selected state, compact resource header, left navigation, three mode cards, commander panel, and deploy CTA. Generate only separate UI implementation assets on a perfectly flat solid `#00ff00` chroma-key background.

Important constraints:

- Do not crop, trace, paste, or reuse pixels from the reference mockup.
- Match the reference style and proportions, but create fresh clean implementation assets.
- Keep every layer separated with clear green padding.
- Header pieces must be true separate assets: left logo panel background, logo lockup, resource panel background, command panel background, right action panel background, inbox icon, settings icon, notification badge.
- The right action panel must include two inset button wells matching the approved mockup proportions.
- The logo lockup must visually match the approved mockup: emblem left, WARLINE in white metal, CAPTURE in gold, small gold speed mark.
- Mode thumbnail art must be wide horizontal 3D images, not small cropped card images, so wider aspects reveal more art instead of stretching.
- Do not bake dynamic labels or numbers into frames: resource values, mode names, progress values, commander level, locked labels, comms status, and CTA text will be live TMP in Unity.
- Avoid green in any asset edge or detail because the background is chroma-key green.

The sheet should be large enough that each asset is crisp after extraction. Use the same layer naming list from this request as the intended output manifest.

## Acceptance Gate

The regenerated pack is accepted only when:

- The contact sheet shows no green fringe.
- The header pieces match the target reference structure without being reference crops.
- The logo is a clean separate asset and visually matches the target.
- The right action panel has the correct two-button structure.
- Wide mode thumbnails exist and are large enough for 20:9 / 21:9 horizontal reveal.
- Manifest source entries point to generated source assets, not the target reference.

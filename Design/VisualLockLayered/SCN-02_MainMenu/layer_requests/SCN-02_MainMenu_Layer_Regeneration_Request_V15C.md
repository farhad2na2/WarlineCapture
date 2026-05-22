# SCN-02 Main Menu Layer Regeneration Request V15C

Date: 2026-05-22
Status: Active
Owner: Designer / UI Agent

## Workflow Correction

V15C replaces the failed V15B one-sheet request. Do not generate all assets in one crowded source sheet.

Generate separate source images per asset group so each layer has clean padding, proper scale, and no accidental cuts:

- Header UI source sheet.
- Logo source image.
- Navigation / icon source sheet.
- Blank frame/background source sheet.
- Icons / badges / meters / chevrons source sheet.
- Commander portrait source image.
- Campaign mode thumbnail source image.
- Operations mode thumbnail source image.
- Skirmish mode thumbnail source image.
- 21:9 menu background source image.

Do not cut, crop, mask, trace, paste, or reuse pixels from `reference/SCN-02_MainMenu_Landscape_Target.png` as implementation layers. The reference is visual guidance only.

## Required Source Files

All green-screen source files must use a perfectly flat `#00ff00` background with no shadows or gradients touching the background.

- `generated_v15c/source/SCN-02_Header_UI_Green.png`
- `generated_v15c/source/SCN-02_Logo_Green.png`
- `generated_v15c/source/SCN-02_Nav_Icons_Green.png`
- `generated_v15c/source/SCN-02_BlankFrames_Green.png`
- `generated_v15c/source/SCN-02_Icons_Meters_Green.png`
- `generated_v15c/source/SCN-02_CommanderPortrait_Green.png`
- `generated_v15c/source/SCN-02_CampaignThumbnail_Wide.png`
- `generated_v15c/source/SCN-02_OperationsThumbnail_Wide.png`
- `generated_v15c/source/SCN-02_SkirmishThumbnail_Wide.png`
- `generated_v15c/source/SCN-02_Background_21x9_NoUI.png`

## Header UI Request

Generate only these separate header assets on green, with large spacing between them:

- `scn02_header_logo_panel_bg`: long angular black metal panel with gold bevel, no logo.
- `scn02_header_resource_panel_bg`: long segmented black metal resource bar with diagonal separators, no text, no numbers.
- `scn02_header_command_panel_bg`: shorter command-resource black metal panel, no text, no numbers.
- `scn02_header_right_actions_bg`: top-right black metal panel with two inset rectangular action button wells, no icons.
- `scn02_icon_inbox_envelope`: separate icon.
- `scn02_icon_settings_gear`: separate icon.
- `scn02_notification_dot`: separate small gold badge.

The right action panel must match the approved mockup structure: two clean button wells with enough inset padding, not rough boxes, not cut off.

## Logo Request

Generate only `scn02_brand_logo_lockup` on green.

The logo must be isolated directly on the `#00ff00` background, not inside a black panel or box. It should visually match the approved mockup: emblem at left, WARLINE in white metallic block letters, CAPTURE in gold below, small gold speed mark to the right. This is the only allowed baked text.

## Mode Thumbnail Requests

Each mode thumbnail must be generated as its own wide image, not packed into a sheet.

Size target: wide 21:9-safe art, at least 1800px wide if possible. No UI text. No labels. No green background.

- Campaign: Middle Eastern town district with convoy staging, civilian urban density, mosque/tower landmarks, attack preparation tone.
- Operations: command/intel district view with tactical map hologram feel, antennae, satellite dish, controlled operation planning tone.
- Skirmish: forward-base combat yard with hangars, barriers, vehicles, training/custom battle setup tone.

Each thumbnail must include horizontal overscan so Unity can reveal more art on 20:9 / 21:9 cards instead of stretching.

## Parent / Child Layer Rule

Parent frames and backgrounds must not contain child gameplay/UI state elements.

Do not bake these into any parent frame:

- star badges
- command shield icons
- lock icons
- progress/readiness bars
- filled progress segments
- chevrons
- route icons
- notification dots
- resource icons

Those must be separate layers from `SCN-02_Icons_Meters_Green.png`.

## Blank Frame / Panel Request

Generate on green with large spacing:

- `scn02_nav_button_selected_frame`
- `scn02_nav_button_inactive_frame`
- `scn02_mode_card_frame`
- `scn02_mode_card_thumbnail_mask_frame`
- `scn02_commander_panel_frame`: blank panel only, no star icon, no lock icons, no progress bars.
- `scn02_commander_portrait_frame`
- `scn02_locked_row_frame`: blank row only, no lock icon.
- `scn02_deploy_cta_frame`: blank gold button frame only, no chevrons and no text.
- `scn02_comms_status_panel_frame`

No baked dynamic text, icons, locks, stars, progress bars, filled meter segments, or chevrons.

## Icons / Meters Request

Generate on green with large spacing:

- `scn02_mode_progress_meter_frame`
- `scn02_readiness_segments`
- `scn02_resource_coin_badge`
- `scn02_resource_supplies_crate`
- `scn02_resource_command_shield`
- `scn02_icon_lock`
- `scn02_deploy_cta_chevrons`
- `scn02_trim_corner_brackets`
- `scn02_trim_slashes_and_bolts`
- Route icons: campaign crosshair, operations map pin, skirmish crossed blades, store cart, commander bust, settings gear.

No baked dynamic text. CTA label, resource values, mode labels, progress values, commander data, locked labels, and comms text are live TMP in Unity.

## Acceptance Gate

V15C is accepted only when:

- No UI assets touch each other in source sheets.
- Header pieces are clean and match the target structure.
- The logo is isolated on green with no panel background.
- Each mode thumbnail is a separate wide image.
- Parent frames contain no child icons, bars, locks, badges, chevrons, or state overlays.
- No implementation asset is sourced from reference crops.
- Contact sheet shows no green edge spill after extraction.
- Manifest lists the generated V15C source file for every layer.

# SCN-02C Main Menu Bright Command Layer Manifest

## Source Target

Reference target:

- `Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/reference/scn02c_target_lock_warline_capture_bright.png`

This target is a visual direction only. Implementation sprites must be atomized and must not bake replaceable UI content.

## Hard Rules

- No gameplay/UI labels are baked into sprites.
- No `DEPLOY`, `CAMPAIGN`, `SKIRMISH`, `OPERATIONS`, nav labels, resource numbers, commander names, or status text are baked into implementation sprites.
- Text is rendered by Unity/UI Toolkit using the project UI font.
- The Warline Capture logo may remain a logo sprite because it is brand art, not replaceable UI text.
- Game mode card artwork is separate from card chrome.
- Game mode card frame/chrome does not include the scene artwork.
- Game mode lower label plate is separate from the card artwork and frame.
- Icons are separate from frames and labels.
- Button frames are separate from icons, arrows, chevrons, and text.
- All generated source sprites use a flat solid `#00ff00` background for chroma-key removal, except the full background plate.
- Final Unity sprites must be clean alpha PNGs with no green fringe and trimmed to the visible bounds.

## Target Folders

Design sources:

- `Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/green_sources`
- `Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/layers`

Unity sprites:

- `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites`

## Atomized Sprite List

### Full-Screen Background

- `scn02c_background_command_table_no_ui`
  - Full 16:9 command-table background.
  - No UI, text, logo, buttons, cards, frames, or labels.
  - Not green-background; this is an opaque background plate.

### Brand

- `scn02c_brand_logo_lockup`
  - Warline Capture logo lockup only.
  - Includes emblem plus WARLINE CAPTURE brand text.
  - No header bar.

### Header Chrome

- `scn02c_header_bar_frame`
  - Full-width top header chrome only.
  - Empty left logo area and right resource/control area.
  - No text, no logo, no icons.
- `scn02c_resource_chip_frame`
  - One reusable empty resource chip frame.
  - No icon, no number, no plus.
- `scn02c_header_square_button_frame_default`
  - One empty square header utility button frame.
- `scn02c_header_square_button_frame_hover`
  - Optional highlighted square header utility button frame.
- `scn02c_plus_icon`
- `scn02c_mail_icon`
- `scn02c_settings_gear_icon`
- `scn02c_menu_hamburger_icon`
- `scn02c_resource_crate_icon`
- `scn02c_resource_diamond_icon`
- `scn02c_resource_energy_icon`

### Left Navigation

- `scn02c_nav_button_frame_selected`
  - One selected angled nav button frame only.
  - No icon, no arrow, no text.
- `scn02c_nav_button_frame_default`
  - One default angled nav button frame only.
  - No icon, no arrow, no text.
- `scn02c_nav_chevron_icon`
- `scn02c_nav_campaign_target_icon`
- `scn02c_nav_armory_ammo_icon`
- `scn02c_nav_supply_crate_icon`
- `scn02c_nav_command_shield_icon`
- `scn02c_nav_tech_tree_nodes_icon`
- `scn02c_nav_profile_tag_icon`

### Game Mode Card Structure

- `scn02c_mode_card_frame_selected`
  - Tall selected card chrome frame only.
  - Transparent interior area.
  - No artwork, no label plate, no icon, no text.
- `scn02c_mode_card_frame_default_blue`
  - Tall default blue/steel card chrome frame only.
  - Transparent interior area.
  - No artwork, no label plate, no icon, no text.
- `scn02c_mode_card_frame_default_amber`
  - Tall default amber/gold card chrome frame only.
  - Transparent interior area.
  - No artwork, no label plate, no icon, no text.
- `scn02c_mode_card_label_plate_selected`
  - Lower label plate for selected card only.
  - No text, no icon.
- `scn02c_mode_card_label_plate_blue`
  - Lower label plate for blue card only.
  - No text, no icon.
- `scn02c_mode_card_label_plate_amber`
  - Lower label plate for amber card only.
  - No text, no icon.
- `scn02c_mode_badge_frame_selected`
- `scn02c_mode_badge_frame_blue`
- `scn02c_mode_badge_frame_amber`
- `scn02c_mode_card_bottom_star_icon`
- `scn02c_mode_card_divider_line`

### Game Mode Card Artwork

- `scn02c_mode_art_campaign_valley`
  - Rectangular card artwork only.
  - No frame, no label plate, no icon, no text.
- `scn02c_mode_art_skirmish_airbase`
  - Rectangular card artwork only.
  - No frame, no label plate, no icon, no text.
- `scn02c_mode_art_operations_radar`
  - Rectangular card artwork only.
  - No frame, no label plate, no icon, no text.

### Game Mode Icons

- `scn02c_mode_campaign_target_icon`
- `scn02c_mode_skirmish_crossed_weapons_icon`
- `scn02c_mode_operations_star_icon`

### Commander Panel

- `scn02c_commander_panel_frame`
  - Tall right panel frame only.
  - No portrait, no text, no stat values.
- `scn02c_commander_portrait`
  - Commander portrait artwork only.
  - No frame, no edit icon, no text.
- `scn02c_commander_portrait_frame`
- `scn02c_commander_edit_icon`
- `scn02c_commander_rank_badge_icon`
- `scn02c_commander_level_badge_frame`
- `scn02c_commander_progress_bar_frame`
- `scn02c_commander_progress_bar_fill`
- `scn02c_commander_faction_standing_tick_on`
- `scn02c_commander_faction_standing_tick_off`

### Deploy Command

- `scn02c_deploy_button_frame`
  - Empty deploy button frame only.
  - No DEPLOY text.
- `scn02c_deploy_chevron_left`
- `scn02c_deploy_chevron_right`
- `scn02c_deploy_star_icon`

## Prompting Pattern

Every green-source implementation sprite should use:

```text
Create ONLY the named sprite on a perfectly flat solid #00ff00 chroma-key background for removal.
The background must be one uniform #00ff00 with no shadows, gradients, texture, reflection, floor, or lighting variation.
Keep the sprite fully separated from the background with crisp edges and generous padding.
Do not use #00ff00 anywhere in the sprite.
No cast shadow, no contact shadow, no watermark.
No baked UI labels or numbers.
```

For frame sprites, require transparent/interior intent in the prompt even though the source is green:

```text
The interior/content area should be open/empty so it can be made transparent after green removal.
Do not place artwork, text, icons, labels, or photos inside the frame.
```

For art plate sprites:

```text
Generate only the rectangular artwork plate. Do not include card chrome, lower label plate, badge, icon, text, or border frame.
```

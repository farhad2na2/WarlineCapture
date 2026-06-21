# UI Toolkit Pixel Per Unit Audit

Last updated: 2026-06-21

Scope:
This file currently records the SCN-02 Main Menu visual-match pass only. It does not complete the global all-screen Pixel Per Unit audit.

Source files:

- `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss`
- `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/*.png.meta`
- `Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV04Imagegen/Sprites/scn01_v04_logo_lockup.png.meta`

Summary:

- SCN-02 referenced sprites inspected: 43.
- Non-default Pixel Per Unit values found: 0.
- All inspected SCN-02 referenced sprites currently use `spritePixelsToUnits: 100`.
- No PPU edits were made in this pass.
- Do not tune PPU until a current post-typography runtime/UI Builder crop proves frame/icon line weight is wrong.

## SCN-02 Referenced Sprite Import Audit

| Sprite | Size | PPU | Border | Corner alpha TL/TR/BL/BR | Empty gutter L/R/T/B | Alpha transparency | Default compression |
| --- | ---: | ---: | --- | --- | --- | ---: | ---: |
| `scn02c_background_command_table_no_ui.png` | 1672x941 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | opaque/no-alpha | n/a | 1 | 0 |
| `scn02c_header_bar_frame.png` | 1608x262 | 100 | `{x: 110, y: 52, z: 110, w: 52}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn01_v04_logo_lockup.png` | 785x193 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 255/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_resource_chip_frame.png` | 1466x325 | 100 | `{x: 135, y: 82, z: 135, w: 82}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_resource_crate_icon.png` | 728x747 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_resource_diamond_icon.png` | 762x675 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_resource_energy_icon.png` | 483x888 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_plus_icon.png` | 604x610 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_header_square_button_frame_default.png` | 887x902 | 100 | `{x: 150, y: 150, z: 150, w: 150}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mail_icon.png` | 707x507 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_settings_gear_icon.png` | 788x799 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_menu_hamburger_icon.png` | 674x547 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_button_frame_default.png` | 1527x485 | 100 | `{x: 130, y: 95, z: 130, w: 95}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_button_frame_selected.png` | 1675x506 | 100 | `{x: 130, y: 95, z: 130, w: 95}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_campaign_target_icon.png` | 779x788 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_armory_ammo_icon.png` | 518x737 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_supply_crate_icon.png` | 728x747 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_command_shield_icon.png` | 680x894 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_tech_tree_nodes_icon.png` | 692x682 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_profile_tag_icon.png` | 448x855 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_chevron_icon.png` | 304x512 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_backing_selected.png` | 783x1390 | 100 | `{x: 72, y: 104, z: 72, w: 104}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_backing_blue.png` | 726x1393 | 100 | `{x: 72, y: 104, z: 72, w: 104}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_backing_amber.png` | 776x1394 | 100 | `{x: 72, y: 104, z: 72, w: 104}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_art_campaign_valley.png` | 1024x1536 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | opaque/no-alpha | n/a | 1 | 0 |
| `scn02c_mode_art_skirmish_airbase.png` | 1024x1536 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | opaque/no-alpha | n/a | 1 | 0 |
| `scn02c_mode_art_operations_radar.png` | 1024x1536 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | opaque/no-alpha | n/a | 1 | 0 |
| `scn02c_mode_card_frame_selected.png` | 783x1390 | 100 | `{x: 72, y: 104, z: 72, w: 104}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_frame_default_blue.png` | 726x1393 | 100 | `{x: 72, y: 104, z: 72, w: 104}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_frame_default_amber.png` | 776x1394 | 100 | `{x: 72, y: 104, z: 72, w: 104}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_label_plate_selected.png` | 1564x434 | 100 | `{x: 135, y: 92, z: 135, w: 92}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_label_plate_blue.png` | 1554x434 | 100 | `{x: 135, y: 92, z: 135, w: 92}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_card_label_plate_amber.png` | 1419x489 | 100 | `{x: 135, y: 92, z: 135, w: 92}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_badge_frame_selected.png` | 546x720 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_badge_frame_blue.png` | 504x624 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_badge_frame_amber.png` | 536x706 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_campaign_target_icon.png` | 779x788 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_skirmish_crossed_weapons_icon.png` | 894x873 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_mode_operations_star_icon.png` | 690x892 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_deploy_star_icon.png` | 613x620 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_nav_button_backing_default.png` | 1527x485 | 100 | `{x: 130, y: 95, z: 130, w: 95}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_commander_portrait.png` | 1024x1536 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | opaque/no-alpha | n/a | 1 | 0 |
| `scn02c_deploy_button_frame.png` | 1664x499 | 100 | `{x: 155, y: 96, z: 155, w: 116}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_deploy_chevron_left.png` | 664x623 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |
| `scn02c_deploy_chevron_right.png` | 632x513 | 100 | `{x: 0, y: 0, z: 0, w: 0}` | 0/0/0/0 | L0/R0/T0/B0 | 1 | 0 |

## Notes

- `scn01_v04_logo_lockup.png` remains intentionally referenced by SCN-02 because the user confirmed the shield/star logo is the correct Main Menu logo.
- Alpha/gutter checks are mechanical edge checks only. Visual crop review is still required before PPU decisions.
- All default texture platform entries inspected here use uncompressed default texture compression (`textureCompression: 0`).

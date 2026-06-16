# SCN-02C Main Menu Bright Command

This folder contains the target lock and atomized sprite pack for the brighter, less-cartoon Warline Capture main menu art direction.

## Reference

- `reference/scn02c_target_lock_warline_capture_bright.png`

The reference is the visual target only. It should not be used as a baked runtime UI background.

## Source And Output Folders

- `green_sources/`
  - Original generated source sprites.
  - Files ending in `_source_green.png` were generated on a flat chroma-key background.
- `layers/`
  - Cleaned implementation sprites.
  - Green background removed to alpha.
  - Transparent margins clamped to visible alpha bounds.
- `validation/`
  - `scn02c_atomized_sprites_contact_sheet.png`
  - `scn02c_atomized_sprites_validation.json`

Unity copies are saved under:

- `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites`

## Implementation Rules

- Use real Unity/UI Toolkit text for menu labels, card titles, resource values, commander names, and status text.
- Do not bake `DEPLOY`, `CAMPAIGN`, `SKIRMISH`, `OPERATIONS`, nav labels, resource values, or commander copy into sprites.
- Use separate sprites for card artwork, card frame, lower label plate, badge frame, and icon.
- Use separate sprites for button frames and icons.
- Use `scn02c_background_command_table_no_ui.png` as the opaque menu background plate.
- Use `scn02c_brand_logo_lockup.png` as the only baked brand text asset.

## Validation Result

- Sprite count: 42
- Visible green leftovers: 0
- Unclamped alpha margins: 0

The validation JSON is the source of record for dimensions and cut checks.

# UI Toolkit 9-Slice Audit

Last updated: 2026-06-21

Scope:
This file currently records the SCN-02 Main Menu visual-match pass only. It does not complete the global all-screen 9-slice audit.

Source files:

- `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss`
- `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/*.png.meta`

Summary:

- SCN-02 sliced USS selectors audited: 13.
- SCN-02 `-unity-slice-scale` values now include explicit `px` units so Unity accepts the USS.
- No sprite border metadata was changed in this pass.
- Source sprite borders and USS slice values align for the audited SCN-02 sliced chrome, except the deploy frame orientation remains a visual-verification item because the meta border is `{x:155, y:96, z:155, w:116}` and the USS uses top/bottom `116/96`.

## SCN-02 Sliced Chrome

| USS selector | Sprite source | USS left | USS right | USS top | USS bottom | USS scale | Sprite border | Status |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
| `.header-back-plate` | `scn02c_header_bar_frame.png` | 110 | 110 | 52 | 52 | 0.28px | `{x:110, y:52, z:110, w:52}` | Matches metadata; needs crop review for visual weight. |
| `.resource-frame` | `scn02c_resource_chip_frame.png` | 135 | 135 | 82 | 82 | 0.2px | `{x:135, y:82, z:135, w:82}` | Matches metadata; needs crop review for chip thickness. |
| `.header-icon-button-frame` | `scn02c_header_square_button_frame_default.png` | 150 | 150 | 150 | 150 | 0.18px | `{x:150, y:150, z:150, w:150}` | Matches metadata; needs icon-button crop review. |
| `.nav-frame` | `scn02c_nav_button_frame_default.png` | 130 | 130 | 95 | 95 | 0.22px | `{x:130, y:95, z:130, w:95}` | Matches metadata for default; selected frame uses same border. |
| `.mode-card-fill` | `scn02c_mode_card_backing_*` | 72 | 72 | 104 | 104 | 0.32px | `{x:72, y:104, z:72, w:104}` | Matches backing metadata. |
| `.mode-card-frame` | `scn02c_mode_card_frame_*` | 72 | 72 | 104 | 104 | 0.32px | `{x:72, y:104, z:72, w:104}` | Matches frame metadata. |
| `.mode-card-label-plate` | `scn02c_mode_card_label_plate_*` | 135 | 135 | 92 | 92 | 0.2px | `{x:135, y:92, z:135, w:92}` | Matches label-plate metadata. |
| `.commander-section-backing` | `scn02c_nav_button_backing_default.png` | 130 | 130 | 95 | 95 | 0.22px | `{x:130, y:95, z:130, w:95}` | Matches metadata; reused nav backing. |
| `.commander-section-frame` | `scn02c_nav_button_frame_default.png` | 130 | 130 | 95 | 95 | 0.22px | `{x:130, y:95, z:130, w:95}` | Matches metadata; reused nav frame. |
| `.commander-portrait-backing` | `scn02c_mode_card_backing_blue.png` | 72 | 72 | 104 | 104 | 0.32px | `{x:72, y:104, z:72, w:104}` | Matches metadata; needs portrait-panel crop review. |
| `.commander-portrait-frame` | `scn02c_mode_card_frame_default_blue.png` | 72 | 72 | 104 | 104 | 0.32px | `{x:72, y:104, z:72, w:104}` | Matches metadata; needs portrait-panel crop review. |
| `.commander-edit-frame` | `scn02c_header_square_button_frame_default.png` | 150 | 150 | 150 | 150 | 0.14px | `{x:150, y:150, z:150, w:150}` | Matches metadata; intentionally smaller scale than header buttons. |
| `.deploy-frame` | `scn02c_deploy_button_frame.png` | 155 | 155 | 116 | 96 | 0.22px | `{x:155, y:96, z:155, w:116}` | Needs crop review for top/bottom orientation before changing metadata or USS. |

## Import Validation

- Shadow import log after slice: `/private/tmp/warline-ui-target-lock-scn02-shadow-import-after-slice.log`
- Result: Unity exit code `0`.
- SCN-02 result: no `Expected (<length>) but found` warnings remain for `SCN02_MainMenuContent.uss`.
- Non-SCN-02 log noise remains from Unity licensing/Android ADB checks and is not treated as SCN-02 visual import failure.

## Next Review Items

- Recapture SCN-02 after a valid runtime screenshot path exists in the shadow project.
- Review focused crops before changing PPU or sprite borders.
- Check the deploy frame top/bottom orientation first, because it is the only audited sliced chrome where USS top/bottom order differs from the literal meta y/w order.

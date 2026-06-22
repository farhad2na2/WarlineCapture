# Canvas Shared Chrome Asset Map

Source baseline: approved UI Toolkit SCN-02 Target Lock pass and its shared chrome reuse in Armory/Build Drawer/Match HUD styles.

This file maps the approved UI Toolkit art primitives to Canvas usage before prefab edits. Canvas prefab work should use these sprites through `Image` components, `Image.Type.Sliced` where borders are present, and Canvas `Selectable` sprite states for interactive controls.

## Rules

- Use SCN-02 menu chrome as the shared menu-adjacent foundation.
- Menu-adjacent screens reuse the same header and left navigation visual language; do not create screen-specific left nav chrome unless a later reference proves the shared style cannot work.
- Match HUD keeps its own header and HUD-specific chrome.
- Split baked multi-section backgrounds into separate Canvas panels instead of assigning one large background sprite to a whole side area.
- Use sprite states that cover the whole chrome frame, not a small tint panel layered inside the frame.
- For Canvas sliced images, trust the imported sprite border first. Only change `spritePixelsToUnits` or borders after screenshot evidence shows scale distortion.

## Header Chrome

| Canvas role | Approved sprite | Size | PPU | Border L/B/R/T | Canvas usage | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Header bar frame | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_header_bar_frame.png` | 1608x262 | 100 | 110 / 52 / 110 / 52 | Sliced `Image` | Shared menu header background. UI Toolkit slice scale was `0.28px`; Canvas uses imported border plus RectTransform size. |
| Brand/logo lockup | `Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV04Imagegen/Sprites/scn01_v04_logo_lockup.png` | art sprite | 100 | 0 / 0 / 0 / 0 | Simple `Image`, preserve aspect | Use the approved Warline Capture lockup, not generated text. |
| Resource chip frame | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_resource_chip_frame.png` | 1466x325 | 100 | 135 / 82 / 135 / 82 | Sliced `Image` | Use for menu resources and small status/resource chips when height is close to SCN-02. |
| Header square button default | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_header_square_button_frame_default.png` | 887x902 | 100 | 150 / 150 / 150 / 150 | Sliced `Image` and default button sprite | Use for menu/settings/inbox/header icon buttons. |
| Header icons | `scn02c_mail_icon.png`, `scn02c_settings_gear_icon.png`, `scn02c_menu_hamburger_icon.png` | icon sprites | 100 | 0 / 0 / 0 / 0 | Child simple `Image` | Keep icon aspect and avoid text labels inside icon-only buttons. |

## Left Navigation Chrome

| Canvas role | Approved sprite | Size | PPU | Border L/B/R/T | Canvas usage | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Nav button backing | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_backing_default.png` | 1527x485 | 100 | 130 / 95 / 130 / 95 | Sliced `Image` backing layer | Optional backing layer for richer selected/hover states. |
| Nav button default | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_default.png` | 1527x485 | 100 | 130 / 95 / 130 / 95 | Sliced default button sprite | Shared default left nav frame. |
| Nav button selected/current | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_selected.png` | 1675x506 | 100 | 130 / 95 / 130 / 95 | Sliced selected sprite | Use for current route and focused/hover emphasis when no separate hover sprite exists. |
| Nav chevron | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_chevron_icon.png` | icon sprite | 100 | 0 / 0 / 0 / 0 | Child simple `Image` | Keep right aligned and visible only where the route button needs a directional cue. |
| Nav icons | `scn02c_nav_campaign_target_icon.png`, `scn02c_nav_armory_ammo_icon.png`, `scn02c_nav_supply_crate_icon.png`, `scn02c_nav_command_shield_icon.png`, `scn02c_nav_tech_tree_nodes_icon.png`, `scn02c_nav_profile_tag_icon.png` | icon sprites | 100 | 0 / 0 / 0 / 0 | Child simple `Image` | Reuse icon slot, spacing, and text alignment across menu-adjacent screens. |

## Shared Panels And Cards

| Canvas role | Approved sprite | Size | PPU | Border L/B/R/T | Canvas usage | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Card backing blue | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_blue.png` | 726x1393 | 100 | 72 / 104 / 72 / 104 | Sliced backing `Image` | Good base for blue/default tall cards. |
| Card backing selected | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_backing_selected.png` | 783x1390 | 100 | 72 / 104 / 72 / 104 | Sliced selected backing | Use for selected cards when replacing full chrome, not as a small overlay. |
| Card frame blue | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_blue.png` | 726x1393 | 100 | 72 / 104 / 72 / 104 | Sliced frame `Image` | Default tall-card frame. |
| Card frame selected | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png` | 783x1390 | 100 | 72 / 104 / 72 / 104 | Sliced selected frame | Use for hover/current/selected card state where full-frame highlight is required. |
| Card label plate blue | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_blue.png` | 1554x434 | 100 | 135 / 92 / 135 / 92 | Sliced label plate | Reuse for labels with enough height for border preservation. |
| Card label plate selected | `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_label_plate_selected.png` | 1564x434 | 100 | 135 / 92 / 135 / 92 | Sliced selected label plate | Use with selected/current state, not as a tiny glow strip. |
| Large HUD panel | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png` | 1496x204 | 300 | 92 / 62 / 92 / 62 | Sliced panel `Image` | PPU differs from SCN-02; keep in HUD/shared utility use unless Canvas screenshots show scale mismatch. |
| Generic square panel | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_panel_frame.png` | square panel sprite | 100 | 96 / 96 / 96 / 96 | Sliced square panel `Image` | Use for square HUD panels and compact modal tiles. |

## Buttons, Tabs, Chips, And Dividers

| Canvas role | Approved sprite | Size | PPU | Border L/B/R/T | Canvas usage | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Rect button default | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_rect_button_frame.png` | 1527x485 | 300 | 90 / 60 / 90 / 60 | Sliced default button sprite | HUD/action button seed. PPU differs from menu chrome. |
| Rect button selected | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_rect_button_selected_frame.png` | 1675x506 | 300 | 90 / 60 / 90 / 60 | Sliced selected/hover button sprite | Full-frame selected state. |
| Square button default | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_button_frame.png` | 887x902 | 300 | 150 / 150 / 150 / 150 | Sliced default square button sprite | Use for command buttons and compact HUD controls. |
| Square button selected | `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_button_selected_frame.png` | 887x902 | 300 | 150 / 150 / 150 / 150 | Sliced selected/hover square button sprite | Full chrome replacement for hover/selected, not a partial overlay. |
| Build drawer tab idle | `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/chrome_05_tab_idle_bg.png` | 367x66 | 100 | 38 / 17 / 38 / 17 | Sliced tab default | Ready for Canvas tab backgrounds. |
| Build drawer tab selected | `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/chrome_04_tab_selected_bg.png` | 369x66 | 100 | 0 / 0 / 0 / 0 | Simple or filled `Image` | No border; avoid stretching too far unless replaced or border-tuned. |
| Build drawer card frame | `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/chrome_08_card_frame_standard.png` | 221x265 | 100 | 0 / 0 / 0 / 0 | Simple `Image` or fixed-size card chrome | No border; do not use as a heavily resized sliced image. |
| Build drawer selected card highlight | `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/chrome_07_selected_card_highlight_frame.png` | 119x107 | 100 | 0 / 0 / 0 / 0 | Fixed-size/highlight overlay only if it covers the whole card | If it does not cover the whole chrome at target size, replace with a full-frame selected sprite. |

## Import Audit

- SCN-02 menu chrome is already imported in `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/`.
- Shared Match HUD chrome is already imported in `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/`.
- Build Drawer tab/card chrome is already imported in `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/`.
- Current SCN-02 shared chrome PPU is consistently `100`.
- Some HUD button/panel chrome uses PPU `300`; keep this distinction during initial prefab application, then validate screenshots before changing imports.
- Many Build Drawer art pieces have zero sprite borders. Use them at fixed-ish sizes or tune borders before stretching them in Canvas.
- Thin chrome should stay visually crisp. Do not increase compression on shared chrome. If artifacts appear, prefer uncompressed or higher platform max size for the specific sprite rather than broad texture setting churn.
- Do not enable mipmaps broadly. Only consider mipmaps for large background/chrome sprites that are consistently scaled down and show shimmer in Game View/device validation.

## Canvas State Mapping

Buttons and selectable cards should use:

- Default: base frame/backing sprite.
- Hover/focus/highlight: full-frame selected or selected-adjacent sprite, plus small scale/translation only if the button remains aligned and does not overlap neighbors.
- Selected/current: full-frame selected sprite.
- Pressed/impact: selected sprite plus short scale/position response via existing Canvas transition/animation assets.
- Disabled: base sprite dimmed or dedicated disabled sprite where available; preserve text readability and do not remove the chrome silhouette.

## Follow-Up Decisions

- Create Canvas prefab/template state sets from the mapped sprites before editing every screen.
- Use `MainMenuLeftNavButton.prefab` as the shared left-nav state template.
- Use `PopupFrameView.prefab` as the shared popup chrome foundation only after active popup wiring decisions are confirmed.
- Do not retune PPU/borders globally from UI Toolkit slice-scale values alone; validate in shadow Canvas screenshots first.

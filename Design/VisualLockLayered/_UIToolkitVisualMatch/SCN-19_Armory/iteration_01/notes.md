# SCN-19 Armory Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Satisfied for the current shared-chrome/readability pass after slice 13. Slice 05 was not approved because the text was too small and the left navigation overlapped the middle panel; slice 07 cleaned up the right inspection area after the user called it messy, but its right-side action buttons were too small.

Scope for this iteration:

- Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for UI Builder validation only.
- Do not open or validate with the main project.
- Enable the `Match Game View` check-mark toggle and click `Fit Viewport` before every screenshot.
- Reuse the approved SCN-02 Main Menu header/nav language where applicable.
- Keep the Armory right inspection area as live panel-by-panel UI Toolkit sections, not one baked multi-section sprite.

Shared chrome decisions:

- Header: do not tune a separate Armory header against the mockup. Main-menu-adjacent screens should reuse the approved SCN-02 header language.
- Left nav: first slice changes Armory category rail toward the approved SCN-02 nav frame/selected state/chevron/spacing while retaining Armory-specific icons and labels.
- Middle roster: item cards now use the approved SCN-02 layered card backing/frame pattern instead of SCN-19 generated roster frame sprites.
- Right panel: the Armory reference shows a single large multi-section inspection panel. That must not be kept as one baked background. Active UXML decomposes it into `PortraitSection`, `LevelSection`, `StatsSection`, and separate CTA buttons. Active USS does not reference `scn19_inspection_panel_frame.png`.
- Right panel section and CTA chrome: section frames, progress bars, and CTA buttons now use SCN-02/SCN-08 reusable Target Lock sprites rather than SCN-19 baked panel/CTA/progress frame sprites.
- Slice 06 layout fix: the middle panel moved from `left: 17%` to `left: 19.4%` and narrowed from `width: 57%` to `width: 53.8%`, clearing the SCN-02-style left navigation rail.
- Slice 06 readability fix: Armory-specific roster, dropdown, inspection-panel, CTA, and bottom-tab typography was increased. The shared SCN-02 header was not restyled.
- Slice 07 right-side cleanup: the previous inspection column was replaced with separate commander-style title, portrait, level, stats, and action sections. Each section uses live UI Toolkit elements and SCN-02-style backing/frame chrome instead of a single baked multi-section background.
- Slice 08 action fix: the right action section height increased from `11.8%` to `18.5%`, the portrait/level/stats sections were compacted to make room, action button heights increased, and CTA label/icon sizes were enlarged so the Upgrade/Equip/Close controls read as real buttons instead of tiny strips.
- Slice 09-11 readability fix: roster cards were increased from `300px` to `520px` high and middle/right typography was enlarged so titles, states, level/type labels, stats, and action labels remain readable in the 4800x2160 UI Builder preview.
- Slice 12-13 stats fix: focused right-panel crops showed stat label/value overlap. The root cause was missing absolute positioning on `stat-name` and `stat-value`, so right stats now use explicit columns and no longer collide.

Validation observations:

- Shadow UI Builder opened SCN-19 successfully.
- Initial capture was invalid because `Match Game View` was unchecked and UI Builder used a `350 x 450` canvas.
- After enabling `Match Game View`, UI Builder reports `4800 x 2160`.
- `Fit Viewport` is applied.
- Early yellow overlay captures are invalid for visual matching because they were either selection contamination or SCN-19 sprite import issues.
- SCN-19 generated art PNGs were still imported as default textures. The current slice converts the SCN-19 generated PNG metas to UI Toolkit-compatible sprite imports: mipmaps off, sprite mode single, alpha transparency on, texture type sprite.
- Yellow warning placeholders were caused by SCN-19 USS importing before referenced art was registered in the shadow AssetDatabase. The static preview tool now refreshes the Armory, SCN-02, Match HUD, and splash art folders before importing/opening the SCN-19 USS/UXML.
- Final shadow Editor log import has no active SCN-19 invalid asset-path warnings and no active SCN-19 slice-scale length warnings.
- Final shadow UI Builder capture was taken after enabling Match Game View and clicking Fit Viewport.
- User rejected slice 05. Slice 06 capture shows the left nav and middle panel separated, with larger Armory-specific text.
- User then rejected the right side as messy. Slice 07 capture shows the right panel reorganized into clean commander-style sections similar to the approved SCN-02 commander area.
- The user correctly called out that slice 07 still had undersized right-side buttons. Do not move to the next screen until the latest shadow UI Builder screenshot is visually acceptable.
- Slice 13 focused crops confirm the right stats no longer overlap and the Upgrade, Equip, and Close buttons are full-width visible controls. The middle-card crop confirms card titles, owned/locked states, level, and type labels are readable.

Current artifacts:

- Invalid initial capture: `shadow_ui_builder_scn19_nav_slice01_full.png`
- Invalid canvas-state capture: `shadow_ui_builder_scn19_nav_slice01_match_fit_attempt.png`
- Valid capture setup but selection/import-contaminated: `shadow_ui_builder_scn19_nav_slice01_selection_clear_attempt.png`
- Clean shared chrome capture: `shadow_ui_builder_scn19_shared_chrome_slice05_final.png`
- Text/overlap revision capture: `shadow_ui_builder_scn19_text_overlap_fix_slice06.png`
- Right commander cleanup capture: `shadow_ui_builder_scn19_right_commander_clean_slice07.png`
- Enlarged right action buttons capture: `shadow_ui_builder_scn19_stats_columns_slice13_valid.png`
- Right-panel focused crop: `shadow_ui_builder_scn19_right_panel_slice13_valid_crop.png`
- Middle-card focused crop: `shadow_ui_builder_scn19_middle_cards_slice13_valid_crop.png`

Next loop:

1. Continue to SCN-03 Commander Profile using the same UI Builder/static shadow-project loop.
2. Keep SCN-02 header/nav shared chrome as the override for menu-adjacent screens.
3. If SCN-19 needs a later precision pass, start from the slice 13 crops and keep the work visual-only.

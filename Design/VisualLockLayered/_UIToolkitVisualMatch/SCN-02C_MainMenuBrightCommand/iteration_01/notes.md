# SCN-02 Main Menu Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Approved by user after preparation/inventory, first logo-source decision, typography scale slices, SCN-02 9-slice import-warning slice, SCN-02 scoped PPU/9-slice audits, and static UI Builder preview setup. The user-confirmed shield/star logo remains unchanged; SCN-02 text sizes were reduced from the original overflow state and then retuned against the valid 4800x2160 artifact; the collapsed diagnostics overlay is visually hidden; SCN-02 `-unity-slice-scale` values now include explicit `px` units so Unity accepts them. Runtime scaling follow-up: `RuntimePanelSettings` now uses UI Toolkit Scale With Screen Size (`m_ScaleMode: 2`) with the 4800x2160 reference and Expand-style aspect handling (`m_ScreenMatchMode: 2`). This matches the old CanvasScaler intent and prevents the 4800-authored fonts from rendering as raw oversized pixels at 1920x1080.

Allowed files touched in this slice:

- `Design/VisualLockLayered/_UIToolkitVisualMatch/target_to_toolkit_mapping.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/notes.md`
- `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/pixel_per_unit_audit.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/nine_slice_audit.md`
- `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss`
- `Assets/Game/UI Toolkit/RuntimePanelSettings.asset`
- `Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uss`
- `Assets/Game/Scripts/Editor/UiToolkitTargetLockStaticPreview.cs`
- `Assets/Game/Scripts/Editor/UiToolkitTargetLockStaticPreview.cs.meta`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/provisional_existing_runtime_20x9.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/provisional_target_vs_existing_20x9_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice04.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice04_canvas.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_typography_slice04_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/main_runtime_scale_with_screen_1920.png`

Structure lock:

- `HeaderContent` remains header.
- `LeftContent` remains left.
- `MiddleContent` remains middle.
- `RightContent` remains right.
- `FooterContent` remains footer.
- `MenuBackgroundContent` remains background.
- No UXML renames, removals, or structural moves.

Reference:
`Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/reference/scn02c_target_lock_warline_capture_bright.png`

Initial findings:

- The SCN-02 UXML already maps to the required structural regions: background, header, left, middle, right, and footer.
- SCN-02 uses the Target Lock MainMenuBrightCommand sprite family for most chrome and icons.
- The logo points at the Splash Loading TargetLockV04 shield/star logo, and the user confirmed this is the correct Main Menu logo. Do not replace it with `scn02c_brand_logo_lockup.png` in this pass.
- Provisional existing runtime capture: `/private/tmp/warline-uitoolkit-menu-startup.png`, saved into this iteration as `provisional_existing_runtime_20x9.png`. This is not accepted as the current reference artifact; it is only a diagnostic artifact.
- Provisional comparison artifact: `provisional_target_vs_existing_20x9_contact.png`.
- The provisional 20:9 capture shows large text overflow/collisions in resource chips, mode card titles, commander labels, and deploy CTA. Iteration 01 reduced those font sizes in `SCN02_MainMenuContent.uss` as the first visual-only fix batch.
- Most chrome sprites import at Pixel Per Unit `100`; no PPU edits are justified until the first UI Builder/runtime crop comparison proves chrome scale mismatch.
- Main and shadow projects both use Unity `6000.4.0f1`.
- Current SCN-02 follow-up required main-scene Game View verification because the user reported UI Builder and runtime mismatch.
- Allowed UI Toolkit/art files were synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; no C# files, scenes, prefabs, asmdefs, gameplay, ECS, or Canvas fallback files were synced.
- Shadow import validation passed with Unity exit code `0`.
- Fresh shadow log: `/private/tmp/warline-ui-target-lock-scn02-shadow-import-after-slice.log`.
- Added editor-only static preview menu `Game/UI Toolkit/Target Lock/Open SCN-02 Main Menu Static Preview` for this visual loop. Unity log confirmed it opened UI Builder with `playMode=False`.
- Unity static preview window observed through macOS UI scripting: `UI Builder` plus `Menu - WarlineCapture - Android - Unity 6.4 (6000.4.0f1) <Metal>`.
- macOS screenshot capture from this process failed with `could not create image from display`, including a display-specific retry. Do not substitute stale runtime/Game View images for the current UI Builder/static artifact.
- After the user required shadow-project capture, the heartbeat was updated and allowed UI Toolkit/art/static-preview files were synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow UI Builder static screenshot saved as `shadow_ui_builder_static_scn02_4800_target_screen.png`. The screenshot bitmap is `2940x1912` because it is the physical display capture, but the UI Builder inspector in the screenshot shows the canvas size set to `4800 x 2160`.
- Shadow UI Builder visible viewport crop saved as `shadow_ui_builder_static_scn02_visible_viewport.png`.
- Fresh static contact sheet saved as `target_vs_shadow_ui_builder_static_contact.png`.
- The first shadow UI Builder screenshot is not approval-ready because the UI Builder viewport is panned/partial; it proves the shadow capture path works and gives a current mismatch artifact, but the next iteration needs a cleaner centered/fit UI Builder viewport.
- Frontmost shadow UI Builder retry saved as `shadow_ui_builder_static_scn02_frontmost_retry.png`.
- Frontmost viewport crop saved as `shadow_ui_builder_static_scn02_frontmost_viewport.png`.
- Fresh frontmost contact sheet saved as `target_vs_shadow_ui_builder_frontmost_contact.png`.
- The frontmost contact sheet is usable for visual decisions. It shows the current live text hierarchy is too small against the Target Lock reference at the 4800x2160 static target, especially navigation labels, resource values, mode card titles, right-column microcopy, and the deploy CTA.
- Fresh shadow import no longer reports SCN-02 `Expected (<length>) but found` warnings for `SCN02_MainMenuContent.uss`.
- The fresh log still contains non-SCN-02 Unity licensing/Android ADB noise; it did not fail the import run.
- SCN-02 pixel-per-unit audit is recorded in `Design/VisualLockLayered/_UIToolkitVisualMatch/pixel_per_unit_audit.md`.
- SCN-02 9-slice audit is recorded in `Design/VisualLockLayered/_UIToolkitVisualMatch/nine_slice_audit.md`.
- The SCN-02 audit found all referenced sprites currently use PPU `100`; no PPU edits were made.
- The SCN-02 9-slice audit records 13 sliced USS selectors. The deploy frame remains the only explicit top/bottom orientation review item before future USS/meta changes.
- The initial clean-retry crop was wrong because it clipped the left side of the UI and included the UI Builder inspector. Corrected clean-canvas crop bounds were applied to the full shadow UI Builder screenshot.
- Typography slice 04 was approved by the user. The remaining visible differences are accepted as 20:9 composition choices against a 16:9 reference rather than proven PPU or 9-slice defects.
- Lessons from this approved pass are saved in `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md`.

Typography adjustments retained:

| USS selector | Old font size | New font size | Reason |
| --- | ---: | ---: | --- |
| `.resource-value` | 36px | 40px | Slice 04 lightly increased resource counters after the corrected canvas crop still showed under-sized values. |
| `.nav-text` | 36px | 44px | Left navigation labels were increased toward the reference hierarchy while keeping chevron clearance. |
| `.mode-title` | 58px | 70px | Mode card titles were increased for 4800x2160 readability without returning to cross-card overlap. |
| `.commander-title` | 36px | 40px | Commander heading remains contained while reading closer to the reference. |
| `.identity-name` | 28px | 32px | Commander identity row was increased while staying inside its frame. |
| `.identity-level` | 18px | 20px | Subtitle stays inside the identity row at 4800x2160. |
| `.commander-level-badge` | 32px | 36px | Level badge was increased while fitting its frame. |
| `.commander-progress-value` | 22px | 24px | Progress value remains inside the progress bar panel. |
| `.readiness-label` | 25px | 28px | Faction standing label was increased while staying inside the lower right frame. |
| `.deploy-text` | 66px | 82px | Deploy CTA text was increased for target hierarchy without overfilling. |

Runtime scale correction:

- `RuntimePanelSettings.asset` now uses `m_ScaleMode: 2`, the UI Toolkit serialized value for Scale With Screen Size.
- `RuntimePanelSettings.asset` now uses `m_ScreenMatchMode: 2`, the UI Toolkit serialized value used here for Expand-style aspect handling.
- `UiToolkitMenuSceneStartupValidation` now preserves those runtime scale settings instead of resetting the panel to the old 1920x1080 setup.
- The temporary attempt to compensate runtime mismatch by shrinking `.nav-text`, `.mode-title`, and `.deploy-text` was reverted; those selectors remain at the UI Builder-approved sizes.
- Reason: 4800-authored USS pixel sizes must be scaled by runtime PanelSettings for lower resolutions. Constant Pixel Size is only valid for one exact resolution and makes 1920x1080 too large.

Latest valid artifact:

- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_runtime_iter04_text_diagnostics_4800x2160.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_iter04_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_4800_target_screen.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_static_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_frontmost_retry.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_frontmost_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice03.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_typography_slice03_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice04.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice04_canvas.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_typography_slice04_contact.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/main_runtime_scale_with_screen_1920.png`

Superseded artifacts:

- `shadow_runtime_after_typography_slice_gui_1156x650.png`
- `shadow_runtime_iter02_typography_scaled_1156x650.png`
- `shadow_runtime_iter03_typography_scaled_1156x650.png`
- `shadow_runtime_iter03b_typography_scaled_1156x650.png`

Do not use the superseded 1156x650 artifacts for visual decisions.

9-slice import-warning adjustments retained:

| USS area | Existing intended value | New accepted value |
| --- | ---: | ---: |
| Header frame | `0.28` | `0.28px` |
| Resource chip | `0.2` | `0.2px` |
| Header icon button | `0.18` | `0.18px` |
| Nav frame | `0.22` | `0.22px` |
| Mode card fill/frame | `0.32` | `0.32px` |
| Mode card label plate | `0.2` | `0.2px` |
| Commander section frame/backing | `0.22` | `0.22px` |
| Commander portrait frame/backing | `0.32` | `0.32px` |
| Commander edit button | `0.14` | `0.14px` |
| Deploy frame | `0.22` | `0.22px` |

Next intended loop:

1. Keep SCN-02 stopped as the approved Target Lock menu baseline.
2. Use `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md` for the next screen.
3. Do not continue other screens until the user explicitly approves the next surface.

Validation:

- Passed `git diff --check` after syncing typography slice 04 to the shadow project and before this notes update.
- Shadow UI Builder/static screenshot path is working; typography slice 04 contact sheet was approved by the user.
- Main-scene Game View follow-up captured at 1920x1080 after the Scale With Screen Size fix: `main_runtime_scale_with_screen_1920.png`.
- No further SCN-02 edits are planned unless the user requests revisions.

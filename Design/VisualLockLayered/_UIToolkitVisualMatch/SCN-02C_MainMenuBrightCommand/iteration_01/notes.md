# SCN-02 Main Menu Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Preparation/inventory, first logo-source decision, typography scale slice, SCN-02 9-slice import-warning slice, and SCN-02 scoped PPU/9-slice audits. The user-confirmed shield/star logo remains unchanged; SCN-02 text sizes were reduced to address the provisional 20:9 capture's major text overflow; SCN-02 `-unity-slice-scale` values now include explicit `px` units so Unity accepts them.

Allowed files touched in this slice:

- `Design/VisualLockLayered/_UIToolkitVisualMatch/target_to_toolkit_mapping.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/notes.md`
- `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/pixel_per_unit_audit.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/nine_slice_audit.md`
- `Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/provisional_existing_runtime_20x9.png`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/provisional_target_vs_existing_20x9_contact.png`

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
- Provisional existing runtime capture: `/private/tmp/warline-uitoolkit-menu-startup.png`, saved into this iteration as `provisional_existing_runtime_20x9.png`. This is not accepted as a shadow capture; it is only a diagnostic artifact.
- Provisional comparison artifact: `provisional_target_vs_existing_20x9_contact.png`.
- The provisional 20:9 capture shows large text overflow/collisions in resource chips, mode card titles, commander labels, and deploy CTA. Iteration 01 reduced those font sizes in `SCN02_MainMenuContent.uss` as the first visual-only fix batch.
- Most chrome sprites import at Pixel Per Unit `100`; no PPU edits are justified until the first UI Builder/runtime crop comparison proves chrome scale mismatch.
- Main and shadow projects both use Unity `6000.4.0f1`.
- The shadow project exists, but it is missing the current SCN-02 UI Toolkit UXML/USS files and the current `UiToolkitMenuSceneStartupValidation.RunPlayModeScreenshot` C# runner. Since this visual loop forbids C# syncs, shadow capture is blocked until an allowed non-C# capture path exists or the user approves a separate tooling step outside this loop.
- Allowed UI Toolkit/art files were synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; no C# files, scenes, prefabs, asmdefs, gameplay, ECS, or Canvas fallback files were synced.
- Shadow import validation passed with Unity exit code `0`.
- Fresh shadow log: `/private/tmp/warline-ui-target-lock-scn02-shadow-import-after-slice.log`.
- Fresh shadow import no longer reports SCN-02 `Expected (<length>) but found` warnings for `SCN02_MainMenuContent.uss`.
- The fresh log still contains non-SCN-02 Unity licensing/Android ADB noise; it did not fail the import run.
- SCN-02 pixel-per-unit audit is recorded in `Design/VisualLockLayered/_UIToolkitVisualMatch/pixel_per_unit_audit.md`.
- SCN-02 9-slice audit is recorded in `Design/VisualLockLayered/_UIToolkitVisualMatch/nine_slice_audit.md`.
- The SCN-02 audit found all referenced sprites currently use PPU `100`; no PPU edits were made.
- The SCN-02 9-slice audit records 13 sliced USS selectors. The deploy frame remains the only explicit top/bottom orientation review item before future USS/meta changes.

Typography adjustments retained:

| USS selector | Old font size | New font size | Reason |
| --- | ---: | ---: | --- |
| `.resource-value` | 54px | 38px | Resource counters collided with icons/plus controls in 20:9 capture. |
| `.nav-text` | 44px | 32px | Left navigation labels were too large relative to target and crowded the chevron. |
| `.mode-title` | 70px | 50px | Mode card titles overlapped across adjacent cards. |
| `.commander-title` | 52px | 36px | Commander heading exceeded the target panel scale. |
| `.identity-name` | 40px | 29px | Commander identity row clipped/overlapped the right panel. |
| `.identity-level` | 31px | 22px | Subtitle overlapped under commander identity. |
| `.commander-level-badge` | 48px | 34px | Level badge was oversized inside its frame. |
| `.commander-progress-value` | 34px | 24px | Progress value crowded the progress bar panel. |
| `.readiness-label` | 36px | 25px | Faction standing label was oversized. |
| `.deploy-text` | 92px | 66px | Deploy CTA text overfilled the button. |

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

1. Sync allowed UI Toolkit/art files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after approval.
2. Use an existing shadow-project UI Toolkit screenshot path if one is added outside this visual-only loop, or record the capture blocker explicitly.
3. Save baseline 16:9 and 20:9 captures under `baseline/`.
4. Generate comparison contact sheets against the saved Target Lock reference.
5. Classify the first visible differences, with Pixel Per Unit and 9-slice issues ahead of layout/font edits.
6. Recapture after the typography pass before making any PPU, 9-slice, or panel-position changes.

Validation:

- Passed `git diff --check` for the tracker and SCN-02 visual-match markdown artifacts.
- Pending post-fix UI Builder/shadow recapture; allowed visual files were synced to shadow, but no C# or forbidden files were synced.
- Passed shadow import validation after syncing allowed visual files only.
- Pending post-fix runtime screenshot; the current screenshot runner is C# and is not present in the shadow project.
- Pending focused crop review before any PPU or sprite border edits.

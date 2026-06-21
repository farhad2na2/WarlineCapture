# SCN-02 Main Menu Target Lock Iteration 01 Notes

Date:
2026-06-21

Status:
Preparation/inventory slice only. No visual implementation edits have been made yet.

Allowed files touched in this slice:

- `Design/VisualLockLayered/_UIToolkitVisualMatch/target_to_toolkit_mapping.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/notes.md`
- `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`

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
- The logo currently points at the Splash Loading TargetLockV04 logo instead of the SCN-02 layer-pack `scn02c_brand_logo_lockup.png`; this is a likely first visual check, not yet changed.
- Most chrome sprites import at Pixel Per Unit `100`; no PPU edits are justified until the first UI Builder/runtime crop comparison proves chrome scale mismatch.
- Existing shadow capture methods appear to exist from prior Canvas/UI work, but capture has not run in this slice.

Next intended loop:

1. Sync allowed UI Toolkit/art files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after approval.
2. Run existing SCN-02 capture methods in the shadow project.
3. Save baseline 16:9 and 20:9 captures under `baseline/`.
4. Generate comparison contact sheets against the saved Target Lock reference.
5. Classify the first visible differences, with Pixel Per Unit and 9-slice issues ahead of layout/font edits.

Validation:

- Passed `git diff --check` for the tracker and SCN-02 visual-match markdown artifacts.

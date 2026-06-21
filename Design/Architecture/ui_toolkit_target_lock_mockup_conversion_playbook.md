# UI Toolkit Target Lock Mockup Conversion Playbook

Purpose:
Capture the reusable workflow learned from the approved SCN-02 Main Menu visual-match pass so future UI Toolkit mockup conversions move faster and avoid the same mistakes.

Last updated:
2026-06-21

Approved reference pass:
`SCN-02 Main Menu iteration 01`

Approved artifact:
`Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_typography_slice04_contact.png`

## Core Rule

Do not tune UI Toolkit visuals from stale runtime captures, bad crops, or screenshots that include UI Builder chrome. First produce a clean static comparison artifact, then make one visual change batch.

## Project And Capture Rule

- Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for Unity Editor and UI Builder preview when available.
- Do not open the main project for visual capture unless the user explicitly asks.
- UI Builder/static preview is allowed.
- Do not enter PlayMode when the current loop says static/UI Builder only.
- Do not use runtime/Game View screenshots as acceptance evidence for static/UI Builder-only passes.
- Sync only allowed visual files to the shadow project before capture.
- Before taking a UI Builder screenshot, enable the `Match Game View` check-mark toggle so the preview uses the intended aspect/resolution.
- After the `Match Game View` toggle is enabled, click `Fit Viewport` so the UI is scaled into the viewport before capturing.

## File Scope Rule

Allowed by default for visual-match loops:

- `Assets/Game/UI Toolkit/**/*.uxml`
- `Assets/Game/UI Toolkit/**/*.uss`
- `Assets/Game/Art/UI/**/*.png`
- `Assets/Game/Art/UI/**/*.png.meta`
- `Design/Architecture/**/*.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/**`

Do not edit runtime C#, ECS, gameplay, composition, asmdefs, scenes, prefabs, or Canvas fallback files during visual-only passes unless the user separately approves that exact boundary.

## Structure Lock Rule

Preserve the existing UI Toolkit shell and screen structure.

Never rename, remove, or collapse these shell roles during target-lock styling:

- `HeaderRegion`
- `LeftRegion`
- `MiddleRegion`
- `RightRegion`
- `FooterRegion`
- `MenuBackgroundRegion`
- screen slots
- popup slot
- modal overlay

Move, resize, restyle, or swap sprites through USS and art import settings only.

## Shared Chrome Override Rules

These rules override pixel-level target matching for future screens.

### Header Reuse

- Do not convert or restyle separate main-menu headers for Armory, Commander Profile, Supply, Command, Tech Tree, Profile, popups, or other main-menu-adjacent screens.
- Reuse the approved SCN-02 Main Menu header chrome for those screens.
- Do not build a different Armory header just because the Armory mockup shows one.
- Only Match HUD is allowed to have its own header because it is an in-match gameplay surface, not a main-menu screen.

### Left Navigation Reuse

- Reuse the approved SCN-02 Main Menu left navigation style and background for other main-menu screens.
- Armory and related menu screens may change nav icons, labels, selection state, and route-specific active item only.
- Do not introduce a different left-nav background, button frame style, spacing language, or chrome treatment for Armory or other menu screens.
- If a mockup shows a different left nav, treat that as overridden by the shared SCN-02 nav contract.

### Right Panel Decomposition

- Do not preserve a large baked right-side panel sprite when it contains multiple visual sections.
- Decompose it into separate UI Toolkit panels like the approved SCN-02 right-side commander area.
- Each section should have its own frame/backing/content elements so state, text, progress, badges, and future data can remain live.
- A baked sprite is acceptable only for purely decorative background art with no separable live UI sections.

### Target Match Priority

When a mockup conflicts with these shared chrome rules, prefer the shared UI system rule:

1. approved SCN-02 header/nav language;
2. live panel-by-panel composition;
3. clean Target Lock visual style;
4. mockup pixel match.

## Iteration Loop

1. Open the target UXML in the shadow project UI Builder.
2. Confirm the intended static canvas size in UI Builder.
3. In UI Builder, enable the `Match Game View` check-mark toggle and confirm the aspect/resolution is correct.
4. Click `Fit Viewport` after the `Match Game View` toggle is enabled so the preview is scaled correctly.
5. Capture a full shadow Editor screenshot.
6. Crop only the UI canvas, excluding UI Builder sidebars, toolbars, inspectors, tabs, and desktop chrome.
7. Generate a contact sheet against the saved reference mockup.
8. Inspect the contact sheet before editing.
9. Classify the mismatch as `PPU`, `9-slice`, `position`, `padding`, `font`, `sprite`, `state`, `content`, `responsive`, or `artifact`.
10. Fix `PPU` and `9-slice` issues before compensating with layout or font changes.
11. Apply one small visual-only change batch.
12. Run `git diff --check`.
13. Sync only the changed allowed files to the shadow project.
14. Refresh UI Builder in the shadow project.
15. Reconfirm the `Match Game View` check-mark toggle is enabled, click `Fit Viewport`, then recapture, recrop, and regenerate the contact sheet.
16. Update the tracker with artifact paths, validation, and the current status.
17. Stop at user-verification gates instead of continuing into more screens.

## Crop Quality Rule

A screenshot is not useful just because it exists.

Reject the artifact and recrop when:

- the left nav, logo, footer, or right panel is clipped;
- the crop includes the UI Builder inspector or hierarchy;
- the crop includes the macOS dock or menu bar;
- the reference and candidate are not scaled to a comparable visual height;
- the crop is from an old runtime or Game View path after the scope changed to UI Builder/static.
- `Match Game View` was not enabled before capture;
- `Fit Viewport` was not clicked after the correct aspect/resolution was set.

For SCN-02, the bad crop made the UI look wrong by clipping the left side and including the inspector. The corrected clean-canvas crop changed the decision from layout speculation to typography-density tuning.

## Typography Rule

Tune typography only after the crop is clean.

When text is too large:

- reduce font size enough to remove clipping and overlap;
- preserve label hierarchy;
- do not hide overflow by shrinking containers blindly.

When text is too small:

- increase key labels in one coherent typography-density slice;
- verify nav labels, card titles, resource chips, commander microcopy, and primary CTA together;
- avoid returning to the previous oversized-font failure mode.

SCN-02 approved direction:

- Do not treat the earliest oversized typography as a target.
- Do not overreact to a bad crop by changing layout.
- Once the clean canvas crop is available, typography may be tuned against the real visual mismatch.

## Pixel Per Unit And 9-Slice Rule

PPU and 9-slice are first-class visual acceptance items.

- If chrome thickness is wrong while element bounds are right, inspect PPU before layout.
- If corners distort or borders stretch unevenly, inspect sprite borders and USS slice values before resizing panels.
- Use explicit USS length units for slice scale values, such as `0.22px`.
- Record PPU and 9-slice findings even when no import changes are made.

SCN-02 result:

- PPU audit recorded.
- No PPU changes were needed.
- 9-slice audit recorded.
- USS slice-scale unit fixes were retained.

## Static Preview Tooling Rule

Editor-only tooling is acceptable when it only improves static UI Builder preview and does not change runtime behavior.

For SCN-02 this was acceptable:

- `Assets/Game/Scripts/Editor/UiToolkitTargetLockStaticPreview.cs`
- shadow-project sync of the editor-only preview launcher;
- UI Builder open/refresh without PlayMode.

Do not use editor tooling to bypass the visual scope by running gameplay or changing runtime behavior.

## Handoff Rule

A screen should move to user verification when:

- the latest contact sheet is based on a clean UI Builder/static crop;
- the user-requested resolution/canvas path was used;
- artifacts are saved under the surface iteration folder;
- PPU and 9-slice findings are recorded;
- `git diff --check` passes;
- no forbidden source files were edited for the current visual scope.

Do not mark a screen `Target matched` until the user explicitly approves it.

SCN-02 approval means:

- use the shield/star Warline Capture logo;
- keep the slice 04 typography-density direction;
- use the corrected clean-canvas contact sheet workflow for later screens;
- continue later screens only after explicit user direction.

## Recommended Next Surface Workflow

For each future screen:

1. Start from the reference mockup and current UXML/USS.
2. Record the structure mapping before edits.
3. Open in shadow UI Builder.
4. Capture a clean canvas crop.
5. Generate the first contact sheet.
6. Fix obvious asset/PPU/9-slice problems.
7. Tune layout and typography in small slices.
8. Save the accepted artifact path in the tracker.
9. Stop for user verification before moving to the next screen.

## SCN-02 Lessons To Reuse

- The correct logo is the shield/star Warline Capture lockup confirmed by the user.
- The shadow project must be the capture surface.
- A bad crop can make good UI look wrong; fix the artifact before editing.
- Text tuning should be judged on a clean contact sheet, not on memory of earlier screenshots.
- The final user-approved state can still differ from a pixel-perfect mockup when the requested target is 4800x2160 static UI Builder but the saved reference mockup has a different aspect ratio.
- Once the user approves a screen, stop and record that approval instead of continuing speculative refinements.

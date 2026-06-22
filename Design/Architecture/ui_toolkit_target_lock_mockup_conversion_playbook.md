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

## Runtime Scaling Rule

UI Builder and Game View do not apply scale in the same way. Treat UI Builder as the authoring/reference view, then validate runtime scaling separately in Game View.

For the Target Lock UI:

- Author and tune USS against the `4800x2160` reference canvas in UI Builder.
- Keep runtime `PanelSettings` on UI Toolkit Scale With Screen Size, not Constant Pixel Size.
- Use `RuntimePanelSettings.asset` values:
  - `m_ScaleMode: 2`
  - `m_ReferenceResolution: {x: 4800, y: 2160}`
  - `m_ScreenMatchMode: 2`
  - `m_Match: 0.5`
- Preserve these values in editor validation/repair tooling. Do not let validation reset the panel to `1920x1080`, Constant Pixel Size, or a different screen match mode.
- Do not compensate for runtime aspect or resolution problems by shrinking screen-specific fonts in USS.
- If text looks correct at `4800x2160` but massive at `1920x1080`, inspect `PanelSettings` scale mode first.
- Constant Pixel Size is only valid for one exact target resolution; it is not Canvas-like and will make lower resolutions render 4800-authored font sizes too large.
- UI Builder does not prove runtime aspect scaling. Validate runtime at the common aspect/resolution cases in Game View after the static UI Builder pass is visually accepted.

SCN-02 correction:

- The temporary Constant Pixel Size fix made the main menu look acceptable only at the current 4800-style view, then failed at `1920x1080`.
- The correct fix was UI Toolkit Scale With Screen Size (`m_ScaleMode: 2`) with the `4800x2160` reference and Expand-style aspect handling (`m_ScreenMatchMode: 2`).
- The validation artifact for the corrected `1920x1080` runtime pass is `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/main_runtime_scale_with_screen_1920.png`.

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

### Selection And Hover Quality

- Selected, hover, focus, active, and disabled states must read as stateful chrome, not as cheap translucent rectangles pasted over content.
- Prefer a dedicated state sprite, state-specific frame image, or frame replacement when the mockup shows the state over the border/chrome.
- Do not add a small inner overlay when the reference state covers the full panel/card frame.
- Repeated controls must use the same base template. A selected example in the mockup is a state example, not permission to make the first item a one-off layout.
- Before leaving a repeated card/button/row family, compare left/right padding, outer margins, repeated gap values, and state coverage for every item in the family.

### Button Interaction Standard

- Every remaining button-like control family must have explicit `selected`/active, `:hover`, and `:focus` visual states unless the control is purely passive or hidden.
- Button states should use premium chrome-level treatment: selected/hover/focus frame replacement, state sprite, or state-specific frame expansion, not only text color or a flat overlay.
- Add a restrained interaction impact for clickable controls: short `translate`/`scale` transitions that make selected/hover/focus controls sit visibly forward without causing overlap or layout shift.
- Tune the amount per layout density. Dense repeated cards should use smaller scale/lift than isolated square command buttons.
- Apply state styling to the whole button family, not just the mockup example item. A highlighted mockup item, such as Move or squad 1, defines the reusable state for all peers.

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
17. Continue to the next screen when the latest static/UI Builder result is visually satisfactory and no explicit approval gate is active.
18. Stop only for unresolved visual direction, missing target assets, capture/tooling failure, explicit user pause, or a screen-specific approval gate.

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

## Repeated Template Gate

Repeated UI is accepted by template quality, not by overall screen readability.

Before moving to another screen, make a focused crop for each repeated family that can hide defects in a broad screenshot:

- squad cards, unit cards, catalog cards, commander cards, and inventory cards;
- left navigation buttons, footer buttons, command buttons, and right-rail buttons;
- list rows, stat rows, tab rows, progress bars, sliders, and segmented status strips.

Reject the screen and keep iterating when any repeated item has:

- overlapping progress/slider/value text;
- extra progress bars, sliders, or decorative strips that are not present in the reference;
- unreadable title, value, or state label;
- status bars that are missing, clipped, or visually detached from the card;
- inconsistent spacing between repeated siblings;
- chrome that is visibly weaker, thicker, stretched, or misaligned compared with the approved shared baseline.

When the reference shows one repeated item in selected, highlighted, hover, disabled, or damaged state, treat it as a state example. Keep the base template identical across siblings, then express the variant as a reusable class or pseudo-class that can move to any item at runtime.

SCN-08 correction:

- The Match HUD was incorrectly advanced from a broad crop while the five squad cards still had poor hierarchy and health/slider overlap.
- The corrected pass required a focused squad-tray crop, larger repeated cards, separated health bar/value text, segmented status pips, and a reusable selected/hover state instead of a one-off first-card treatment.
- Future HUD and catalog screens must not move forward until their repeated-card crop has the same level of evidence.

## Panel-By-Panel Cleanliness Gate

Do not accept a dense screen from one broad full-screen screenshot. Every visible panel region needs its own focused alignment pass before the screen can be `Satisfied for current pass` or `Target matched`.

For each screen, inspect and crop each major panel group separately:

- header/resources/current-order area;
- left panel stack;
- middle content area;
- right panel stack or quick-rail;
- footer/tray/command/minimap areas;
- every popup, drawer, modal, and overlay panel.

Reject the screen and keep iterating when any panel group has:

- panel edges that do not align to neighboring chrome or the mockup grid;
- inconsistent left/right/top/bottom padding inside related panels;
- text, icons, sliders, progress bars, or values touching chrome borders;
- baked multi-section backgrounds where live panel-by-panel composition is required;
- mismatched frame thickness, bad 9-slice corners, or inconsistent Pixel Per Unit;
- unclear selected/hover/focus/active button states;
- visual clutter that makes the panel read as messy even when all elements are technically visible.

SCN-08 Match HUD rule:

- Do not advance SCN-08 from the bottom squad/command fixes alone.
- Recheck header/resources/current order, left objectives/selected-unit stack, right threat/quick rail, minimap, feedback panel, footer command rail, and squad tray as separate focused crops.
- When user feedback identifies one bad panel group on a dense HUD, audit the neighboring and symmetrical panel groups in the same pass. Do not only repair the specific panel named by the user while leaving other obvious alignment, padding, state, or crop mismatches visible.
- Keep SCN-08 `In progress` until those panel groups are aligned, clean, and comparable to the reference, or until a user-approved exception is recorded.

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

A screen can move forward to the next surface when:

- the latest contact sheet is based on a clean UI Builder/static crop;
- the user-requested resolution/canvas path was used;
- artifacts are saved under the surface iteration folder;
- PPU and 9-slice findings are recorded;
- every repeated template has a focused crop or clearly visible full-screen crop, including every card, row, tab, and button family;
- every crop is reviewed for overlaps, clipped text, missing status bars, stretched/chrome artifacts, and mismatched reference hierarchy;
- `git diff --check` passes;
- no forbidden source files were edited for the current visual scope.

Do not mark a screen `Target matched` until the user explicitly approves it. For the remaining-screen loop, use `Satisfied for current pass` when the screen is clean enough to continue without per-screen approval.

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
9. Continue to the next screen if the result is visually satisfactory and no blocker remains.

## SCN-02 Lessons To Reuse

- The correct logo is the shield/star Warline Capture lockup confirmed by the user.
- The shadow project must be the capture surface.
- A bad crop can make good UI look wrong; fix the artifact before editing.
- Text tuning should be judged on a clean contact sheet, not on memory of earlier screenshots.
- The final user-approved state can still differ from a pixel-perfect mockup when the requested target is 4800x2160 static UI Builder but the saved reference mockup has a different aspect ratio.
- Once the user approves a screen, stop and record that approval instead of continuing speculative refinements.

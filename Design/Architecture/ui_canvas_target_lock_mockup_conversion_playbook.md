# UI Canvas Target Lock Mockup Conversion Playbook

Purpose:
Capture the reusable Canvas workflow learned during the SCN-02 Main Menu recovery pass so future Target Lock Canvas conversions move faster and avoid repeating the same visual mistakes.

Last updated:
2026-06-23

Primary reference:

- `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md`
- Approved UI Toolkit SCN-02 Main Menu Target Lock pass
- Current Canvas tracker: `Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`

## Core Rule

Canvas Target Lock work is accepted panel-by-panel and button-by-button, not by one broad screenshot.

Do not move to another panel, popup, or screen while the current panel family has obvious sizing, padding, chrome, sprite, hover, selected, or containment defects.

## Runtime Boundary

- Canvas is the runtime UI path for this migration.
- UI Toolkit is a visual reference only.
- Do not edit UI Toolkit files while doing Canvas visual migration.
- Do not edit gameplay, ECS, composition, route behavior, scenes, or runtime C# unless the user explicitly approves that boundary.
- Preserve Canvas prefab shell structure, runtime-bound GameObject names, serialized fields, view bindings, and route hooks.
- Validate in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` first when available.

## Main Menu First Rule

For SCN-02 and other user-gated screens:

1. Finish the current screen before starting any other screen.
2. Finish each visible panel family before moving to a different panel family.
3. If the user points out a defect in a panel, audit the neighboring panel family in the same screen before claiming progress.
4. Keep the screen `In progress` until the latest capture has no known visible defects, or until a clearly documented blocker exists.
5. Every status handoff must include `Still wrong / next iteration`.

## Screenshot And Comparison Loop

Every iteration must use this loop:

1. Sync only allowed Canvas visual files to the shadow project.
2. Capture the current screen in the shadow project.
3. Generate a reference-vs-candidate contact sheet at comparable scale.
4. Inspect the contact sheet before editing.
5. List visible defects by panel family.
6. Classify each defect as `sprite`, `PPU`, `9-slice`, `size`, `anchor`, `padding`, `font`, `state`, `mask`, `content`, or `render order`.
7. Fix `PPU`, `9-slice`, and sprite selection before compensating with anchors or sizes.
8. Apply one coherent visual batch.
9. Run `git diff --check`.
10. Capture again and repeat.

Do not report a candidate as approval-ready unless the latest capture and focused crops have been inspected after the final edit.

## Approved SCN-02 Recovery Lessons

The approved SCN-02 Canvas main menu was reached only after treating every visible area as a panel family and checking the real Canvas render, not just prefab values.

Carry these lessons into every later screen:

- Start from the approved SCN-02 Canvas result for shared menu chrome, not from the earlier rejected attempts.
- Check one panel family completely before moving on: parent frame, child frames, icon/text insets, state graphics, sibling spacing, and all aspect captures.
- Do not trust a full-screen capture alone. Make focused crops for the panel being fixed and compare those crops against the target/reference.
- If a user points out one bad control, inspect the whole family of that control. Settings and Inbox were not only icon issues; they exposed header centerline, button frame size, sliced-frame PPU, and sibling resource-chip alignment issues.
- Use exact family rhythm before subjective polish: same height, same Y centerline, same chrome thickness, same icon safe inset, and consistent gap.
- Never ask for approval while a panel has known visible defects such as uneven padding, sibling overlap, icon-border collision, mismatched chrome thickness, or text that looks too small inside its frame.
- When a candidate is rejected, update the tracker immediately so future loops do not resume from stale “approval-ready” language.

## Header And Action Button Rule

Header controls are approved as one family: logo, resource chips, plus buttons, and square action buttons.

For each header pass:

- Compare the resource chips and action buttons in one focused crop.
- Align square action buttons to the resource chip vertical centerline.
- Keep Settings/Inbox-style action buttons the same visual height as the header resource chip family unless the target explicitly differs.
- Do not shrink the square button to create padding. Keep the frame rhythm correct, then inset the icon inside it.
- Confirm action icons and plus icons have enough safe padding from every border.
- Confirm the plus icon does not touch or visually crowd the right chrome.
- Confirm resource icon, value text, and plus icon have visible separation and no overlap.
- If the frame border looks too fat while the element size is correct, fix PPU/`m_PixelsPerUnitMultiplier` before resizing.

SCN-02 approved header lesson:

- `InboxButton` and `SettingsButton` looked wrong because the sliced square-frame Image rendered with the wrong Canvas scale treatment.
- The approved correction keeps both buttons at the resource-family `160x160` rhythm and uses `m_PixelsPerUnitMultiplier: 5.5` on the sliced square-frame Image to match the UI Toolkit thin-chrome slice scale.
- The icons are then padded inside the frame; they are not allowed to overlap or visually touch the border.

## PPU And 9-Slice Rule

Thin Target Lock chrome depends on correct sprite import and Image setup.

- If a border is too thick but the element size is close, inspect `spritePixelsToUnits` before resizing.
- If corners are distorted, stretched, or soft, inspect the sprite border and Image `m_Type`.
- Sliced frame Images must use `m_Type: 1`.
- Do not use a single large baked multi-section background to fake several panels.
- Do not stretch a decorative full-panel sprite across unrelated live sections.
- Prefer separate sliced Canvas Images for each live panel section.
- Record any PPU or 9-slice tuning in the tracker.

SCN-02 lesson:

- The rejected main menu had oversized chrome, partly from using correct art with the wrong scale treatment.
- Pixel Per Unit and 9-slice must be checked before nudging anchors for settings/inbox buttons, deploy CTA, nav rows, commander rows, and mode-card frames.
- Per-Image `m_PixelsPerUnitMultiplier` can be the right fix when one sliced frame is correct art but wrong Canvas chrome thickness at the current element size.
- Do not compensate for bad PPU by making controls tiny, moving children around, or adding oversized margins.
- After any PPU or slice change, capture the screen and a focused crop. A prefab value that looks mathematically correct can still read wrong in the rendered Canvas.

Canvas sliced-frame inspection order:

1. Confirm the sprite has usable borders.
2. Confirm the Canvas Image is `Sliced`.
3. Confirm the element size matches the reference rhythm.
4. Tune sprite PPU or Image `m_PixelsPerUnitMultiplier` until chrome thickness matches.
5. Tune child padding and text/icon sizes only after chrome thickness is right.

## Panel Decomposition Rule

If a mockup or generated asset contains a single background with multiple embedded sections, split it into live Canvas sections.

Use separate panels for:

- header/title strip;
- portrait/image viewport;
- identity/name row;
- progress/readiness row;
- faction/status row;
- button/CTA row;
- card title plate;
- repeated list or tab rows.

Each section needs its own frame/backing, padding, mask if required, and interaction state if clickable.

## Panel Family Acceptance Gate

A panel family is not done until these are all true in the latest capture:

- no visible child overlaps the frame border;
- no sibling panels overlap each other;
- all contents have consistent left/right and top/bottom padding;
- labels are readable at the smallest validated aspect;
- icons are centered and have safe border padding;
- repeated rows/cards share the same size and gap rhythm;
- hover/selected/pressed graphics cover the full intended frame;
- any selected/active panel still lines up with its unselected siblings;
- background art is contained by the intended mask or frame;
- the panel still looks clean in focused crops, not only in the full screenshot.

For right-side detail stacks like SCN-02 Commander:

- keep the stack far enough from the header;
- split title, portrait, identity, progress, and status rows into separate live panels;
- give every row enough internal padding before increasing text size;
- avoid empty right-edge margin by sizing and positioning the whole stack as a unit;
- inspect portrait and top-row overlap specifically after any vertical move.

## Fine Detail Rule

Small premium details need real Canvas image treatment.

- Do not fake chrome dividers, stars, trim, chevrons, or badges with text characters such as `----- * -----`.
- If a reference uses a fine star/divider finish, use a real sprite, sliced line image, or separate Canvas Image primitives with controlled color and size.
- Reject a quick approximation when the capture reads cheap or noisy, even if it technically resembles the reference.
- Keep the task open rather than marking the panel done with a low-quality substitute.

SCN-02 lesson:

- A text-based mode-card divider was tested and rejected because it looked noisy and low quality in the shadow capture.
- The correct next pass is a real sprite/image solution for the lower card divider/star finish, or a documented decision to omit the detail if no high-quality asset is available.

## Button And Selectable State Rule

Every button/selectable/card family must have complete state coverage before the panel is considered done.

Required states:

- default;
- hover/highlight/focus;
- selected/current;
- pressed/impact;
- disabled.

Use premium chrome-level state changes:

- whole-frame sprite replacement;
- selected frame or glow that covers the full chrome;
- restrained scale/position impact only when it does not overlap neighbors;
- text/icon color as a supplement, not the only state.

Do not add a small colored overlay on top of a panel when the reference shows the state replacing or covering the whole frame.

## Repeated Family Gate

Repeated controls are approved as a family.

Before leaving a repeated family, check:

- every item has the same base size;
- left/right padding matches;
- top/bottom padding matches;
- gap values are consistent;
- selected state can move to any item;
- hover/pressed states do not overlap siblings;
- labels remain readable at all validated aspects;
- icons are centered and not clipped;
- chrome thickness is consistent across all items.

Examples:

- left navigation rows;
- mode cards;
- commander rows;
- resource chips;
- settings/inbox square buttons;
- deploy CTA;
- popup action buttons;
- build cards;
- squad panels.

## Fast Iteration Checklist

Use this order to move faster without skipping quality:

1. Open the latest target/reference and latest candidate together.
2. Name the exact panel family being fixed.
3. Create a focused crop for that family before editing.
4. Fix sprite/PPU/9-slice first.
5. Fix parent frame size and Y/X rhythm next.
6. Fix child icon/text padding next.
7. Fix state graphics last.
8. Capture the same focused crop again.
9. Only then update the full all-aspect contact sheet.
10. Record what changed and what still looks wrong.

Do not start a second panel family during this sequence unless the first family is visibly clean.

## SCN-02 Approved Baseline Checklist

SCN-02 Main Menu iteration 66 is user-approved and is the shared Canvas baseline for menu-adjacent screens.

Approved proof artifacts:

- `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_42/shadow_canvas_scn02_mainmenu_iter66_thin_action_chrome_1920x1080.png`
- `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_42/focused/iter66_header_reference_vs_canvas.png`
- `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_42/scn02_iter66_all_aspect_contact.png`

Reuse these choices unless a later screen has a specific approved exception:

- UI Toolkit-approved logo lockup.
- SCN-02 left navigation chrome, row rhythm, icon padding, chevron treatment, and selected state.
- Header resource-chip rhythm and plus-icon padding.
- Two-button Canvas header action rule for Main Menu: Inbox and Settings only.
- Thin sliced chrome from correct PPU/Image multiplier before anchor tweaks.
- Separate right-side live panels instead of one baked multi-section background.
- Full-frame selectable states for buttons and cards.

## SCN-02 Defect Checklist Used For Approval

This checklist is preserved as a pattern for future screens. Before a screen can be approval-ready, verify the equivalent panel families:

- Header actions: only Inbox and Settings are present; both have correct square size, spacing, sliced chrome, icon alignment, and hover/selected/pressed states.
- Left navigation: rows match the approved Target Lock nav language; no overlap with middle content; row height, icon size, label padding, chevron position, and active state are consistent.
- Mode cards: thumbnail art is contained inside card chrome; bottom title plates are not oversized; badge, title, divider, star, and selection states match the reference language.
- Commander area: right stack is split into separate live panels; portrait viewport is masked; identity/readiness/faction rows have readable content and consistent padding; no clipping or map bleed shows through chrome.
- Deploy CTA: button is wide enough, not tiny, uses sliced Target Lock chrome, has centered text/chevrons/star tab, and includes default/hover/selected/pressed/disabled states.
- Old sprites: no visible legacy main-menu art-direction sprites remain.
- Focused crops: header, left nav, mode cards, commander area, and deploy CTA each have a crop for the latest candidate.

## Status Handoff Template

Every status handoff for this work should include:

- Current screen and iteration number.
- Latest capture/contact sheet path.
- Progress snapshot from the tracker.
- Validation status.
- `Still wrong / next iteration` with concrete defects and planned fixes.

If no known visual defects remain, say that explicitly and state the next action is user approval.

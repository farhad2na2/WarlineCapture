# SCN19 Armory Clean Target-Lock Implementation Plan

Date: 2026-05-27
Status: In progress. This is the single step-by-step tracker for producing a clean SCN19 Armory shell that visually follows the active target mockup and uses correct GameUI hierarchy, anchors, and pivots.

## Rule

The Armory screen is complete only when the GameUI capture looks clean and target-like against the active SCN19 target mockup, and the prefab hierarchy is correctly parented under `MenuBackgroundContent`, `HeaderContent`, `LeftContent`, `MiddleContent`, `RightContent`, and `FooterContent`.

Do not mark a step complete just because code compiles, a prefab exists, or Unity validation passes. Visual quality is part of the requirement.

## Progress Tracker

Use this section as the only tracker for this work. Change `[PENDING]` to `[COMPLETE]` only when the described output exists and is verified.

- [x] `[COMPLETE]` Step 01 - Open the active target mockup and layer inventory only from `Design/VisualLockLayered/SCN-19_Armory/`.
- [x] `[COMPLETE]` Step 02 - Confirm source of truth: `reference/SCN-19_Armory_Landscape_Target.png`, `layers/`, `layer_manifest.json`, `target_lock_manifest.json`, and `README.md`.
- [x] `[COMPLETE]` Step 03 - Confirm implementation must not use old generated Unity scenes, archived legacy VisualLock folders, or flattened target reference PNGs.
- [x] `[COMPLETE]` Step 04 - Define the SCN19 layout contract for the 4800x2160 GameUI base: background, secondary header, left category rail, center roster grid, right inspection panel, and footer tabs/breadcrumb.
- [x] `[COMPLETE]` Step 05 - Implement `MenuBackgroundContent` from active no-UI background layer.
- [x] `[COMPLETE]` Step 06 - Implement `HeaderContent` with brand/resources/inbox/settings plus local back/title block and no far-right global back button.
- [x] `[COMPLETE]` Step 07 - Implement `LeftContent` with category rail and comms panel; every icon/text child must be local to the rail/button parent.
- [x] `[COMPLETE]` Step 08 - Implement `MiddleContent` with filter/sort controls and roster cards; every card owns its art, labels, status, progress, and badge.
- [x] `[COMPLETE]` Step 09 - Implement `RightContent` with selected item inspection, stats, abilities, upgrade track, source/unlock, and CTAs.
- [x] `[COMPLETE]` Step 10 - Implement `FooterContent` with bottom tabs and route breadcrumb strip.
- [x] `[COMPLETE]` Step 11 - Preserve required shell compatibility paths and route/button behavior from Commander Profile to Armory and back.
- [x] `[COMPLETE]` Step 12 - Build `SCN19_ArmoryContent.prefab` through Unity using only `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 13 - Capture GameUI Armory screenshots through the shadow sibling project at `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160`.
- [x] `[COMPLETE]` Step 14 - Compare the `2400x1080` capture against the active target mockup for composition, spacing, typography scale, visual hierarchy, and readability.
- [x] `[COMPLETE]` Step 15 - Verify responsive behavior at all captured aspect ratios: header stays stable, left/right panels stay anchored, roster stays readable, footer remains usable, and no text/panels overlap.
- [x] `[COMPLETE]` Step 16 - Verify hierarchy and transform rules mechanically: each panel owns its contents, centered children use centered anchors/pivots, and no target reference PNG or archived source path is used as implementation art.
- [x] `[COMPLETE]` Step 17 - Verify menu behavior: entering Armory replaces body content with SCN19 content and shows SCN19 footer route strip.
- [x] `[COMPLETE]` Step 18 - Save final screenshots and a short verification report under `Design/AgentReports/Captures/GameUI/Armory/`.
- [x] `[COMPLETE]` Step 19 - Mark this plan complete only if the final capture is clean and target-like; otherwise leave the relevant step pending with the exact reason.

## Source Of Truth

Use:

- `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-19_Armory/layers/`
- `Design/VisualLockLayered/SCN-19_Armory/layer_manifest.json`
- `Design/VisualLockLayered/SCN-19_Armory/target_lock_manifest.json`
- `Design/VisualLockLayered/SCN-19_Armory/README.md`
- `Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/`

Do not use:

- Old generated Unity target scenes
- Archived legacy VisualLock folders as source of truth
- The target reference PNG as implementation art
- `SCN01_LoadingContent.prefab`

## Visual Acceptance

- Header follows the SCN19 secondary-screen rule with local back/title on the left and no far-right back button.
- Left category rail stays anchored left.
- Roster grid stays centered and readable.
- Inspection panel stays anchored right.
- Footer tabs and breadcrumb are visible and locally owned by `FooterContent`.
- Live labels, counters, stats, progress values, buttons, and statuses remain TMP text.

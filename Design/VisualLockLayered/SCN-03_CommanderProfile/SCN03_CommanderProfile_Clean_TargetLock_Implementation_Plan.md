# SCN03 Commander Profile Clean Target-Lock Implementation Plan

Date: 2026-05-27
Status: In progress. This is the single step-by-step tracker for producing a clean SCN03 commander/profile shell that visually follows the active target mockup and uses correct GameUI hierarchy, anchors, and pivots.

## Rule

The commander/profile screen is complete only when the GameUI capture looks clean and target-like against the active SCN03 target mockup, and the prefab hierarchy is correctly parented under `MenuBackgroundContent`, `HeaderContent`, `LeftContent`, `MiddleContent`, `RightContent`, and `FooterContent`.

Do not mark a step complete just because code compiles, a prefab exists, or Unity validation passes. Visual quality is part of the requirement.

## Progress Tracker

Use this section as the only tracker for this work. Change `[PENDING]` to `[COMPLETE]` only when the described output exists and is verified.

- [x] `[COMPLETE]` Step 01 - Open the active target mockup and layer inventory only from `Design/VisualLockLayered/SCN-03_CommanderProfile/`.
- [x] `[COMPLETE]` Step 02 - Confirm source of truth: `reference/SCN-03_CommanderProfile_Landscape_Target.png`, `layers/`, `layer_manifest.json`, and `README.md`.
- [x] `[COMPLETE]` Step 03 - Confirm implementation must not use old generated Unity scenes, archived legacy VisualLock folders, or flattened target reference PNGs.
- [x] `[COMPLETE]` Step 04 - Define the SCN03 layout contract for the 4800x2160 GameUI base: background, header, left identity panel, middle overview/reward/history panels, right armory/rewards/account panels, and footer route strip.
- [x] `[COMPLETE]` Step 05 - Implement `MenuBackgroundContent` from active background layer so commander/profile keeps a target-like environment without flattening the full target.
- [x] `[COMPLETE]` Step 06 - Implement `HeaderContent` as the target-like profile header with logo, screen title, live resources, inbox, settings, and back button.
- [x] `[COMPLETE]` Step 07 - Implement `LeftContent` with commander identity, portrait, level/XP, edit ID, and badges buttons; every child must be local to the identity panel.
- [x] `[COMPLETE]` Step 08 - Implement `MiddleContent` with tabs, overview stat cards, reward track, claim button, and recent history rows; every card/row owns its icons and text.
- [x] `[COMPLETE]` Step 09 - Implement `RightContent` with armory/squads summary, Open Armory CTA, profile rewards, and account snapshot panels.
- [x] `[COMPLETE]` Step 10 - Implement `FooterContent` with route breadcrumb strip: Main Menu, Commander Profile, Armory.
- [x] `[COMPLETE]` Step 11 - Preserve required shell compatibility paths and route/button behavior without duplicate visible UI.
- [x] `[COMPLETE]` Step 12 - Build `SCN03_CommanderProfileContent.prefab` through Unity using only `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 13 - Capture GameUI commander/profile screenshots through the shadow sibling project at `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160`.
- [x] `[COMPLETE]` Step 14 - Compare the `2400x1080` capture against the active target mockup for composition, spacing, typography scale, visual hierarchy, and readability.
- [x] `[COMPLETE]` Step 15 - Verify responsive behavior at all captured aspect ratios: header stays stable, left/right panels stay anchored, middle content stays readable, footer remains usable, and no text/panels overlap.
- [x] `[COMPLETE]` Step 16 - Verify hierarchy and transform rules mechanically: each panel owns its contents, centered children use centered anchors/pivots, and no target reference PNG or archived source path is used as implementation art.
- [x] `[COMPLETE]` Step 17 - Verify menu behavior: entering Commander Profile keeps the main menu shell concept but replaces body content with SCN03 content and shows the SCN03 footer route strip.
- [x] `[COMPLETE]` Step 18 - Save final screenshots and a short verification report under `Design/AgentReports/Captures/GameUI/CommanderProfile/`.
- [x] `[COMPLETE]` Step 19 - Mark this plan complete only if the final capture is clean and target-like; otherwise leave the relevant step pending with the exact reason.

## Source Of Truth

Use:

- `Design/VisualLockLayered/SCN-03_CommanderProfile/reference/SCN-03_CommanderProfile_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-03_CommanderProfile/layers/`
- `Design/VisualLockLayered/SCN-03_CommanderProfile/layer_manifest.json`
- `Design/VisualLockLayered/SCN-03_CommanderProfile/README.md`
- `Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/`

Do not use:

- Old generated Unity target scenes
- Archived legacy VisualLock folders as source of truth
- The target reference PNG as implementation art
- `SCN01_LoadingContent.prefab`

## Visual Acceptance

- Header reads as the SCN03 commander profile header, not the SCN02 main menu header.
- Background supports the profile screen without baking UI into it.
- Commander identity panel stays anchored to the left.
- Overview, reward track, and history stay centered and readable.
- Armory, rewards, and account panels stay anchored to the right.
- Footer route strip is visible and locally owned by `FooterContent`.
- Live labels, counters, values, tabs, reward states, and history rows remain TMP text.

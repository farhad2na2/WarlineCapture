# SCN08 Match HUD Clean Target-Lock Implementation Plan

Date: 2026-05-27
Status: Complete for the clean shell implementation pass. This is the single step-by-step tracker for producing a clean SCN08 match HUD that visually follows the active target mockups and uses correct GameUI hierarchy, anchors, and pivots.

## Rule

The match HUD is complete only when the GameUI capture looks clean and target-like against the active SCN08 target mockups, and the prefab hierarchy is correctly parented under `HeaderContent`, `LeftContent`, `RightContent`, and `FooterContent`.

Do not mark a step complete just because code compiles, a prefab exists, or Unity validation passes. Visual quality is part of the requirement.

## Progress Tracker

Use this section as the only tracker for this work. Change `[PENDING]` to `[COMPLETE]` only when the described output exists and is verified.

- [x] `[COMPLETE]` Step 01 - Open the active target mockups and layer inventory only from `Design/VisualLockLayered/SCN-08_RTSBattleHUD/`.
- [x] `[COMPLETE]` Step 02 - Confirm source of truth: `reference/SCN-08_RTSBattleHUD_TargetLock_V01.png`, `reference/SCN-08_RTSBattleHUD_16x9_TargetLock_V01.png`, `layers/`, and `layer_manifest.json`.
- [x] `[COMPLETE]` Step 03 - Confirm implementation must not use old generated Unity scenes, archived legacy VisualLock folders, old teal/cyan HUD assets, or sliced target reference PNGs.
- [x] `[COMPLETE]` Step 04 - Write the SCN08 layout contract: header, left objective panel, selected squad panel, center battlefield markers, right quick rail, minimap, threat feed, squad tray, and command rail.
- [x] `[COMPLETE]` Step 05 - Define final prefab section roots exactly: `HeaderContent`, `LeftContent`, `RightContent`, and `FooterContent`; do not add SCN08 content to `SCN01_LoadingContent.prefab`.
- [x] `[COMPLETE]` Step 06 - Define local hierarchy and anchor rules for every HUD panel: frame owns fill/icons/text/buttons; children use local coordinates under their owning panel.
- [x] `[COMPLETE]` Step 07 - Implement `HeaderContent` as the target-like top command/resource/status strip with command banner, credits, fuel, supply, civilian risk, menu, pause, and settings.
- [x] `[COMPLETE]` Step 08 - Implement `LeftContent` with objective tracking and selected squad detail panel, using local child panels and target layer assets.
- [x] `[COMPLETE]` Step 09 - Implement `RightContent` with threat/jump feedback, vertical quick actions, and minimap with local markers/zoom/focus controls.
- [x] `[COMPLETE]` Step 10 - Implement `FooterContent` with squad tray cards and bottom command rail: Select, Move, Attack, Hold, Stop, Build, Scan, and Support.
- [x] `[COMPLETE]` Step 11 - Implement battlefield overlay markers as local HUD children where the shell allows it, without blocking the clear central gameplay view.
- [x] `[COMPLETE]` Step 12 - Preserve required shell compatibility paths and route/button behavior without duplicate visible UI.
- [x] `[COMPLETE]` Step 13 - Build `SCN08_MatchHudContent.prefab` through Unity using only `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 14 - Capture GameUI match HUD screenshots through the shadow sibling project at `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160`.
- [x] `[COMPLETE]` Step 15 - Compare the `1920x1080` capture against the active 16:9 target mockup for composition, spacing, typography scale, visual hierarchy, and readability.
- [x] `[COMPLETE]` Step 16 - Compare the `2400x1080` capture against the active 20:9 target mockup for composition, spacing, typography scale, visual hierarchy, and readability.
- [x] `[COMPLETE]` Step 17 - Verify responsive behavior at all captured aspect ratios: header stays stable, left/right panels stay anchored, footer remains usable, and no text/panels overlap.
- [x] `[COMPLETE]` Step 18 - Verify hierarchy and transform rules mechanically: each panel owns its contents, centered children use centered anchors/pivots, and no target reference PNG or archived source path is used as implementation art.
- [x] `[COMPLETE]` Step 19 - Verify menu behavior: entering match HUD hides the main-menu background/header and replaces it with match HUD content.
- [x] `[COMPLETE]` Step 20 - Save final screenshots and a short verification report under `Design/AgentReports/Captures/GameUI/MatchHud/`.
- [x] `[COMPLETE]` Step 21 - Mark this plan complete only if the final capture is clean and target-like; otherwise leave the relevant step pending with the exact reason.

## Source Of Truth

Use:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_TargetLock_V01.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_16x9_TargetLock_V01.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`

Do not use:

- Old generated Unity target scenes
- Archived legacy VisualLock folders as source of truth
- Old teal/cyan MatchHUD assets
- The target reference PNGs as sliced implementation art
- `SCN01_LoadingContent.prefab`

## Visual Acceptance

- HUD chrome frames gameplay instead of covering the battlefield.
- Header reads like one compact RTS command/status strip.
- Left objective and squad panels stay anchored to the left side.
- Right threat, quick actions, and minimap stay anchored to the right side.
- Footer squad tray and command rail remain readable at 16:9 and 20:9.
- Marker overlays support the target feeling without becoming a flat full-screen mockup.

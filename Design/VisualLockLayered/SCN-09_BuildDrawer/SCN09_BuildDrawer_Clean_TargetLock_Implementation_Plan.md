# SCN09 Build Drawer Clean Target-Lock Implementation Plan

Date: 2026-05-27
Status: In progress. This is the single step-by-step tracker for producing the SCN09 in-match Build drawer over the active SCN08 Match HUD.

## Rule

The Build drawer is complete only when the GameUI capture shows the SCN09 drawer cleanly over the Match HUD, using the active SCN09 target/layers and correct popup-layer hierarchy, anchors, and pivots.

Do not mark a step complete just because code compiles, a prefab exists, or Unity validation passes. Visual quality is part of the requirement.

## Progress Tracker

Use this section as the only tracker for this work. Change `[PENDING]` to `[COMPLETE]` only when the described output exists and is verified.

- [x] `[COMPLETE]` Step 01 - Confirm source of truth: `reference/SCN-09_BuildDrawer_OnExistingMatchHUD_TargetLock_V01.png`, `layers/`, `layer_manifest.json`, and `README.md`.
- [x] `[COMPLETE]` Step 02 - Confirm this is an in-match drawer over SCN08 Match HUD, not a full-screen menu replacement and not the old BuildPlacement popup assets.
- [x] `[COMPLETE]` Step 03 - Import the active SCN09 layer pack into `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/`.
- [x] `[COMPLETE]` Step 04 - Define the 4800x2160 popup-layer layout contract: full root, left production drawer, tab rail, card grid, detail panel, support panel, and bottom instruction strip.
- [x] `[COMPLETE]` Step 05 - Implement `SCN09_BuildDrawerPopup.prefab` using active layer sprites and live TMP text for labels, costs, timers, lock reasons, and warnings.
- [x] `[COMPLETE]` Step 06 - Keep every card, tab, icon, thumbnail, and button parented under its local drawer/detail/support parent with centered anchors/pivots for centered children.
- [x] `[COMPLETE]` Step 07 - Wire the shell presenter and GameUI scene builder so the Build drawer can be installed on `PopupLayer` while SCN08 Match HUD remains visible underneath.
- [x] `[COMPLETE]` Step 08 - Build `SCN09_BuildDrawerPopup.prefab` through Unity using only `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 09 - Capture GameUI Build drawer screenshots through the shadow sibling project at `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160`.
- [x] `[COMPLETE]` Step 10 - Compare the `2400x1080` capture against the active target for composition, spacing, visual hierarchy, readability, and drawer-over-HUD behavior.
- [x] `[COMPLETE]` Step 11 - Verify responsive behavior at all captured aspect ratios: drawer stays left anchored, detail panel remains readable, bottom instruction strip remains visible, and no critical text overlaps.
- [x] `[COMPLETE]` Step 12 - Verify hierarchy and source rules mechanically: no target reference PNG, no archived source path, no old BuildPlacement generated popup assets, and no flattened target mockup use.
- [x] `[COMPLETE]` Step 13 - Save final screenshots and a short verification report under `Design/AgentReports/Captures/GameUI/BuildDrawer/`.
- [x] `[COMPLETE]` Step 14 - Mark this plan complete only if the final capture is clean and target-like; otherwise leave the relevant step pending with the exact reason.

## Source Of Truth

Use:

- `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_OnExistingMatchHUD_TargetLock_V01.png`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/layers/`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/layer_manifest.json`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/README.md`

Do not use:

- Old generated Unity scenes
- Archived legacy VisualLock folders
- The target reference PNG as implementation art
- Old `Assets/Game/Art/UI/Generated/Popups/BuildPlacement_*` assets
- `SCN01_LoadingContent.prefab`

## Visual Acceptance

- Build drawer sits over the existing SCN08 Match HUD.
- Header/left/right/footer Match HUD remain visible behind the drawer unless occluded by the drawer itself.
- Drawer has Buildings, Vehicles, Soldiers tabs, with Buildings selected.
- Production cards contain live title, role, cost, time, status, and add/lock affordances.
- Detail panel exposes selected building info, placement requirement, cost breakdown, and build CTA.
- Bottom instruction strip explains placement flow with live text.

# POP05 Mission Result Clean Target-Lock Implementation Plan

Date: 2026-05-27
Status: In progress. This is the single step-by-step tracker for producing a clean POP05 mission result popup that visually follows the active target mockup and uses correct GameUI popup hierarchy, anchors, and pivots.

## Rule

The mission result popup is complete only when the GameUI capture looks clean and target-like against the active POP05 target mockup, and the prefab hierarchy is correctly parented under `PopupFrame` with local child panels for header, summary, rating/objectives, stats, rewards, consequences, and actions.

Do not mark a step complete just because code compiles, a prefab exists, or Unity validation passes. Visual quality is part of the requirement.

## Progress Tracker

Use this section as the only tracker for this work. Change `[PENDING]` to `[COMPLETE]` only when the described output exists and is verified.

- [x] `[COMPLETE]` Step 01 - Open the active target mockup and layer inventory only from `Design/VisualLockLayered/POP-05_MissionResult/`.
- [x] `[COMPLETE]` Step 02 - Confirm source of truth: `reference/POP-05_MissionResult_Landscape_Target.png`, `layers/`, `layer_manifest.json`, `target_lock_variants_manifest.json`, and `README.md`.
- [x] `[COMPLETE]` Step 03 - Confirm implementation must not use old generated Unity scenes, archived legacy VisualLock folders, or flattened target reference PNGs.
- [x] `[COMPLETE]` Step 04 - Define the POP05 layout contract for the 4800x2160 GameUI base: dimmed background, centered result frame, header, left mission summary, middle rating/objectives/stats, right rewards/consequences, and bottom actions.
- [x] `[COMPLETE]` Step 05 - Implement `PopupFrame` as a centered modal sized from the active 2400x1080 target proportions and scalable in the GameUI popup layer.
- [x] `[COMPLETE]` Step 06 - Implement live target-like header with logo, victory label, mission title, metadata strip, and commander XP progress.
- [x] `[COMPLETE]` Step 07 - Implement left mission summary panel with snapshot art, mission title, and description; all image/text children must be local to the summary panel.
- [x] `[COMPLETE]` Step 08 - Implement middle mission rating panel with three stars, objective labels, and objective checklist rows.
- [x] `[COMPLETE]` Step 09 - Implement middle performance stats panel with stat tiles and live values.
- [x] `[COMPLETE]` Step 10 - Implement right rewards and consequences panels with icons, labels, values, and positive/negative/stable value color.
- [x] `[COMPLETE]` Step 11 - Implement bottom action bar with Replay, route note, and Continue CTA, preserving the existing `ContinueButton` confirmation behavior.
- [x] `[COMPLETE]` Step 12 - Preserve required shell compatibility paths and route/button behavior without duplicate visible UI.
- [x] `[COMPLETE]` Step 13 - Build `POP05_MissionResultPopup.prefab` through Unity using only `D:\Projects\WarlineCapture-CodexUnity1`.
- [x] `[COMPLETE]` Step 14 - Capture GameUI mission result popup screenshots through the shadow sibling project at `1920x1080`, `2400x1080`, `3840x2160`, and `4800x2160`.
- [x] `[COMPLETE]` Step 15 - Compare the `2400x1080` capture against the active target mockup for composition, spacing, typography scale, visual hierarchy, and readability.
- [x] `[COMPLETE]` Step 16 - Verify responsive behavior at all captured aspect ratios: popup stays centered, modal remains readable, buttons remain usable, and no text/panels overlap.
- [x] `[COMPLETE]` Step 17 - Verify hierarchy and transform rules mechanically: each panel owns its contents, centered children use centered anchors/pivots, and no target reference PNG or archived source path is used as implementation art.
- [x] `[COMPLETE]` Step 18 - Save final screenshots and a short verification report under `Design/AgentReports/Captures/GameUI/MissionResult/`.
- [x] `[COMPLETE]` Step 19 - Mark this plan complete only if the final capture is clean and target-like; otherwise leave the relevant step pending with the exact reason.

## Source Of Truth

Use:

- `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png`
- `Design/VisualLockLayered/POP-05_MissionResult/layers/`
- `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json`
- `Design/VisualLockLayered/POP-05_MissionResult/target_lock_variants_manifest.json`
- `Design/VisualLockLayered/POP-05_MissionResult/README.md`
- `Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01/`

Do not use:

- Old generated Unity target scenes
- Archived legacy VisualLock folders as source of truth
- The target reference PNG as implementation art
- `SCN01_LoadingContent.prefab`

## Visual Acceptance

- Popup reads as the active victory POP05 result target.
- Header, mission summary, rating/objectives, stats, rewards, consequences, and action bar align as a unified centered debrief.
- Background is no-UI art/dimming, not a flattened target mockup.
- Live labels, values, objective rows, reward values, consequence deltas, and buttons remain TMP text.
- Continue button keeps the existing shell confirmation component.

# UI Canvas Armory Shadow Validation Pass

Date: 2026-06-23

Validation result: passed

## Scope

- Task: SCN-19 Armory Canvas Target Lock category, catalog portrait, and selected-detail imagery validation.
- Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Main repo tracker: `/Users/farhad/Projects/WarlineCapture/Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`
- Artifact folder: `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_16/`

## Result

The previous Unity licensing blocker resolved on retry. Shadow-project route captures passed for every Armory category at `1920x1080`:

- `Characters`: `luma=0.239`
- `Vehicles`: `luma=0.253`
- `Aircrafts`: `luma=0.272`
- `Buildings`: `luma=0.264`
- `Support`: `luma=0.301`

## Evidence

- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_16/scn19_iter16_armory_category_contact.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_16/focused/scn19_iter16_category_catalog_category_contact.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_16/focused/scn19_iter16_right_detail_category_contact.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_16/focused/scn19_iter16_footer_tabs_category_contact.png`

## Visual Findings

- Selected-detail imagery updates across all five captured categories.
- Catalog card portraits update for Characters, Vehicles, Aircrafts, and Buildings.
- Support has no visible catalog cards in the captured state, but the selected detail image still updates.

## Remaining Armory Work

- Footer tab switching (`Owned`, `Upgrade Tracks`, `Parts`, `Gear Modules`) still needs live visual validation without layout shifts.
- The right inspection panel still needs a density decision or another visual pass because it preserves runtime-bound source, ability, and progress sections that make it denser than the reference.

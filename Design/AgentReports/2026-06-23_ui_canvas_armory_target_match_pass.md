# UI Canvas Armory Target Match Pass

Date: 2026-06-23

Validation result: passed

## Scope

- Surface: SCN-19 Armory Canvas screen.
- Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Tracker: `/Users/farhad/Projects/WarlineCapture/Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`
- Final artifact folder: `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/`

## Result

SCN-19 Armory is counted target-matched for the current Canvas runtime behavior.

The screen keeps the approved SCN-02 shared header unchanged, reuses the shared left-nav chrome, uses Target Lock card/action/footer/right-panel treatment, preserves runtime-bound names and bindings, and passes shadow all-aspect route captures.

## Final All-Aspect Captures

- `1280x720`: `luma=0.242`
- `1920x1080`: `luma=0.239`
- `2400x1080`: `luma=0.240`
- `4800x2160`: `luma=0.244`

Evidence:

- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/scn19_iter18_all_aspect_contact.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/focused/scn19_iter18_left_nav.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/focused/scn19_iter18_catalog.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/focused/scn19_iter18_right_panel.png`
- `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/focused/scn19_iter18_footer.png`

## Decisions

- Right inspection panel density is accepted for this pass. It is denser than the reference because it preserves live runtime-bound source/unlock, capability, stat, progress, and action sections, but focused captures show separate panels, readable labels, safe border padding, and no sibling overlap.
- The Canvas footer strip is treated as a visual/data tab family. It is not backed by an active content-switching footer controller in the current prefab, so no new runtime behavior was added during this visual migration. The footer remains visually stable across category captures and all-aspect captures.

## Remaining Work

No known SCN-19 visual defects remain at the current Canvas runtime behavior level.

# UI Canvas Armory Shadow Validation Blocked

Date: 2026-06-23

Validation result: blocked, superseded by later pass

Superseded by:

`/Users/farhad/Projects/WarlineCapture/Design/AgentReports/2026-06-23_ui_canvas_armory_shadow_validation_pass.md`

Current note:

The validation blocker described below was real for iteration 15. A later retry on 2026-06-23 reached the shadow project successfully and produced the iteration 16 category captures.

## Scope

- Task: SCN-19 Armory Canvas Target Lock tab-state and selected-detail validation.
- Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Main repo tracker: `/Users/farhad/Projects/WarlineCapture/Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`
- Artifact folder: `/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/`

## Workaround Attempted

The documented Unity licensing workaround from `/Users/farhad/Projects/WarlineCapture/Design/Agent_Coordination_Workflow.md` was attempted.

The validation was first attempted in sandboxed batchmode, then retried after the licensing helper processes had exited. After that, the same required validation was rerun with Codex escalation/out-of-sandbox execution in the assigned shadow workspace. A graphics-enabled Editor/open path was also attempted because the user confirmed Unity can open locally and visual validation should use the shadow project.

## Exact Escalated Batchmode Command

```sh
env WARLINE_CANVAS_ROUTE=Armory WARLINE_CANVAS_ARMORY_CATEGORY=Characters WARLINE_CANVAS_SCREENSHOT_WIDTH=1920 WARLINE_CANVAS_SCREENSHOT_HEIGHT=1080 WARLINE_CANVAS_SCREENSHOT_PATH=/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/shadow_canvas_scn19_armory_category_characters_escalated_1920x1080.png WARLINE_CANVAS_ROUTE_CAPTURE_SETTLE_FRAMES=150 /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod CanvasMenuFallbackValidation.RunRouteCapture -logFile /Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/shadow_canvas_scn19_armory_category_characters_escalated_1920x1080.log
```

Log path:

`/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/shadow_canvas_scn19_armory_category_characters_escalated_1920x1080.log`

## Graphics-Enabled Editor/Open Attempt

```sh
open -n -W -a /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app --env WARLINE_CANVAS_ROUTE=Armory --env WARLINE_CANVAS_ARMORY_CATEGORY=Characters --env WARLINE_CANVAS_SCREENSHOT_WIDTH=1920 --env WARLINE_CANVAS_SCREENSHOT_HEIGHT=1080 --env WARLINE_CANVAS_SCREENSHOT_PATH=/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/shadow_canvas_scn19_armory_category_characters_open_1920x1080.png --env WARLINE_CANVAS_ROUTE_CAPTURE_SETTLE_FRAMES=150 --args -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod CanvasMenuFallbackValidation.RunRouteCapture -logFile /Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/shadow_canvas_scn19_armory_category_characters_open_1920x1080.log
```

Log path:

`/Users/farhad/Projects/WarlineCapture/Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/shadow_canvas_scn19_armory_category_characters_open_1920x1080.log`

## Licensing Symptom

The escalated batchmode run reached Unity project startup in the shadow workspace, but licensing repeatedly lost the `LicenseClient-farhad` channel before the execute method could complete:

- `Channel LicenseClient-farhad doesn't exist`
- `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad"`
- `The re-connection attempt was UN-successful`
- `Error: 'com.unity.editor.headless' was not found`

The graphics-enabled Editor/open attempt also reached shadow-project startup but stopped before route capture with:

- `Licensing is not yet initialized`

No `shadow_canvas_scn19_armory_category_characters_*.png` screenshot was produced by these attempts.

## Product-Code Status

- Editor-only Armory category capture plumbing was added through `WARLINE_CANVAS_ARMORY_CATEGORY`.
- Runtime Armory behavior was not changed.
- The SCN-19 prefab visual work remains at iteration 14 for the last successful capture until per-category validation can run.

## Next Action

When Unity licensing allows the shadow project to reach `CanvasMenuFallbackValidation.RunRouteCapture`, rerun the category captures for:

- `Characters`
- `Vehicles`
- `Aircrafts`
- `Buildings`
- `Support`

Then inspect focused crops for tab selection state, catalog portrait changes, and selected right-detail imagery before checking the Armory tab/image validation items in the tracker.

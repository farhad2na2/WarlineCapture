# M3 placement drag and reachable green footprint

## Reported experience

The first guided defense preview opened over occupied base scenery. Dragging was difficult because the camera also moved, and the player could not reach a confirmable green footprint.

## Corrections

- Removed recurring camera follow from placement previews. The old one-second pointer-idle rule repeatedly recentered the camera, including while the player paused a drag.
- A pending placement press owns the camera through release. It cancels pan and unfinished focus/zoom transitions before processing camera motion.
- A ground tap can relocate the preview; dragging does not require grabbing its small visible model. UI presses remain excluded.
- Evaluate the final pointer position before ending the drag, so release and confirmation use the location the player actually chose. Hover after release leaves that position unchanged.
- If the mission's central lot has no nearby legal origin, sample the authored build zone for a valid starting plot and focus there once. This fallback performs at most 169 validation calls and preserves the existing bounded local search. It does not scan the full map or relax road, occupancy, resource, or mission-zone validation.
- Keep camera placement handling in its own partial file and remove obsolete idle-pointer bookkeeping. The camera and input files shrink within their reviewed limits. Retire the now-unused D-134 placement-visual growth exception; no baseline ceilings are raised.

## Validation

The earlier tutorial acceptance probe directly staged a valid preview. This follow-up replaces that shortcut with Input System pointer events: press, drag, hold for 1.5 seconds, release, confirm, and verify the next tutorial lesson. It checks camera position/rotation throughout the gesture, requires the preview to reach the exact chosen destination at least eight grid cells away, and requires a valid, confirmable footprint afterward.

The first live run exposed that no valid plot was visible from the original crowded starting location. The bounded mission-zone fallback addresses that additional failure.

- Final combined entry point: `MissionTutorialNextActionValidation.RunPlacementAcceptance` through the approved macOS wrapper; **exit code 0**.
- Architecture: **139 passed, 0 failed**, nine fixtures, including source growth and responsibility guards.
- Focused tutorial, pointer, bounded-search, and construction transaction checks: **passed**. Existing M1–M4 guidance/integration suites also passed.
- Live M3: **passed**. Input System drag reached the exact destination at least eight grid cells away; the camera remained within 0.03 world units / degrees through the drag and 1.5-second hold. Release left a green, confirmable footprint. The real Confirm button placed the defense and advanced to squad movement.
- Final log: `/private/tmp/warline-m03-placement-acceptance-final.log`. Visually inspected the final Farsi placement capture at `/private/tmp/warline-m03-editor-probe/tutorial-build-confirm.png`.
- Validation used an isolated Editor project. The user's Editor was not terminated. No Android validation was performed, and no evidence images were stored in Design.

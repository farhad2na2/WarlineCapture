# M3 road gate placement

The Road Barrier was rejected by the general building road mask. The previous tutorial drag test searched for any legal open-ground plot, so it did not prove that a road barrier could be placed on a road. Nearby-wall alignment could also override an explicit rotation, and pointer positioning still used the unrotated footprint. The stricter convoy-lane test exposed a second blocker: the small gate inherited a 20×10-cell reservation, overlapping roadside obstacles even after road permission was corrected.

## Changes

- The gate prefab now reserves 8×2 cells, measured against its full closed arm and posts through Unity prefab APIs. The geometry fits inside this reservation; physical blockers are not ignored to make it fit.

- Existing Road Barrier/gate classification opts into road overlap. Ordinary buildings and walls retain their road rejection.
- Gates bypass the mixed road/static-obstacle prefix mask and check obstacles directly. Grid boundaries, mission build zones, static obstacles and runtime building occupancy still apply.
- Gates automatically align across nearby straight roads: 90 degrees across an east-west road, zero degrees across a north-south road. Ambiguous intersections leave orientation to the player.
- Explicit Rotate takes precedence over automatic road/wall alignment. The visual now uses the resolved orientation directly, matching placement validation and the committed footprint.
- Dragging and pointer hit testing use the rotated footprint, keeping the gate centered under the pointer.
- Road-orientation sampling is bounded and only repeated when the preview origin changes.

## Verification

Four focused tests cover the canonical footprint against closed gate geometry, road permission, cached road masks, obstacle/occupancy rejection, both road orientations, manual orientation and rotated pointer geometry. Existing gate door tests, six construction transaction tests and nineteen placement command tests also run.

The final architecture run passed all 139 checks with no failures. Helper extraction preserves the source-growth limits without changing their baselines.

The live Editor probe follows the M3 tutorial's Build → Road Barrier card → Place → real mouse drag → Confirm flow. It specifically requires the authored convoy lane at row 426, checks a green footprint and 90-degree yaw, verifies model centering, clicks Rotate in both directions, rejects an ordinary building on the same road plot, and requires tutorial progression after confirmation. The camera is checked for stability during drag, a 1.5-second hold, and release.

Logs and screenshots remain in `/private/tmp`, outside Design:

- `/private/tmp/warline-road-gate-acceptance.log`: all 139 architecture checks and 31 focused/existing regressions passed. The subsequent live phase exposed an initial-orientation error in the QA pointer driver, corrected before the final run.
- `/private/tmp/warline-road-gate-final-live.log`: final Editor tutorial run passed, wrapper exit 0. The driver presses at the actual initial footprint center before dragging to the rotated destination.
- `/private/tmp/warline-m03-editor-probe/tutorial-build-confirm.png`

Validation is Editor-only, as requested. This is a targeted road-placement fix, not a claim that every M3/M4 gameplay issue has been audited again.

Final live result: green gate on convoy lane row 426, visual center (946, 426), yaw 90°, manual rotation in both directions, ordinary building rejected on that road, stable camera throughout drag/hold/release, and successful confirmation advancing guidance to squad movement. The screenshot was visually checked. Restart M3 after recompilation to load the corrected gate prefab.

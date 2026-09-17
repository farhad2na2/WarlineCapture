# M3 gate preview / confirmation mismatch — 2026-09-16

Confirmation checked the visible preview's validity, then overwrote `OriginCell` with `CommittedOriginCell`. The latter is a raw pointer/initial-placement snapshot. Automatic road centering changes the displayed origin and rotation afterward. Consequently, a green road preview could be built at an older sidewalk cell that had not been validated.

The transaction now commits the displayed and validated origin, retaining its rotation. It synchronizes the pointer snapshot to that origin, rather than restoring the older position. This is shared placement code, so rotated or automatically aligned buildings receive the same correction.

Regression coverage compares the registered building cell and the road-plane position/rotation of both its root and model, plus the registered terrain foundation height against the visible preview, immediately after Confirm and after tutorial progression. The previous replay checked the preview and next tutorial step but did not compare the built object's pose.

The real default-preview replay reproduced visible origin `(882, 419)` versus stale pointer origin `(879, 426)`. The first comparison also detected registration applying the existing 0.45 m foundation height; the final check obtains that height independently from `BuildingSurfaceComponent` and still requires unchanged X/Z, rotation, and relative model pose.

Validation passed in the isolated Unity QA project using the checked macOS wrapper (exit 0 for both runs):

- Seven construction transaction tests, including stale-pointer versus snapped-preview regression, resource accounting, rejection and duplicate-transaction handling.
- Farsi M3 default preview → Confirm: registered origin remained `(882, 419)`, world X/Z `(883, 423)`, with matching rotation and model pose immediately and after tutorial progression.
- Farsi M3 drag → 1.5-second hold → release → rotate both ways → Confirm: camera remained stationary during dragging; registered origin remained `(874, 418)`, world X/Z `(875, 422)`, with matching rotation and model pose.
- Both replays advanced to selecting defenders. Screenshots of the built gates were inspected.

Pass markers are in `default-validation.txt` and `drag-validation.txt`. These are focused placement replays, not new full-mission completion runs.

# Vehicle fidelity and closer M3 cameras

The render-budget visual planner could replace an unselected enemy vehicle with a far impostor once its distance exceeded 64 units, even while it was moving. Changing the camera alone would only hide that defect at some distances.

Vehicles now retain the detailed model regardless of faction, distance, movement, selection, or availability of mid/low models. The existing render-state transition removes stale impostor tags immediately, hides alternative visual roots, and keeps the complete vehicle mesh. Character animation and distance policies remain separate.

M3's command camera is now 55 units above the focus, down from 85. Warning focus explicitly uses a 40-unit pose instead of retaining the current distant camera height. Returning from a warning restores the saved camera pose, and reduced-motion settings still control transition smoothing.

## Validation

- 108/108 Editor regression tests passed, exit 0: `/private/tmp/warline-vehicle-detail-tests.xml` and `.log`. Includes vehicle fidelity across distances/movement/factions, render-state and hierarchy handling, vehicle attachments, M3 warning/return-camera behavior, camera fallbacks, and 17 source-growth architecture checks.
- The live M3 vehicle probe observes the actual command view and naturally moving convoy vehicles. It checks detailed current/desired visual state, absence of impostor tags, active full-model mesh entities, movement, and unobstructed viewport position, then captures each vehicle using the closer contact pose.
- Live run passed, exit 0: all three moving convoy vehicles used detailed meshes with no impostor tag, and all focused subjects were clear of the HUD. `/private/tmp/warline-m3-vehicle-visual.log`; command-view, armored-car, and APC images were inspected under `/private/tmp/warline-m03-editor-probe`, outside Design and Git.

Validation is Editor-only. This change intentionally spends more rendering work on vehicles to preserve their complete silhouettes and moving parts; it is not a new large-army mobile performance certification.

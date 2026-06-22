# SCN-01 Splash Loading - Iteration 01

Status: Satisfied for current pass.

## Slice 01

Validated the existing TargetLockV04 loading screen against the saved reference.

Changed:

- Added the editor-only `Open SCN-01 Loading Static Preview` menu item to `UiToolkitTargetLockStaticPreview`.
- Synced the editor preview tool plus current SCN-01 UXML/USS to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Made no SCN-01 visual UXML/USS changes because the current UI already closely matches the target.

Visual audit:

- Background art, logo lockup, command-system chip, loading panel, progress bar, percentage, bottom status chip, and corner brackets are clean and readable.
- The static preview composition matches the target closely enough for the current pass.
- No runtime/Game View or PlayMode validation was used.

Validation:

- Visible shadow UI Builder preview opened from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Match Game View was enabled and Fit Viewport was clicked before capture.
- `git diff --check` passes.

Artifacts:

- Current-pass shadow UI Builder capture: `shadow_ui_builder_scn01_baseline_window.png`.

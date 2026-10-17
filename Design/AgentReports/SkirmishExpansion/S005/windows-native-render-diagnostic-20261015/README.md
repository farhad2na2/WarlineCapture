# Native packed-render failure diagnostic

Camera trial 01 failed before the opening camera observer acquired an attempt.
The native log records shaders settled, then
`incompletePackedRenderInitialization`, with actual checked exit code 2.
Launch and Finish fingerprints both match candidate
`9a58bfc0d2fa7a7d160ca50c90eb2a6dd9a058e2384df6b138dbc15e1d673f57`.
No PNG was produced; the observer raw stream contains only Started. The original
failed log and source snapshots remain under
`Logs/SkirmishProduction/CameraCalibration/20261015-01`.

The source gate at harness lines 445–458 requires exactly one packed render
database, readiness/state/metrics components, initialized state, first view
applied, positive rebuild count, positive active slots, zero state and metrics
overflow, matching enabled/active slot counts, and sufficient capacity. Stage 2
evaluates it after elapsed time, shell readiness and the native shader gate;
failure immediately aborts. A successful opening is latched only after the
whole predicate passes. No change to that behavior is proposed here.

The failed log does not contain these database values. Global units/models,
draw calls, FPS or the fact that some models render cannot establish which
predicate failed. No runtime cause has been asserted from those counters.
Initialization can await the unique valid native camera snapshot; assignment
publication and initial proxy apply are separate steps, each represented by
the state fields. The diagnostic must identify which actual step or overflow
condition is missing before an implementation fix is selected.

The proposed harness adds a read-only diagnostic immediately before its existing
failure. The new Editor partial records every matching database entity and
component-presence bit, packed readiness counts, virtualization state and
metrics, state-sync counts, published command version, command/canonical buffer
lengths, active map count, and actual runtime camera snapshots. It also records
the individual existing gate predicates. No component, buffer, camera, funding,
input or AI state is changed by the diagnostic.

The same failure branch requests a PNG through the existing native capture
path at `LiveTracePath()+".packed-render-failed-unqualified.png"`. It is explicitly
unqualified diagnostic evidence. The marker says captureRequested rather than
claiming a completed image or same-frame actor measurement. Root must check the
actual PNG after finish hashing keeps normal frames alive. Diagnostic and PNG
exceptions are recorded, then the original failure and immediate abort still
execute.

Exact proposed sources:

- Harness predecessor SHA256:
  `5da55c799105473a569df73170574e0d026b37ba8c94db3cb68c24304d1c6775`.
- Projected harness SHA256:
  `e0f96b44d4faebb2099a045c62d5d214b99944456819d6d62ad27120b5348376`.
- New `SkirmishS002AriaRunHarness.NativeRenderingDiagnostic.cs.txt` SHA256:
  `6ae11790b4765d7d1f8806bf3d666db31ea9a4d96137f85f11687296c6fb98ea`.

The generator guards the exact single failure seam; the projected full source
and minimal patch are retained here. No Assets source was edited. Root owns any
import after the failed wrapper and source-after capture release the lane.

Full current Editor type compilation passed with real references, no fallback,
and 18 analyzers/generators omitted. Evidence is preserved under
`validation/offline-type-8ua_bj4n`; compiler DLLs and response scratch now remain
under unique ignored `Logs/SkirmishProduction/Offline/S005PlaneCamera` paths.
This is type compatibility only; generators, ILPP, loaded domain and actual
native diagnostic execution remain pending. Independent authoring review found
no blocker in the bounded diagnostic delta and confirmed the gate stays intact.

Separately, `qualify_s005_plane_camera.py` now preserves a legitimate pre-attempt
native failure as structured Failed output instead of throwing for its absent
trace. A Started-only/raw, packed-failure log, wrapper-2 control passed together
with the existing controls. The actual trial was then qualified against root's
before/after snapshots and reviewed imported observer/collector pins:
`trial01-pre-attempt-qualification.json` reports Failed, qualified=false,
NativePreAttemptRenderingFailure, the exact reason and wrapper exit 2. It
does not invent an attempt or open missing contact/camera streams. All camera,
mission and player acceptance flags remain false.

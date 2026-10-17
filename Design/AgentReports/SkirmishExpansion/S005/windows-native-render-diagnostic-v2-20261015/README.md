Native evidence update: preparation05 passed the checked wrapper with exit 0, including DTO roundtrip, camera/profile checks and 42 objective rules. Normal-input trial03 captured the actual native selector, assignment result, per-bucket demand and camera matrices. It recorded a real OpaqueShadowsOn deficit of 136 (4725 required, 4589 capacity), with no selection/assignment failure. Full source and DLL manifests were unchanged and native launch/finish hashes matched. The mission failed its unchanged render gate; Editor shutdown then returned -1073741819. This is diagnostic evidence only, with no camera, gameplay or player acceptance. See ../windows-normal-camera-trials-20261015/trial03.json and trial03-rendering-diagnostic.json. The stale initial guard hypothesis still needs exact blob/camera replay.

# Trial 02 overflow diagnosis: explicit native values

Trial 02 establishes one database, initialized allocation, first view applied,
at least one successful rebuild, positive active slots, equal enabled/active
counts and sufficient total capacity. Both state and metrics overflow predicates
fail. It is neither a completed empty view nor missing initial application.
The exact cause is still unknown: the initial JSON serializer omitted the
component structs and runtime camera array because their types are not marked
Serializable. The booleans and primitive counts did survive. Preserve that
failed evidence and its diagnostic PNG; it does not establish camera acceptance.

The native publisher can report overflow for either a real pool-bucket deficit
or an internal selection/assignment validation failure. Total commands=7403 and
sufficient aggregate capacity do not prove every mesh/material policy bucket
can satisfy its actual selected demand. Conversely, overflow must not trigger
budget increases without knowing the native failure and actual bucket deficit.

The v2 successor replaces the same imported Editor diagnostic partial. The
harness's strict readiness predicate, screenshot request and immediate abort
branch are unchanged. Do not import both diagnostic versions.

Final source:
`Tools/CI/SkirmishProductionDrafts/SkirmishS002AriaRunHarness.NativeRenderingDiagnostic.ExplicitValidated.cs.txt`
SHA256 `cde4219387b9505236a6994e346cd40f1a18e0fe32fa3269d134f4d7febb809c`.
The generator derives it from the exact immutable v1 partial. The earlier
Explicit checkpoint is preserved separately and is superseded by this source.

All readiness/state/metrics/sync fields and camera snapshot fields are copied
to explicit Serializable scalar DTO records: path, original scalar type and
invariant value. Nested camera matrix and position values are included. No raw
component struct is left for JsonUtility to silently omit.

For the exact active database, the diagnostic completes already tracked jobs
and reads a boxed copy of the existing unmanaged initialization system. It
verifies world and database ownership, then copies the existing private native
selection failure enum, assignment result fields, selected cell/placement/row
counts and actual selected row demand per pool bucket. Bucket capacities,
policy, peak/headroom metadata and measured deficits come from the live blob.
This code never calls the rendering system's update/schedule, writes native
state or disposes native allocations. Read errors remain captured=false with
the exact exception. A failed reflection read must not become a zero-deficit
or healthy result.

Interpret the native selector result before proposing a correction:

- Selection failure other than None: inspect its exact invalid/capacity enum;
  do not call it bucket exhaustion.
- Assignment failure other than None: inspect actual invalid database,
  selection, binding or capacity contract.
- Both failure enums None with positive assignment OverflowCount: compare
  actual per-bucket selectedRequiredRows against that bucket's capacity. This
  establishes a demand deficit in that pool, not permission to change budgets.
- No captured selector, disagreement with published state, or stale command
  versions: preserve the inconsistency and diagnose it before claiming a cause.

Public focused entry:
`Game.Editor.SkirmishS002AriaRunHarness.RunNativeRenderingDiagnosticSerializationValidation`.
It creates synthetic diagnostic DTOs containing overflow17, capacity7403,
camera position/matrix values and a bucket deficit, then requires exact native
JsonUtility roundtrip persistence. Its marker labels syntheticDtoRoundtrip=1
and nativeGameplayCertified=0. Root should run it through the checked focused
preparation before another long trial; it is not gameplay qualification.

Exact full Editor type compilation passed with all current source/references,
no fallback and 18 analyzers/generators omitted. Evidence is preserved in
`validation/offline-type-k1ry76k5`; compiler scratch is under unique ignored
Logs/SkirmishProduction/Offline paths. Loaded/native serialization and selector
read compatibility remain native gates. No Assets source was edited by this
lane. Root owns import and the next checked native run.

Independent authoring review found no blocker in this bounded diagnostic.
The native references retain the last actual selection/assignment result until
another selection is scheduled or the map/database is torn down. They are not
cleared by an ordinary idle frame. RuntimeCameraReferenceSystem publishes a new
camera version each frame, but a guard-contained view with unchanged canonical
state skips selection and leaves these results intact. Diagnostics run before
Finish teardown.

Published state/metrics OverflowCount is not merely a historical maximum: a
successful publication overwrites it with the current assignment overflow; a
validation-failure publication forces it to at least one. HighestDeficit is the
historical maximum retained until initialization. A failure also leaves the
prior successful rebuild/envelope/command values in place. Therefore a positive
RebuildCount and command version do not by themselves prove the most recently
attempted selection succeeded. Read the exact retained failure/result enums.

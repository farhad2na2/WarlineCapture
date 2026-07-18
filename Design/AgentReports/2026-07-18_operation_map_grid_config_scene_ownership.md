# Operation Map Grid Config Scene Ownership

Date: 2026-07-18
Status: Passed

## Change

`OperationMapSceneView` now owns the current map's `GridAuthoringConfig` reference. `MatchSceneView` resolves that reference from the loaded operation map and retains its serialized field only as the compatibility fallback before thin-shell cutover.

This preserves the authored blocked-cell array when `runtimeGridConfig` is removed from `Match.unity`; no grid data is copied or allocated per frame.

## Validation

- Unity compile and scene staging passed.
- `OperationMapCurrentCompatibilitySceneStagerTests`: `11 / 11` passed.
- `ScriptArchitectureAlignmentContractTests`: `49 / 58` passed; the nine failures are pre-existing repository findings and do not reference any changed path. Applicable naming and structure checks passed.
- `git diff --check` passed.

Logs:

- `/private/tmp/opmap-grid-scene-view-stage.log`
- `/private/tmp/opmap-grid-scene-view-tests.xml`
- `/private/tmp/opmap-grid-architecture-tests.xml`

## Next Gate

The atomic thin-shell cutover may clear `runtimeGridConfig` together with the other classified map-owned references only after staged presentation publication and final parity validation.

# Operation Map Camera Pose Binding

Date: 2026-07-16

## Scope

Added `OperationMapCameraPoseCameraHelper` as a narrow Unity-camera boundary.
It resolves exactly one active operation-map metadata blob, looks up planning
or battle camera records by their bounded typed ids, validates finite pose and
projection data, and applies the selected transform/projection directly to the
world camera.

Managed gameplay startup now applies the active map's planning pose as its
initial camera pose before grid/surface startup projection. Missing or invalid
active-map metadata is a no-op, preserving the current `Match.unity` camera
behavior. Planning and battle pose application use the same bounded lookup
path for later camera-mode transitions.

The helper has no update loop, scene search, asset lookup, manager/controller,
or per-frame conversion. Its ECS query runs only on an explicit camera-pose
application call.

## Validation

- Focused camera-pose and metadata-bootstrap EditMode tests: `8 / 8` passed.
  - Results: `/private/tmp/opmap-camera-pose-tests.xml`
  - Log: `/private/tmp/opmap-camera-pose-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-camera-pose-architecture.xml`
  - Log: `/private/tmp/opmap-camera-pose-architecture.log`
- Missing-active-map compatibility fallback leaves the camera unchanged.
- Planning, initial, and battle pose transforms/projections are covered.
- Unity compilation completed with zero compiler errors.

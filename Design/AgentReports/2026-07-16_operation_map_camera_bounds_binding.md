# Operation Map Camera Bounds Binding

Date: 2026-07-16

## Scope

Bound the existing RTS camera request bridge to the single active operation
map's immutable `OperationMapBoundsComponent.CameraMin/CameraMax` extent.
`GridConfig` remains the compatibility fallback when no valid active-map
bounds entity exists.

The binding preserves the existing shell-owned camera, viewport-footprint
clamp, tactical-follow policy, and request queue. It adds no loader, scene
policy, recurring system, managed cache, or map asset mutation.

## Behavior

- Exactly one entity with `ActiveOperationMapComponent` and
  `OperationMapBoundsComponent` takes priority over `GridConfig`.
- Non-finite or non-positive active-map X/Z extents fail closed to the existing
  grid fallback.
- The existing camera-footprint clamp keeps the full viewport inside the
  selected extent, including initial camera correction and later pan/zoom.

## Validation

- Focused RTS camera EditMode tests: `33 / 33` passed.
  - Results: `/private/tmp/opmap-camera-bounds-tests.xml`
  - Log: `/private/tmp/opmap-camera-bounds-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-camera-bounds-architecture.xml`
  - Log: `/private/tmp/opmap-camera-bounds-architecture.log`
- Unity compilation: zero compiler errors.
- `git diff --check`: passed.
- Reviewed source-growth decision: `D-164`, exact ceiling `578` lines and
  `26,940` bytes for `RtsCameraRequestSystem.cs`.

PlayMode/device visual parity remains part of the later complete current-map
compatibility launch gate; this slice changes only the boundary source.

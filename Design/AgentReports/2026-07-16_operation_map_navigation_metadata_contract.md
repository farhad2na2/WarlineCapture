# Operation Map Navigation Metadata Contract

Date: 2026-07-16
Status: Accepted load-strategy-neutral navigation metadata

## Implemented

- Added bounded `OperationMapNavigationMetadataConfig` data to the existing
  definition and immutable metadata blob.
- Bound current authored navigation identity to MatchSubScene GUID
  `8d5e3c3f2ef84b61a4d61472c40c9a11` and exact `GridAuthoring` local id
  `146043441`.
- Measured direct `StaticGridBlockerAuthoring` components in MatchSubScene as
  `0`. No static blocker records were invented or copied.
- Recorded that path movement uses the existing map-surface payload and that
  dynamic blocker and dynamic occupancy systems remain supported runtime
  authorities.

## Architecture

- The config/blob stores only one GUID, one local id, one count, and three
  capability bytes.
- It does not duplicate blocked-cell arrays, occupancy bitsets, surface samples,
  or runtime native containers.
- No loader, scene search, Addressables handle, manager, controller, recurring
  update, or managed hot-path conversion was added.

## Validation

- Focused operation-map EditMode tests: `46 / 46` passed,
  `/private/tmp/opmap-navigation-tests.xml`.
- Source-growth and non-ECS naming gates: `24 / 24` passed,
  `/private/tmp/opmap-navigation-architecture.xml`.
- Two Unity generations produced byte-identical definition SHA-256
  `a54e293733537cb8fc7ebedc4b4dab8656eee21f64a1ff0b9225a080bcda974c`.
- Unity compilation: zero compiler errors.
- `git diff --check`: passed.
- No scene, subscene, surface, grid, manifest, static-presentation chunk,
  placement config, prefab, or build-setting file changed.

This closes the loader-neutral Phase 1 bounds/grid/surface/blocker/path metadata
reference contract. Runtime movement/placement binding remains a separate Phase
6 task.

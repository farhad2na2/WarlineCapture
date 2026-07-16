# Operation Map Grid Domain Binding

Date: 2026-07-16
Status: Stable shared-foundation slice
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Result

- Match startup now resolves grid dimensions, origin, and cell size from the single active operation-map metadata blob.
- The serialized `MatchSceneView` grid config remains a compatibility fallback only when no active operation-map root exists.
- Duplicate roots, missing identity/metadata, stale generations, mismatched map ids, and invalid grid values fail startup instead of silently using the compatibility grid.
- The existing one-shot runtime-grid bootstrap remains the sole owner that creates the `GridConfig`, path buffers, dynamic blocker storage, and dynamic occupancy storage consumed by movement and building placement.
- No loader, scene mutation, recurring update loop, map payload duplication, or managed hot-path conversion was added.

This is a partial implementation of Phase 6 blocker/path/build binding. The checklist row remains open until authored blocked-cell parity, selected-subscene identity, and map teardown/replacement behavior are accepted.

## Validation

- `OperationMapMetadataUtilityTests`: `10 / 10` passed. Log/result: `/private/tmp/opmap-grid-binding-tests-final.log`, `/private/tmp/opmap-grid-binding-tests-final.xml`.
- `RuntimeGridDeduplicationSystemTests`: `4 / 4` passed. Log/result: `/private/tmp/opmap-grid-bootstrap-tests.log`, `/private/tmp/opmap-grid-bootstrap-tests.xml`.
- Movement/blocker regressions: `26 / 26` passed. Log/result: `/private/tmp/opmap-grid-movement-tests.log`, `/private/tmp/opmap-grid-movement-tests.xml`.
- Building-placement regressions: `23 / 23` passed. Log/result: `/private/tmp/opmap-grid-placement-tests.log`, `/private/tmp/opmap-grid-placement-tests.xml`.
- Menu -> Match -> Menu PlayMode lifecycle: `1 / 1` passed. Log/result: `/private/tmp/opmap-grid-lifecycle-playmode.log`, `/private/tmp/opmap-grid-lifecycle-playmode.xml`.
- Non-ECS naming and source-growth architecture gates: `24 / 24` passed. Broad architecture result: `74 / 82`; all eight failures are pre-existing unrelated repository debt and none names a changed file. Log/result: `/private/tmp/opmap-grid-architecture-tests.log`, `/private/tmp/opmap-grid-architecture-tests.xml`.
- `Game.Runtime`, `Game.Composition`, and `Game.Tests.Editor` compiled with zero errors.
- Scoped `git diff --check`: passed. The unrelated modified M01 runtime-generation prototype scene remains outside this slice.

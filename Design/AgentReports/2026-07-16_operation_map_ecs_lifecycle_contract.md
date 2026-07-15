# Operation-Map ECS Lifecycle Contract

Date: 2026-07-16

## Implemented

- Added pure ECS root, queue, load-state, active-map, bounds, metadata, and readiness components.
- Added bounded request/result buffer elements carrying operation-map, scenario, and mission ids.
- Added typed request, status, result, and readiness enums covering validation, stale content, interruption, preload, unload, and teardown outcomes.
- Added the small immutable `OperationMapBlob` with camera, minimap, and anchor records.
- Kept `MapSurfaceBlob`, blocker/occupancy grids, meshes, textures, renderer entries, and scene/loading handles outside the operation-map blob.

## Architecture

- All contract types are unmanaged and live in `Game.Components`.
- Runtime ids use `FixedString64Bytes`; SHA-256 text and diagnostics use `FixedString128Bytes`.
- Bounds and transforms use `Unity.Mathematics` types.
- Readiness is generation-scoped and uses explicit ready/required/failed masks, so optional subscene and presentation requirements remain selected-content policy.
- No `ISystem`, job, loader, scene API, Addressables handle, managed collection, Unity object, update callback, manager, controller, facade, or service locator was introduced.

## Validation

- Unity EditMode `OperationMapEcsContractTests`: passed `7/7` (`/private/tmp/opmap-ecs-contract-focused.xml`).
- `Game.Components` and `Game.Tests.Editor` compilation: passed with `0` errors (`/private/tmp/opmap-ecs-*-build.log`).
- Unity production source-growth guardrail: passed `15/15` (`/private/tmp/opmap-ecs-growth.xml`).
- `git diff --check`: passed.

## Deferred

Composition ownership, blob construction/disposal, concrete content loading, runtime publication, match-start gating, presentation binding, and teardown behavior remain separate tracker items.

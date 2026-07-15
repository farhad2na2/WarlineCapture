# Operation-Map Spatial Metadata Configs

Date: 2026-07-16
Scope: load-strategy-neutral Phase 1 metadata only

## Implemented

- Added `OperationMapBoundsConfig` with finite, positive-planar-extent, ordered-vertical-range, and world-containment validation for world, playable, and camera bounds.
- Added `OperationMapCameraConfig` with stable scoped identity, transform, projection, and clamp-policy metadata.
- Added `OperationMapMinimapConfig` with stable scoped identity and projection metadata. It contains no raster or heavyweight asset reference.
- Added `OperationMapAnchorConfig` and the closed `OperationMapAnchorKind : byte` taxonomy for spawn, objective, deployment, build, civilian, hostile, base, resource, runway, helipad, lane, camera, minimap, and debug anchors.
- Extended `OperationMapDefinition` with small bounds/camera/minimap/anchor records plus planning and battle camera ids.
- Added deterministic ordinal duplicate detection and reference validation for camera and anchor ids.

## Architecture

- Config records are serializable value types in `Game.Configs`.
- The anchor taxonomy is a byte enum in `Game.Components`.
- No scene path, hierarchy lookup, `UnityEngine.Object`, Addressables reference, rendering dependency, runtime loader, ECS system, update callback, or per-frame policy was introduced.
- The metadata is suitable for one-time conversion into later Burst-readable ECS state; no heavy map payload is duplicated here.

## Validation

- Unity EditMode `OperationMapSpatialConfigTests`: passed `17/17` (`/private/tmp/opmap-spatial-focused-final.xml`).
- Unity EditMode `OperationMapIdentityConfigTests`: passed `23/23` (`/private/tmp/opmap-spatial-identity.xml`).
- Unity EditMode non-ECS naming, non-UI SystemBase migration, and production source-growth gates: passed `43/43` (`/private/tmp/opmap-spatial-gates.xml`); final source-growth rerun passed `15/15` (`/private/tmp/opmap-spatial-growth-final.xml`).
- `Game.Components`, `Game.Configs`, and `Game.Tests.Editor` project compilation: passed with `0` errors (logs under `/private/tmp/opmap-spatial-*-build.log`).
- `git diff --check`: passed.

## Deferred

- Surface/grid/blocker/path references, generated metadata hashes, map catalogs, runtime ECS publication, scene loading, Addressables, raster assets, and physical map rollout remain separate tracker items.

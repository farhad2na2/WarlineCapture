# Operation-Map Metadata Utility

Date: 2026-07-16

## Implemented

- Added the planned `Game.Runtime.OperationMapMetadataUtility` pure static boundary.
- Added exact linear lookup for bounded anchor and camera records in the immutable operation-map blob.
- Added inclusive world/playable/camera bounds queries and clamps using `Unity.Mathematics`.
- Added lower-left X/Z minimap projection compatible with the current zero-rotation contract, plus deterministic oriented projection and clamped inverse conversion.
- Invalid or non-finite projection metadata fails closed.

## Architecture And Performance

- The utility is Burst-compatible pure data/math with no hidden state.
- It uses no Unity objects, managed collections, hierarchy lookup, logging, scene API, loader, ECS system, update callback, cache owner, or asset reference.
- Linear blob scans are intentional for small immutable camera/anchor sets; a persistent native index is not justified without profiler evidence.
- The operation-map blob remains separate from the existing large `MapSurfaceBlob`.

## Validation

- Unity EditMode `OperationMapMetadataUtilityTests`: passed `6/6`, including zero managed allocations after warmup (`/private/tmp/opmap-metadata-utility-focused.xml`).
- `Game.Runtime` and `Game.Tests.Editor` compilation: passed with `0` errors (`/private/tmp/opmap-metadata-*-build.log`).
- Unity production source-growth guardrail: passed `15/15` (`/private/tmp/opmap-metadata-growth.xml`).
- `git diff --check`: passed.

## Tracker Accounting

This is enabling foundation only. It does not claim that camera, minimap, objective, ARIA, spawn, movement, surface, runway, or helipad consumers are bound to active-map metadata, so no Phase 6 behavior checkbox is closed.

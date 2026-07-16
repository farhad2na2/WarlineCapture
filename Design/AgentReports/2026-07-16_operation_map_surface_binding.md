# Operation-Map Surface Binding

Date: 2026-07-16
Status: Passed

## Scope

Bind the existing authoritative `MapSurfaceBlob` to the active operation-map metadata without adding a loader, duplicating surface cells, or changing the canonical Match scene.

## Implementation

- `OperationMapMetadataUtility` resolves generation-matched active surface and grid metadata and fails closed for ambiguous, stale, mismatched, or invalid map roots.
- `MapSurfaceRuntimeBootstrapSceneSystemHelper` validates the serialized surface payload hash, count, encoding, version, origin, dimensions, and cell size before creating or replacing a runtime surface.
- The created runtime blob is checked again before publication. A failed binding preserves the previously published surface.
- Match startup reports a surface-binding failure through the existing startup failure boundary. No-active-map startup retains the compatibility path.
- The existing `MapSurfaceComponent`/`MapSurfaceBlob` remains the only large runtime surface dataset consumed by grounding, movement, pathing, and aircraft clearance.
- Existing scene-overlay publication was separated into `MapSurfaceSceneOverlayPresentation`, keeping the runtime surface owner below its previous source-growth ceiling without an exception.

## Validation

- Surface/metadata/grid focused EditMode: `21/21` passed.
- Surface consumers and startup EditMode: `55/55` passed, including character grounding, layered surfaces, vehicle/path movement, and fixed-wing terrain clearance.
- Phase 0 ownership suites: `95/95` passed; navigation and camera/minimap reports were byte-identical across repeated runs.
- Naming guard: `9/9` passed. Source-growth guard: `16/17` passed; the sole failure names two unrelated narrative helpers already present on latest `main`, and no changed surface path is reported.
- `Game.Runtime` and `Game.Composition` compile: zero errors.
- `git diff --check`: passed.

Logs: `/private/tmp/opmap-surface-focused.log`, `/private/tmp/opmap-surface-consumers.log`.

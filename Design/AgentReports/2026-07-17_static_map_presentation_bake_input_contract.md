# Static Map Presentation Bake Input Contract

Date: 2026-07-17

## Scope

Introduced the loader-neutral immutable editor contract required before the static presentation baker can support a staged operation-map scene. This slice does not alter the current baker, manifest, integrity ledger, chunk scenes, runtime loading, or Addressables.

## Contract

`Game.Editor.StaticMapPresentationBakeInput` carries:

- operation-map id;
- source scene asset path;
- source map-root hierarchy path;
- owned output root and derived scene-output folder;
- manifest and integrity-ledger paths owned by that root;
- finite positive chunk size.

Validation rejects missing or oversized ids, external or malformed asset paths, malformed hierarchy paths, outputs outside the owned root, incorrect file extensions, and invalid chunk sizes. The type is a 103-line immutable editor-only value and introduces no update loop or runtime allocation path.

## Validation

- Focused Unity validation: passed 7/7; `/private/tmp/opmap-bake-input-focused.log`.
- Non-ECS naming/architecture: passed 9/9; `/private/tmp/opmap-bake-input-naming.log`.
- Unity compilation: passed with no C# errors.
- `git diff --check`: passed.
- No scene, config, manifest, integrity ledger, chunk, package, project setting, or runtime source changed.

The broad source-growth gate remains blocked by the two unrelated upstream `f993c3084` baseline violations already recorded in `2026-07-17_operation_map_placement_ownership_refresh.md`; both new files are below 500 lines.

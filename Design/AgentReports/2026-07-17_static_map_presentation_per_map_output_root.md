# Static Map Presentation Per-Map Output Root

Date: 2026-07-17
Result: Passed

## Scope

The existing static-presentation output for `opmap.skirmish.desert_base_01` now lives under the deterministic map-owned root:

`Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01`

`StaticMapPresentationOutputPathContract` derives this path from the validated operation-map id. `StaticMapPresentationBakeInput` rejects mismatched output roots, so a bake cannot silently publish one map into another map's directory.

## Migration

- Moved the manifest, integrity ledger, 514 chunk scenes, and their `.meta` files together.
- Preserved every existing scene and asset GUID.
- Compared all 1,028 moved chunk scene/meta files with their pre-move Git blobs: `1,028` checked, `0` mismatches.
- Rebuilt only path-bearing manifest/integrity metadata through `StaticMapPresentationBaker.MigrateCurrentOutputRoot`.
- Removed physical scene paths from chunk content identity so relocating an unchanged chunk does not rewrite scene bytes.

## Validation

- Structured migration: passed; `514` chunks, content hash `ec51367ab8853815a6c44d99f4fc3d6d`.
- Canonical bake at the new root: passed; `16,542` sources, `514` chunks, `0` scene writes, `0` stale deletes.
- Bake-input/path contract tests: `11 / 11` passed.
- Phase 0 baseline probe tests: `8 / 8` passed with zero compiler errors.
- Camera/minimap ownership probe tests after deterministic evidence refresh: `33 / 33` passed.
- Broad affected suite: `153 / 156` initially passed; the three exposed validation-contract regressions were corrected and their complete focused classes then passed `76 / 76`.
- Android resolver tests in the affected suite: `25 / 25` passed.
- Source-growth and non-ECS naming architecture gates: `26 / 26` passed.
- Final no-op bake repeated the accepted `0` scene writes and `0` stale deletes, followed by `1,028 / 1,028` byte-identical moved chunk scene/meta comparisons.
- `git diff --check` passed and Unity reported zero compiler errors in the final runs.

No source map scene, authored placement, runtime loading behavior, Addressables configuration, or second physical map was changed in this slice.

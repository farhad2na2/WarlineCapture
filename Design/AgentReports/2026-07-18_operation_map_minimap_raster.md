# Operation Map Minimap Raster

## Scope

The single approved physical map now owns a deterministic 512x512 minimap base raster at:

`Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/MinimapRaster.png`

`OperationMapMinimapRasterBaker` decodes the accepted `MapSurfaceDataAsset` blob without creating an ECS world. It samples every source cell covered by each output pixel and applies deterministic priority for blocked surfaces, bridges, ramps, highways, roads, dirt roads, and plazas over the existing minimap background language.

The current authored subscene does not contain a canonical serialized road-cell dataset. Runtime road ECS data therefore remains a dynamic overlay concern; the baker does not invent or snapshot transient runtime state.

## Ownership And Packaging

- Stable address: `operation-map/opmap.skirmish.desert_base_01/minimap-raster`.
- Group: the map's Local Core group.
- Role label: `operation-map-role-minimap-raster`.
- The definition stores the raster GUID through its lazy `AssetReference`.
- Importer metadata records algorithm version, map id, generated metadata hash, and map-surface runtime blob hash.
- Android imports at 512px ASTC 6x6 with no mipmaps and no CPU-readable copy.
- Strict layout validation rejects a missing/stale texture, wrong dimensions, mismatched definition GUID, or stale source identity.

## Validation

- Visual inspection confirmed the raster contains the current map's road/surface structures and blocked regions.
- PNG: 144,635 bytes; SHA-256 `40f75b96737d9f65a0edf4ac20d94618a03faa78c2700dee09396036449e72e3`.
- Two identical bakes preserved the same bytes and file timestamp; final log reported `wrote=0 importerChanged=0`.
- `OperationMapMinimapRasterBakerTests`: 2 passed, 0 failed.
- `OperationMapAddressablesLayoutBuilderTests`: 2 passed, 0 failed after the dedicated role expectation was added.
- `OperationMapAddressablesLayoutValidatorTests`: 2 passed, 0 failed in strict mode.
- `NonEcsSystemConversionArchitectureTests`: 9 passed, 0 failed.
- Compiler errors in focused Unity runs: 0.
- `git diff --check`: passed.

Logs are under `/private/tmp/opmap-minimap-*` and `/private/tmp/OperationMap*`.

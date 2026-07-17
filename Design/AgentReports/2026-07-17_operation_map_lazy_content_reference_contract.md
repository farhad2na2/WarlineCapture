# Operation Map Lazy Content Reference Contract

## Scope

`Game.Configs.OperationMapDefinition` now owns untyped, lazy Addressables handles for the selected map's source scene, optional heavyweight metadata, static presentation manifest, map surface data, cached minimap raster, and compatibility building/vehicle placement configs.

The handles remain ordinary `AssetReference` values so `Game.Configs` does not depend on concrete rendering/editor asset types. Existing hot metadata and `OperationMapBlob` creation are unchanged.

## Validation Boundary

`TryValidateLocalContentReferences` is an explicit packaging validation boundary. It requires every selected local-package role except optional heavyweight metadata and rejects an invalid optional handle when one is present. It is intentionally separate from `TryValidateMetadata`, so adding the schema does not change current catalog/runtime behavior before real package assets are assigned.

Address assignment, concrete type checks, stable-address checks, and the real map-owned minimap raster remain Phase 2A work.

## Evidence

- `dotnet build Game.Configs.csproj --no-restore`: passed, 0 errors.
- Unity 6000.5.2f1 EditMode `OperationMapLazyContentReferenceTests`: 8 passed, 0 failed.
- Unity 6000.5.2f1 EditMode `NonEcsSystemConversionArchitectureTests`: 9 passed, 0 failed.
- Unity compiler errors in the final focused run: 0.
- `git diff --check`: passed.
- Detailed logs: `/private/tmp/opmap-lazy-refs-unity.log`, `/private/tmp/opmap-lazy-refs-results.xml`.

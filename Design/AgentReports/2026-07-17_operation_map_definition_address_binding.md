# Operation Map Definition Address Binding

## Scope

The existing one-map Addressables layout builder now writes the configured source scene, map surface, static presentation manifest, and compatibility building/vehicle placement GUIDs into the lazy references on `OperationMap_Compatibility_DesertBase01`.

The definition stores GUID-only `AssetReference` values. Gameplay and composition policy still contain no direct scene path, no asset is loaded eagerly, and runtime behavior is unchanged.

The optional heavyweight metadata handle remains empty because the Addressable source scene owns its Entities subscene dependency. The minimap handle also remains empty until a real deterministic map-owned raster exists; no placeholder UI texture was accepted.

## Validation

- Unity builder execution: passed with zero compiler errors.
- `OperationMapLazyContentReferenceTests`: 9 passed, 0 failed.
- `OperationMapAddressablesLayoutBuilderTests`: 2 passed, 0 failed.
- `NonEcsSystemConversionArchitectureTests`: 9 passed, 0 failed.
- The committed GUIDs match the existing Addressables entries for all five assigned roles.
- `git diff --check`: passed after normalizing Unity's empty YAML fields.
- Logs: `/private/tmp/opmap-definition-binding-builder.log`, `/private/tmp/opmap-definition-binding-contract-results.xml`, `/private/tmp/opmap-definition-binding-layout-results.xml`.

## Remaining Gate

The Phase 2A all-addresses row remains open until the cached minimap raster is authored, assigned the stable `operation-map/opmap.skirmish.desert_base_01/minimap-raster` address, bound into the definition, and accepted by strict layout/type validation.

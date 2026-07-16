# Operation Map Catalog Contract

Date: 2026-07-16
Status: Accepted load-strategy-neutral catalog foundation

## Implemented

- Added `Game.Configs.OperationMapCatalogConfig` as the approved sealed config
  owner for a small explicit list of operation-map definitions.
- Added `OperationMapCatalog_Compatibility.asset` with only
  `opmap.skirmish.desert_base_01` registered.
- Added uniqueness, missing-definition, and nested-definition validation.
- Added ordinal id resolution with zero allocations after warmup.
- Extended the existing `OperationMapRuntimeBootstrapSceneSystemHelper` with a
  one-shot catalog publication overload. Composition resolves the selected
  definition once per publication and reuses the existing metadata path.

## Architecture

- No `AssetDatabase`, scene search, Resources lookup, Addressables handle,
  dictionary, service locator, manager, controller, or recurring update loop is
  present in runtime code.
- The catalog references small validated `OperationMapDefinition` assets only;
  heavyweight map payloads remain represented by bounded identities.
- The existing bootstrap helper remains the single composition boundary and
  retains exact source-growth authorization `D-163`.

## Validation

- Focused catalog/bootstrap/current-definition EditMode tests: `10 / 10`
  passed, `/private/tmp/opmap-catalog-final-tests.xml`.
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed,
  `/private/tmp/opmap-catalog-final-architecture.xml`.
- Unity compilation: zero compiler errors.
- `git diff --check`: passed.

This slice does not select or implement a loader, Addressables packaging,
scene load/unload behavior, remote content, map generation, or future physical
map rollout.

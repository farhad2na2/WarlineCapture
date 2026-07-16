# Current Operation Map Placement Staging

Date: 2026-07-16

## Scope

Created distinct operation-map placement assets:

- `OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset`
  - GUID: `a84d1564be130447197d5a5f2a7fe11d`
  - Entries: `451`
- `OperationMap_Compatibility_DesertBase01_VehiclePlacements.asset`
  - GUID: `819521cb66e1e4e90a46b49e04b15aec`
  - Entries: `29`

Before copying serialized gameplay data, the staging tool opens the extracted
operation-map scene and matches every eligible authored placement against the
accepted source config by hierarchy path, category, prefab, world position,
world rotation, and world scale. Duplicate hierarchy paths therefore do not
serve as unique identity.

The resulting assets preserve every serialized placement field and the current
`spawnOnMatchStart` and `hideAuthoringVisualsAfterSpawn` behavior. Their GUIDs
and internal asset names are operation-map specific. Existing Match config
assets and `MatchSceneView` bindings remain unchanged.

## Validation

- Focused current-map staging EditMode tests: `4 / 4` passed.
  - Results: `/private/tmp/opmap-placement-tests.xml`
  - Log: `/private/tmp/opmap-placement-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-placement-architecture.xml`
  - Log: `/private/tmp/opmap-placement-architecture.log`
- Unity compilation: zero compiler errors.
- No runtime loader, Addressables, generated presentation, or current Match
  binding changed.

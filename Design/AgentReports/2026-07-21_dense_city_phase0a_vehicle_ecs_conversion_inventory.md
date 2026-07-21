# Dense City Phase 0A Vehicle ECS Conversion Inventory

## Scope

- Tracker item: prove which current vehicle placements already produce ECS entities/render entities and identify only missing conversion/duplication cleanup.
- Probe: `Assets/Game/Scripts/Editor/OperationMapVehicleEcsConversionInventoryProbe.cs`
- Focused tests: `OperationMapVehicleEcsConversionInventoryProbeTests` — 4/4 passed
- Full report: `Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.json`
- Summary: `Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory_summary.json`
- Transient originals: `/private/tmp/warline-operation-map-vehicle-ecs-conversion-inventory{,-summary}.json`

## Result

`AllPlacementsAlreadyProduceEcs`

| Metric | Count |
|---|---:|
| Vehicle placements | 22 |
| Exact authored-source joins | 22 |
| Already ready | 22 |
| Cleanup required | 0 |
| Unresolved joins | 0 |

Every placement disposition is `AlreadyProducesEcsGameplayAndRender`:

- exact authored join;
- configured vehicle prefab + source key;
- `UnitGridAuthoring` with vehicle motion;
- `Model` root with renderers (ECS render entity path);
- destroyed visual prefab present;
- `HideAuthoringVisualsAfterSpawn` enabled (no duplicate-authoring risk under current config).

## Runtime path proved

`MapVehiclePlacementSpawnPrefabSystemHelper` → `RuntimeUnitPrefabSystem` → `UnitGridAuthoring.UnitGridBaker`

Vehicle work for Phase 0A is verification/cleanup only. No new vehicle Baker is required before cutover. Remaining gameplay conversion risk is on buildings (`RuntimeBuildingEntity` managed dependencies and attached visuals), not vehicles.

## Non-mutation guarantee

This slice did not change the canonical map scene, SubScene, static package, Addressables ownership, or `OperationMapPresentationKind`.

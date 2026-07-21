# Dense City Phase 0A RuntimeBuildingEntity Dependency Inventory

## Scope

- Tracker item: inventory managed `RuntimeBuildingEntity`/GameObject dependencies for current authored buildings and define equivalent ECS ownership before visual cutover.
- Probe: `Assets/Game/Scripts/Editor/OperationMapRuntimeBuildingEntityDependencyInventoryProbe.cs`
- Focused tests: `OperationMapRuntimeBuildingEntityDependencyInventoryProbeTests` — 5/5 passed
- Full report: `Design/AgentReports/2026-07-21_dense_city_phase0a_runtime_building_entity_dependency_inventory.json`
- Summary: `Design/AgentReports/2026-07-21_dense_city_phase0a_runtime_building_entity_dependency_inventory_summary.json`

## Result

`AllPlacementsRequireManagedRuntimeBuildingEntity`

| Metric | Count |
|---|---:|
| Building placements | 432 |
| Exact authored joins | 432 |
| Missing prefab / definition authoring | 0 / 0 |
| Requires managed RuntimeBuildingEntity | 432 |
| Has destroyed visual prefab | 266 |
| Has production slots | 20 |
| Has resource capacity/production | 70 |
| Requires runway ownership (`Building_Airport`) | 1 |
| Hide authoring after spawn | true |

## Dependency catalog → ECS ownership

Ten fixed managed surfaces were inventoried with proposed ECS replacements:

1. `Instance` hierarchy → `LocalTransform` + presentation entity hierarchy
2. Faction renderer colors → Entities Graphics material/color presentation
3. Door open state → optional animated entity or approved transient FX
4. Intact/destroyed visuals → intact/destroyed entity references
5. Animated resource visuals → baked animation or approved transient presentation
6. Production queues/slots → ECS buffers/components
7. Production transport/drop visuals → approved transient boundary or entity-space transport
8. `RuntimeBuildingEntityLink` sync → remove; combat entity owns transform
9. Runway transform discovery → typed ECS runway anchor/bounds
10. Selection/UI focus via Instance → entity-derived focus position

## Implication

Unlike vehicles (already ECS-ready), every current building placement still depends on managed `RuntimeBuildingEntity` presentation. ECS building conversion and attached-visual ownership remain GPT mid-point review gates before scene mutation.

## Non-mutation guarantee

No scene, SubScene, Addressables, presentation-mode, or asset mutation in this slice.

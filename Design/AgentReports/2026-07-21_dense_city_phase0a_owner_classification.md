# Dense City Phase 0A Owner Classification

## Scope

- Tracker item: classify every existing owner as gameplay building, gameplay vehicle, render-only entity, map metadata/proxy, approved managed boundary, or rejected/unresolved without name-based inference.
- Probe: `Assets/Game/Scripts/Editor/OperationMapEntityPresentationOwnerClassificationProbe.cs`
- Focused tests: `OperationMapEntityPresentationOwnerClassificationProbeTests` — 11/11 passed
- Full report: `Design/AgentReports/2026-07-21_dense_city_phase0a_owner_classification.json`
- Summary: `Design/AgentReports/2026-07-21_dense_city_phase0a_owner_classification_summary.json`

## Result

`OwnerClassificationComplete`

| Role | Count | Evidence |
|---|---:|---|
| GameplayBuilding | 432 | Exact building placement joins (authored `Map/Buildings/...`; absent from static manifest) |
| GameplayVehicle | 22 | Exact vehicle placement joins (authored `Map/Vehicles/...`; absent from static manifest) |
| RenderOnlyEntity | 9,090 | Static-manifest migration owners |
| MapMetadataProxy | 5 (catalog) | Grid/surface/blocker/runway/minimap metadata — non-visual |
| ApprovedManagedBoundary | 3 (catalog) | RuntimeBuildingEntity interim + approved transient FX |
| RejectedUnresolved | 0 | — |

Total classified owners: **9,544** (9,090 static + 432 buildings + 22 vehicles). All 432 GameplayBuilding owners require approved managed `RuntimeBuildingEntity` until ECS building cutover.

## Important finding

Authored gameplay building/vehicle visuals are **not** static-presentation sources. Classification therefore combines:

1. static-manifest migration owners → `RenderOnlyEntity`
2. exact placement-join targets → `GameplayBuilding` / `GameplayVehicle`

No name or proximity inference was used.

## Non-mutation guarantee

No scene, SubScene, Addressables, presentation-mode, or asset mutation in this slice.

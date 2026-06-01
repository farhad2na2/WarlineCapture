# Building Faction Visual Roadmap

This roadmap tracks replacing per-building `FactionMarker` prefab children with renderer-based building faction visuals.

## Target

Building ownership remains runtime/ECS state. Building faction visual projection belongs to `BuildingFactionVisualSystem`, which applies an explicit configured tint/material policy to cached renderers from the real building model. `FactionMarker` children must not exist in building prefabs.

## Ownership Rules

- `BuildingFactionVisualSystem` owns applying and clearing building owner-faction visuals.
- `BuildingRuntimeVisualSystem` caches visible building renderers and their base colors during runtime visual initialization.
- `BuildingRuntimeOwnershipSystem` updates ownership state, combat faction data, gate friendly-pass data, and delegates visual projection to `BuildingFactionVisualSystem`.
- `BuildingPlacementSystemConfig` owns building-specific faction visual policy such as tint strength.
- Building prefabs under `Assets/Game/Prefabs/Buildings` must not contain `FactionMarker` children.
- `FactionVisualSettingsConfig` remains the source of faction colors.
- Unit faction visuals and ECS `FactionVisualSystem` remain unchanged.

## Steps

1. Document this roadmap and contract rule.
2. Add explicit building faction visual config through the building gameplay config path.
3. Add `BuildingFactionVisualSystem`.
4. Update `RuntimeBuildingData` to remove `FactionMarker` fields and store cached renderers/base colors.
5. Update `BuildingRuntimeVisualSystem` to stop finding `FactionMarker` and cache real building renderers.
6. Wire `BuildingRuntimeOwnershipSystem` through `BuildingFactionVisualSystem`.
7. Update destroy/cleanup paths to remove `FactionMarker` visibility handling.
8. Remove `FactionMarker` children and inherited overrides from building prefabs.
9. Add architecture and focused behavior tests.
10. Run focused validation and runtime smoke.

## Progress

- Step 1: Complete.
- Step 2: Complete.
- Step 3: Complete.
- Step 4: Complete.
- Step 5: Complete.
- Step 6: Complete.
- Step 7: Complete.
- Step 8: Complete.
- Step 9: Complete.
- Step 10: Complete.

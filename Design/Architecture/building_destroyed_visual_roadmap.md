# Building Destroyed Visual Roadmap

This roadmap tracks replacing live building prefab `Destroyed` children with configured destroyed visual prefabs spawned only when a runtime building is destroyed.

## Target

Building destruction state remains owned by `BuildingCombatSystem`. Destroyed visual projection belongs to `BuildingDestroyedVisualSystem`, which hides live building visuals and spawns the configured `BuildingDefinition.DestroyedVisualPrefab` only when destruction begins. Live building prefabs under `Assets/Game/Prefabs/Buildings` must not contain `Destroyed` children.

## Ownership Rules

- `BuildingDefinitionAuthoringConfig` owns the per-building destroyed visual prefab reference.
- `BuildingDefinitionAuthoring` projects the config reference into `BuildingDefinition`.
- `BuildingRuntimeVisualSystem` initializes live building visuals only; it must not find or cache `Destroyed` children.
- `BuildingDestroyedVisualSystem` owns spawning, caching, hiding, and cleanup of runtime destroyed visual instances.
- `BuildingCombatSystem` owns destroyed state, cleanup deadlines, blocker/entity cleanup, and delegates visual projection to `BuildingDestroyedVisualSystem` through explicit context.
- Building prefabs under `Assets/Game/Prefabs/Buildings` must not contain `Destroyed` children after migration.
- Unit destroyed visual authoring remains unchanged.

## Steps

1. Audit current `Destroyed` child usage and destruction flow.
2. Document this roadmap and contract rule.
3. Add destroyed visual prefab data to building authoring/config and runtime definitions.
4. Create standalone destroyed visual prefabs from existing `Destroyed` children.
5. Add `BuildingDestroyedVisualSystem`.
6. Update runtime visual initialization to stop finding/caching `Destroyed` children.
7. Wire `BuildingCombatSystem` through `BuildingDestroyedVisualSystem`.
8. Remove `Destroyed` children and inherited overrides from building prefabs.
9. Add architecture and focused behavior tests.
10. Run focused validation.

## Progress

- Step 1: Complete.
- Step 2: Complete.
- Step 3: Complete. Building definition authoring/config now projects `DestroyedVisualPrefab` into runtime building definitions.
- Step 4: Complete. Existing prefab `Destroyed` children were extracted into standalone prefabs under `Assets/Game/Prefabs/Buildings/Destroyed`.
- Step 5: Complete. `BuildingDestroyedVisualSystem` owns spawned destroyed visual instances and cleanup.
- Step 6: Complete. `BuildingRuntimeVisualSystem` no longer finds or caches `Destroyed` children from live building prefabs.
- Step 7: Complete. `BuildingCombatSystem` delegates destroyed visual begin/cleanup through `BuildingDestroyedVisualSystem`.
- Step 8: Complete. Live building prefabs no longer contain `Destroyed` children.
- Step 9: Complete. Architecture and focused destroyed visual behavior tests cover the new boundary.
- Step 10: Complete. Shadow-project focused EditMode validation passed for `BuildingDestroyedVisualSystemTests` and the destroyed visual architecture contract.

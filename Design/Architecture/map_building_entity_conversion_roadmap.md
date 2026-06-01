# Map Building Entity Conversion Roadmap

## Goal

Convert authored `Match` scene map-building model groups into runtime building entities without replacing the existing building gameplay path.

## Completed Implementation

1. Add `MapBuildingPlacementConfig` as the authored placement data source.
2. Add `Warline > Map > Bake Match Building Placements` editor bake.
3. Scan `Map/Buildings/<BuildingPrefabName>` groups from the open `Match` scene.
4. Resolve each group to `Assets/Game/Prefabs/Buildings/<BuildingPrefabName>.prefab`.
5. Resolve owner faction from authored `Faction1` and `Faction2` bounds:
   - outside both is faction `0`
   - inside `Faction1` is player faction `1`
   - inside `Faction2` is enemy faction `2`
   - inside both is a bake error
6. Store source path, prefab reference, faction, world center, world transform, and rotate-vertical hint in the config asset.
7. Assign the config and `Map/Buildings` authoring root explicitly through `MatchSceneView`.
8. Add `MapBuildingPlacementSpawnSystem` as the runtime conversion owner.
9. Wire the system through building gameplay composition and the building runtime tick.
10. Spawn authored map buildings through `BuildingRuntimeSpawnSystem` registration so runtime building data, faction ownership, selection, combat, and UI read paths remain shared with normal buildings.
11. Hide the authored `Map/Buildings` model root after conversion completes.

## Ownership Rules

- Runtime code must not use broad scene lookup for map-building conversion.
- The editor bake may scan the open `Match` scene, but runtime receives only explicit `MatchSceneView` references and config data.
- Authored map layout is authoritative. Runtime conversion must not use the generic build-request path that can search and move a building to a nearby valid cell.
- Building systems must continue to own selection, ownership, combat, destruction, resources, and UI behavior.

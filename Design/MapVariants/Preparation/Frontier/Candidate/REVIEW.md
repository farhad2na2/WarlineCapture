# Frontier prepared authoring candidate

Status: In progress. Authoring and surface artifacts only; runtime packing, actual routes, native captures and device measurements remain gates.

- Independent neutral building owners: 3444; original duplicates: 0.
- Static alternatives removed by mapped branch provenance: 1467.
- Static movement cells: 264109; water/static exclusion cells: 305498; bridge deck cells: 400.
- Unsupported industrial/civic destruction pairs explicitly become non-destructible scenery. No unrelated rubble substitution.
- Rotated static footprints include partial boundary intersections. Gameplay owners use the existing conservative AABB runtime blocker contract.
- Surface build exclusion is separate from movement exclusion. Dynamic building footprints are blocked by owners, not duplicated as grid static blockers.
- Semantic hash: `e2d40d5eea9a0b81a1500cd14040329fc638b7909249ddf095ea3e5d9c6d54cf`; regeneration: `Game.Editor.MapVariants.MapVariantPreparedCandidateBuilder.BuildPort`; replay: Passed.

## Bounds decisions

| Zone | Decision | Center surface |
|---|---|---|
| Anchor_CanalBridge | Preserve authored footprint; center surface valid | BridgeDeck |
| Anchor_ControlTower | Preserve authored footprint; center surface valid | Pad |
| Anchor_EnemyEast | Preserve authored footprint; center surface valid | Ground |
| Anchor_EnemyWest | Preserve authored footprint; center surface valid | Ground |
| Anchor_FriendlyDepot | Preserve authored footprint; center surface valid | Pad |
| Anchor_Helipads | Preserve authored footprint; center surface valid | Pad |
| Anchor_LauncherRidge | Preserve authored footprint; center surface valid | Ground |
| Anchor_PortWatchtower | Preserve authored footprint; center surface valid | Pad |
| Frontier_AirfieldSector | Preserve authored footprint; center surface valid | Ground |
| Frontier_PortSector | Preserve authored footprint; center surface FAILED | WaterExcluded |
| Frontier_RefinerySector | Preserve authored footprint; center surface valid | Ground |

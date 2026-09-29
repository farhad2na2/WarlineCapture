# RefineryDistrict prepared authoring candidate

Status: In progress. Authoring and surface artifacts only; runtime packing, actual routes, native captures and device measurements remain gates.

- Independent neutral building owners: 217; original duplicates: 0.
- Static alternatives removed by mapped branch provenance: 1968.
- Static movement cells: 40008; water/static exclusion cells: 40008; bridge deck cells: 0.
- Unsupported industrial/civic destruction pairs explicitly become non-destructible scenery. No unrelated rubble substitution.
- Rotated static footprints include partial boundary intersections. Gameplay owners use the existing conservative AABB runtime blocker contract.
- Surface build exclusion is separate from movement exclusion. Dynamic building footprints are blocked by owners, not duplicated as grid static blockers.
- Semantic hash: `722770eae00961d6a82cea22081ceaa42839dd17f4c80437bf28647b054615ab`; regeneration: `Game.Editor.MapVariants.MapVariantPreparedCandidateBuilder.BuildRefinery`; replay: Passed.

## Bounds decisions

| Zone | Decision | Center surface |
|---|---|---|
| Anchor_EnemySpawnWest | Preserve authored footprint; center surface valid | Ground |
| Anchor_FriendlyDepot | Preserve authored footprint; center surface valid | Pad |
| Anchor_LauncherCompound | Preserve authored footprint; center surface valid | Ground |
| Anchor_RefineryGate | Preserve authored footprint; center surface valid | Pad |
| Anchor_Substation | Preserve authored footprint; center surface valid | Pad |
| Anchor_WestFork | Preserve authored footprint; center surface valid | Ground |
| PowerRelay_Substation | Preserve authored footprint; center surface valid | Pad |
| SplitFront_Ridge | Reduce ridge district north edge to 690m; maintain deployment margin; center surface valid | Pad |
| SupplyLine_Refinery | Preserve authored footprint; center surface valid | Ground |

# AshLinePort prepared authoring candidate

Status: In progress. Authoring and surface artifacts only; runtime packing, actual routes, native captures and device measurements remain gates.

- Independent neutral building owners: 232; original duplicates: 0.
- Static alternatives removed by mapped branch provenance: 1685.
- Static movement cells: 39828; water/static exclusion cells: 53221; bridge deck cells: 800.
- Unsupported industrial/civic destruction pairs explicitly become non-destructible scenery. No unrelated rubble substitution.
- Rotated static footprints include partial boundary intersections. Gameplay owners use the existing conservative AABB runtime blocker contract.
- Surface build exclusion is separate from movement exclusion. Dynamic building footprints are blocked by owners, not duplicated as grid static blockers.
- Semantic hash: `3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5`; regeneration: `Game.Editor.MapVariants.MapVariantPreparedCandidateBuilder.BuildPort`; replay: Passed.

## Bounds decisions

| Zone | Decision | Center surface |
|---|---|---|
| Anchor_EastCheckpoint | Move boundary anchor 20m inward to playable checkpoint approach; center surface valid | Ground |
| Anchor_EnemyWest | Preserve authored footprint; center surface valid | Ground |
| Anchor_MainBridge | Preserve authored footprint; center surface valid | BridgeDeck |
| Anchor_SouthBridge | Preserve authored footprint; center surface valid | BridgeDeck |
| Anchor_Watchtower | Preserve authored footprint; center surface valid | Pad |
| ConvoyApproach_Bridge | Preserve authored footprint; center surface valid | BridgeDeck |
| RouteReopened_Hub | Reduce north hub district to 690m; center surface valid | Pad |
| SupplyYard_East | Reduce north supply district to 690m; center surface valid | Pad |

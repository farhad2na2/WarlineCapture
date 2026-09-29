# CityEdgeAirfield prepared authoring candidate

Status: In progress. Authoring and surface artifacts only; runtime packing, actual routes, native captures and device measurements remain gates.

- Independent neutral building owners: 344; original duplicates: 0.
- Static alternatives removed by mapped branch provenance: 2552.
- Static movement cells: 22840; water/static exclusion cells: 22840; bridge deck cells: 0.
- Unsupported industrial/civic destruction pairs explicitly become non-destructible scenery. No unrelated rubble substitution.
- Rotated static footprints include partial boundary intersections. Gameplay owners use the existing conservative AABB runtime blocker contract.
- Surface build exclusion is separate from movement exclusion. Dynamic building footprints are blocked by owners, not duplicated as grid static blockers.
- Semantic hash: `e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22`; regeneration: `Game.Editor.MapVariants.MapVariantPreparedCandidateBuilder.BuildAirfield`; replay: Passed.

## Bounds decisions

| Zone | Decision | Center surface |
|---|---|---|
| AirCorridor_Tower | Reduce north corridor district to 690m; center surface valid | Ground |
| Airlift_Helipad | Reduce north helipad district to 690m; boarding clearance still requires movement validation; center surface valid | Pad |
| Anchor_CityGate | Move boundary anchor 10m inward; preserve gate approach rather than out-of-map center; center surface valid | Ground |
| Anchor_ControlTower | Preserve authored footprint; center surface valid | Pad |
| Anchor_EnemySouth | Preserve authored footprint; center surface valid | Ground |
| Anchor_FieldHospital | Preserve authored footprint; center surface valid | Pad |
| Anchor_Helipads | Preserve authored footprint; center surface valid | Pad |
| ForwardPost_West | Preserve authored footprint; center surface valid | Ground |

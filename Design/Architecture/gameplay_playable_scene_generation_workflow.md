# Gameplay Playable Scene Generation Workflow

## Goal

Generate high-quality 3D RTS scenes that are playable on the flat 2D grid before they are visually dressed. The generator must prevent houses, rocks, cliffs, and decoration from blocking streets or sitting inside each other.

## Required Order

1. Define the gameplay grid and scene grammar.
2. Reserve walkable roads, sidewalks, spawn zones, combat lanes, and objective access.
3. Place structural buildings only into legal lots beside roads.
4. Place blocker rocks, walls, cliffs, and fences only after walkable lanes are reserved.
5. Place cover and tactical props beside, not inside, walkable roads.
6. Place decoration last.
7. Place proof soldiers on walkable streets and show their intended route lines.
8. Validate the layout automatically.
9. Capture visual proofs.
10. Fix generator rules, not individual object nudges, when validation or visual review fails.

## Validation Gates

- Road rectangles must stay clear of building footprints.
- Road rectangles must stay clear of blocker rock/cliff footprints.
- Building footprints must not overlap each other.
- Blocker footprints must not overlap buildings.
- Soldiers and route points must sit on walkable roads or spawn zones.
- Every required route must connect from spawn to objective without crossing a blocked footprint.
- Large scenic rocks may frame the city, but may not sit in street centers or inside houses.
- Generated scenes must include a top-down walkability proof and at least one perspective RTS proof with visible soldiers.

## Naming

- `GC02` remains the visual composition prototype.
- `GC03` is the first playable-city generator pass.
- Reports and captures live under `Design/AgentReports`.
- Generated scenes live under `Assets/Game/Scenes/Generated`.

## First GC03 Target

Build a 2048x2048 flat-grid scene with:

- central highway;
- west-side dense town blocks;
- east-side military base edge;
- side streets and alleys;
- soldiers placed on walkable streets;
- route proof strips on streets;
- overlay proof for walkable, building, blocker, spawn, and objective zones.


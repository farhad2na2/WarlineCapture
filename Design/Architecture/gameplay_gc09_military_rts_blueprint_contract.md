# GC09 Military RTS Blueprint Contract

## Goal

Build the RTS military scene from a blueprint first, then place assets against the accepted plan. The scene must not be a duplicated Demo layout and must not be random object scatter.

## Blueprint Source

- Visual blueprint: `Design/Blueprints/gc09_military_rts_2048_blueprint.svg`
- Target map size: 2048 x 2048 flat playable plane.
- Style target: dense top-down Polygon Military RTS map with a city/town district, highway approach, military base, runway, vehicles, walls, guard towers, roads, and clear combat lanes.

## Required Build Order

1. Create the flat 2048 gameplay plane.
2. Create the road network and gates.
3. Reserve walkable lanes and spawn/objective zones.
4. Place district modules inside named blueprint zones only.
5. Place blockers, walls, rocks, and scenic dressing outside walkable lanes.
6. Place proof soldiers/units on walkable roads.
7. Capture visual proof from high RTS cameras.
8. Fix the blueprint or zone rules first if the result fails, not individual random object positions.

## Districts

- West city/town district with market objective.
- Central highway and open combat approach.
- East/right military base compound.
- West/north-west tent camp and barracks inside the base.
- Central command objective inside the base.
- North-east runway/apron with aircraft.
- South-east vehicle depot objective.
- East fuel/utility boundary.
- Scenic rocks/terrain only on marked blocked edges.

## Hard Rules

- No copied Demo scene layout as the final map.
- No unplanned duplicated Demo chunks around the map.
- No terrain/hill chunks inside playable lanes.
- No houses, rocks, aircraft, vehicles, or walls inside reserved roads.
- No visible debug overlays in beauty captures.
- Units must be visible on roads and should prove the planned lanes.
- The blueprint must show the whole 2048 map, including city and base, without cropped right-side districts.

## Validation

- Road network exists before asset placement.
- All proof units are on roads or spawn/objective zones.
- Every district is placed inside its named blueprint zone.
- Blocked scenic zones do not overlap roads.
- Capture includes at least one full blueprint-style overview and one closer RTS gameplay view.

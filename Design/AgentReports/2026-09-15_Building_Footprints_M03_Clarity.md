# Building footprints and M3 construction clarity — 2026-09-15

## Changes

M5 uses `Building_Satelite_Dish` for the enemy radar objective. Its old 20×10 reservation exceeded its roughly 9.74×9.72 model. M2 Barracks used a 40×20 reservation around its roughly 27.10×14.86 model. Ordinary runtime definitions now derive their footprint from model geometry, rounded up to grid cells, instead of retaining an oversized historical reservation. Catalog authoring is synchronized to the same measurements.

The measurement uses transformed local mesh bounds, excludes disabled/inactive geometry and non-mesh effects, and is independent of world rotation. Road gates retain their intentional 8×2 moving-arm clearance. The two base templates without visible geometry retain their fallback reservations. Ordinary buildings still use road-blocking placement validation; the existing road-gate exception is preserved.

Authored map buildings bake their grid origin and footprint from the actual intact model in world space, including instance rotation and scale. This prevents a corrected reusable prefab size from being combined with a historical map origin and incorrectly reserving the M2 Barracks plot. This includes generated dense-city buildings: the audit found a shop visually at world (1708, 689.5) still reserving cells (935, 428), on M3’s road. Baking now measures the current visual hierarchy instead of retaining that stale reservation. Geometry-free owners retain their authored fallback.

The placement confirmation panel now reads the active, rotated footprint through a typed query instead of displaying the fixed 3×3 placeholder.

M3's Build button previously acknowledged only the M2 tutorial. Opening it during M3 step 3 now advances to the construction lesson. That lesson offers its two valid choices (Road Barrier / Guard Tower), explains in English and Farsi that only one is needed, distinguishes on-road gates from off-road towers, and retains normal manual selection, rotation and confirmation. The Barracks returns to the normal catalog outside this lesson. Opening the drawer no longer suppresses the shared yellow next-click cue when ARIA is temporarily hidden. Gate and tower previews prefer their separate mission defense anchors within the allowed build zone, keeping the intended road or off-road site in view.

Runtime spawn relocation now retains an 80-cell minimum search radius (still capped at 160), independent of a corrected small footprint. Previously shrinking the radar reservation also halved its search reach, preventing it from finding clear land outside the occupied compound. Player placement remains subject to its existing bounded search and mission zone validation.

Correct map reservations exposed a blocked final stretch of M3's convoy route through the airfield. The route now follows the clear eastern edge before returning to its destination. The generated map and scenario were rebuilt together; the runtime grid check validates all 25 route segments with five-cell vehicle clearance before the player deliberately places a gate across the approach.

The placement readout is a passive UI query on the existing command adapter. Its immutable context data and binding factory now live in focused companion files. The command adapter has its own file. Retired source-growth exceptions D-141 and D-142 are removed because these dispatch files are below their original ceilings; no ceiling is raised.

## Catalog audit

| Building | Configured cells | Runtime cells |
|---|---|---|
| Building | (20, 10) | (20, 10) |
| Building_Airport | (210, 55) | (210, 55) |
| Building_Ammunition_Depot | (19, 16) | (19, 16) |
| Building_Barrack | (28, 15) | (28, 15) |
| Building_Fuel_Bladder | (14, 10) | (14, 10) |
| Building_GuardTower | (3, 4) | (3, 4) |
| Building_GuardTower_Big | (4, 7) | (4, 7) |
| Building_Hall | (24, 37) | (24, 37) |
| Building_Helipad | (16, 15) | (16, 15) |
| Building_House | (5, 7) | (5, 7) |
| Building_OilPump | (15, 4) | (15, 4) |
| Building_Refinery | (25, 15) | (25, 15) |
| Building_Refinery_Big | (53, 38) | (53, 38) |
| Building_Road_Barrier | (8, 2) | (8, 2) |
| Building_Satelite_Dish | (10, 10) | (10, 10) |
| Building_Shop | (10, 7) | (10, 7) |
| Building_WaterTank | (3, 4) | (3, 4) |
| Portaloo | (2, 3) | (2, 3) |
| Tent | (20, 10) | (20, 10) |
| Tent_Contractor | (13, 11) | (13, 11) |
| Tent_Expert | (15, 8) | (15, 8) |
| Tent_Refugee | (10, 6) | (10, 6) |
| Tent_Regular | (15, 8) | (15, 8) |
| Wall_Dirt_Straight | (12, 3) | (12, 3) |
| Wall_Fence_Straight | (12, 1) | (12, 1) |

## Validation

- Catalog and focused regressions: passed, 25 definitions, 6 geometry cases and the compact-building relocation case (`/private/tmp/warline-footprint-regressions-complete2.log`).
- Architecture: passed, 9 fixtures / 139 checks / 0 failures, in the same log. No architecture ceiling was raised. The final rerun includes the rebuilt route and current baking code.
- M2 actual Editor placement and Continue: passed (`/private/tmp/warline-m02-footprint-continue-final.log`). Verified the 28×15 footprint query and visible readout, green preview, confirmation, immediate Continue consumption and next rifle-production instruction.
- M5 actual radar spawn: passed (`/private/tmp/warline-m05-radar-footprint-verified.log`). Verified the rendered model, combat footprint and selection footprint all agree with the 10×10 reservation.
- M3 English and Farsi construction-to-victory Editor journeys: passed (`/private/tmp/warline-m03-building-journey-en6.log`, `/private/tmp/warline-m03-building-journey-fa9.log`). Used the actual catalog card, placement controls, drag, rotation and confirmation; verified a green road gate and rejection of ordinary buildings on the road. Continued with normal squad selection, Move, Hold and Stop orders, real combat, three-star victory, frozen finale clock, all three debrief panels, localized result screen and result-guide return. No health or outcome was injected. Both runs also passed the actual map route and 20-actor spawn-clearance checks.

Evidence captures remain under `/private/tmp`, outside the Design folder. These checks do not establish Android performance or certify absence of every gameplay bug.

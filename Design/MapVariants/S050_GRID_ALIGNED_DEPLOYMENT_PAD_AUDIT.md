# S050 grid-aligned deployment pad audit

## Decision and scope

This is a logical-layout candidate for the existing `opmap.ch05.citywide_alert` Campaign source. It changes no Campaign terrain, roads, bridges, scenes, or physical map assets. The candidate remains source-only: it does not establish runtime fit, mission readiness, or player readiness.

The player-facing reason for the bounds change is to give each faction a usable deployment area for the compiler-selected force, instead of projecting all starting units to a one-cell point. The source candidate is grid-aligned so the typed-pad cell mask contains complete cells.

## Pad bounds and force facts

The original layout used an infantry center at player `(923,416)` and enemy `(1188,416)`, with a `1×1 m` pad. Its centers fell on grid-line intersections, so the typed-pad mask contained no complete grid cell. Vehicle pads were `3×3 m` at `(932,426)` and `(1178,426)`, also centered on grid-line intersections.

The current candidate keeps the source-returned path cells as deployment centers and places pad centers at cell centers:

| Pad family | Faction | Old center / bounds | Candidate center | Candidate bounds | Owner facts used |
| --- | --- | --- | --- | --- | --- |
| Infantry spawn | Player | `(923,416)`, `1×1 m` | `(938.5,416.5)` | `15×11 m` | 20 per faction; owner footprint `1×1` |
| Infantry spawn | Enemy | `(1188,416)`, `1×1 m` | `(1188.5,416.5)` | `15×11 m` | 20 per faction; owner footprint `1×1` |
| Vehicle spawn | Player | `(932,426)`, `3×3 m` | `(932.5,426.5)` | `9×7 m` | 6 per faction; owner footprint `3×3` |
| Vehicle spawn | Enemy | `(1178,426)`, `3×3 m` | `(1178.5,426.5)` | `9×7 m` | 6 per faction; owner footprint `3×3` |

The refreshed Standard/Regular author replay confirms the compiler-selected BaseAssault request pins `Assets/Game/Resources/SkirmishBaseAssault.asset` at SHA-256 `ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe`. Its owner-projected compiled roster is 20 infantry and 6 vehicles per faction. The source-only geometry provides 165 infantry cells per pad and six nonoverlapping `3×3` vehicle slots in each `9×7` pad. Those are capacity calculations, not observed unit allocations.

The `AirReturn` pad kind remains unchanged. The selected force has no air unit rows; the separate helipad structure placement is preserved. Rally pad centers and all 20 starting-structure centers are also projected to cell centers by the builder, without moving any Campaign geometry.

## Current source checks

The source layout builder reads the current physical capture’s walkability, authored-building, road/sidewalk, and navigation-blocker masks. It also accounts for the 18 provisional non-helipad starting-structure rectangles as blockers for the owner path probe. At the candidate infantry pads, all 165 cells are captured walkable and there are zero authored-building or navigation-blocker cells. At the vehicle pads, all 63 cells are captured walkable and there are zero authored-building or navigation-blocker cells.

The candidate pads overlap captured `authoredRoadsIncludingSidewalks` in 141 of 165 infantry cells and all 63 vehicle-pad cells; captured sidewalks are zero in these windows. This overlap is recorded rather than treated as a unit-placement rejection. The live starting-unit owner passes walkability, dynamic blockers, occupancy, reservations, and surface traversal to `SpawnCellUtility` / `SkirmishSpawnPlacement`; it does not consult authored road or sidewalk masks. Road restrictions belong to building placement and must not be applied to unit pads without evidence from the unit owner.

The original source path/surface owner accepted the exact candidate endpoints and returned connected routes: infantry `[78,116]` to `[328,116]` (251 cells), vehicle `[72,126]` to `[318,126]` (2 cells). The probe used the current physical capture’s walkability, authored buildings, navigation blockers, and the 18 provisional starting-structure rectangles. It did not allocate a Unity World or measure live occupancy. A successful route therefore does not prove that all 52 units can be placed or that a battle fits.

## Compiler and evidence status

The current layout JSON SHA-256 is `555eb1dbd253ea290ae9a93cf7ca085f70d0e480d3606fae7620e50b2785aafc`. R3 and r4 are byte-identical. The standalone original compiler/binding probe accepted the candidate bytes and roundtripped the logical binding; its bound setup hash is `1275857659` and layout semantic fingerprint is `2c5bc3d4ab123d167dc8046a24a518931b17c29193070527f602b7a110347592`. The checked source-owner author replay independently accepted the exact layout, binding, BaseAssault preset, S050/104779 identity and pins with the same compiled setup hash. The replay did not initialize a World.

The standalone binding probe consumes the source config graph, candidate layout, and Standard/Regular setup row; the author replay then applies the exact BaseAssault preset and compiler request. Existing physical capture and source-map masks can be reused only where their exact input pins remain unchanged. Bundle/capture requests that pin an older author result must be regenerated or explicitly rejoined to the new result before use.

Evidence receipts:

- Candidate layout: `T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-layout-20261116-r4/receipt.json`
- Original compiler/binding roundtrip: `T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-binding-roundtrip-20261116-r1/receipt.json`
- Current source path-owner probe: `T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-owner-path-r1-20261116-r1/receipt.json`
- Exact S050/BaseAssault owner replay: `T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-author-replay-20261116-r2/authoring-receipt.json`

All receipts keep `source_fit_verified=false` and `mission_ready=false`.

## 2026-11-16 owner-failure-driven candidate revision

The retained source-host one-tick run did not initialize all compiler-selected vehicles. It placed five player-side 3×3 vehicles (car `(72,126)`, APC `(69,127)`, tank `(75,124)`, tanker `(75,127)`, and first logistics truck `(69,124)`), then rejected the sixth request, `role.logistics_truck.1.13.1`, through `spawn_cell_search_failed` at tick 2. The process log's global candidate-origin census reports 1,316 out-of-bounds, 16,107 unwalkable, 4,798 blocked, 74,526 occupied, 53 reserved, and 0 available. This is actual owner failure evidence; it does not identify a single rejected cell or attribute the result solely to the pad geometry.

The latest previously authored layout used player vehicle center `(932.5,426.5)` and enemy center `(1178.5,426.5)`, both with `9×7 m` pads. The new source-only candidate extends each same source-selected pad one grid row toward `+Z`, changing both to `9×8 m` and centers `(932.5,427.0)` and `(1178.5,427.0)`. This is a bounded logical-layout experiment; it changes no Campaign source geometry or gameplay budget. The current captured mask reports 72/72 walkable cells, zero authored building cells, zero navigation-blocker cells, and 72 authored-road cells in each candidate pad. The unit owner does not use authored-road masks as a rejection rule, so road overlap is not treated as a defect.

The added row widens the source-selected search region but does not increase the simple rectangular non-overlapping 3×3 tiling count beyond six; the actual owner must determine whether the changed bounds allow the sixth request under live occupancy, reservations, and traversal. No fit claim follows from the static masks. The candidate at `T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-layout-20261116-r8/layout.json` has SHA-256 `80ed16299228baeb7c39ef5e18da5c50a1f17d6ed1b80574dd0de78cbcc79fe0`; receipt `.../receipt.json` has SHA-256 `6106f5ec1ddde9c026b6643edf02aa2e12b44de95bb2f5734741604b391a704f` and pins the failed run, bundle, previous layout, owner log, exact BaseAssault request, source masks, and compiler roster. That author result still refers to the previous layout, so the revised bytes need a fresh original-owner binding and author replay before any current capture or source-host run.

The author request pins `Assets/Game/Resources/SkirmishBaseAssault.asset` SHA-256 `ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe`. The checked request/result preserve that preset selection and the 20-infantry/6-vehicle roster per faction. However, direct-owner parity evidence does not establish the preset's runtime cadence; cadence remains unverified. Source fit, runtime admission, and mission readiness remain false.

## 2026-11-16 r9 response to the enemy logistics-truck rejection

The first actual one-tick run on r8 placed 51 of the 52 compiler-selected force actors. It placed all 20 infantry and all 3 combat vehicles per faction, both tankers, and the first logistics truck for each faction; the final enemy `role.logistics_truck.2.15.1` was rejected at tick 2 by `spawn_cell_search_failed` with a 3×3 footprint and radius 12. The captured owner census over all grid candidate origins was 1,316 out-of-bounds, 16,107 unwalkable, 4,798 blocked, 74,522 occupied, 57 reserved, and 0 available. This is an actual owner failure, but does not identify a single rejected cell or prove that the logical pad alone caused it.

For the candidate revision, the current r4 physical readset was decoded for walkability, authored buildings, and navigation blockers. The r1 failure-state actor list contains 71 actual starting actors (36 player-side, 35 enemy-side). Their cell footprints were reconstructed from the r3 original unit and building owner metadata. In the old 9×8 vehicle-pad bounds, this approximation leaves 0 player and 2 enemy 3×3 origins after applying those actors and the captured static masks. A centered 15×16 window leaves 61 player and 83 enemy origins under the same calculation. The calculation omits live dynamic blockers, allocator reservations, and surface traversal. It provides margin for a new owner check; it is not a placement or fit result.

The r9 candidate keeps the current source-selected route cells `(72,126)` and `(318,126)` and preserves the tested pad centers `(932.5,427.0)` and `(1178.5,427.0)`. It expands both vehicle pads from 9×8 m to 15×16 m, with grid-edge bounds `[65,119]..[80,135]` for the player and `[311,119]..[326,135]` for the enemy. The change is logical layout data only: Campaign terrain, roads, physical geometry, deployment anchors, and force budgets do not change. A fresh binding and author replay are required before these bytes can feed a current capture or run.

Candidate artifact: `T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-layout-20261116-r9/layout.json`, SHA-256 `448ac50e60f072a1ee42af1a3e6f4505e1a5e3d0022f639ae7fc9a87ec2e9f71`. Its receipt `.../receipt.json` pins the current r3 BaseAssault author result, r4 physical readset, r3 direct owner footprints, and r1 failed live owner run. The worker exception path omitted installed-map receipt serialization; the current source-map package was pinned and its separate source-only preflight passed, so the run receipt's `source_derived_map_bound=false` is not evidence that the map was absent or installed. BaseAssault runtime cadence was not reached: source-owner initialization that consumes the preset is called only after all opening placements succeed. Source fit, runtime admission, mission readiness, and cadence remain unverified.

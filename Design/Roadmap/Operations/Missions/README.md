# All 60 mission briefs

## Existing Campaign maps only — 2026-10-03

Follow the [Campaign map reuse policy](../../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md). All six districts bind current Campaign physical sources; no new district environments, mountain terrain, bridges or runways. The district briefs update all source assignments while retaining mission graphs and budgets. New physical map design requires an explicit owner request.

## Future-map planning amendment — 2026-09-29

Use the [future map plan](../../../MapVariants/FUTURE_CONTENT_MAP_PLAN.md) for all 57 uncoded entries O004–O060. The six district packets now assign physical-source foundations and each future entry carries a map-preparation requirement. O001–O003 retain their current bindings. D03/D04/D06 derive from prepared RefineryDistrict/AshLinePort/CityEdgeAirfield; D01/D02 use urban layouts and D05 needs new highland terrain. Graphs, catalog IDs, budgets and evidence remain unchanged.

## Mission product amendment — 2026-09-28

Apply the [mission product contract](../../../Monetization/Mission_Product_Contract_2026-09-28.md) and [205-entry register](../../../Monetization/Mission_Product_Policies_2026-09-28.csv). All 60 individually authored entries belong to the complete theater. O001–O003 additionally use the standalone free-intro scope. Each district brief now contains a product/scope and required-adjustment row per mission. Objectives, IDs, deadlines, force packages and existing acceptance fields remain unchanged.

## Support contract for every district brief

Apply the September 28 [Support roadmap](../../Support/PLAN.md) and matching [O001–O060 policy row](../../Support/OPERATIONS_SUPPORT.csv) to every brief below. Family-based ceilings are optional tools; bind them to mission targets/resources/routes and narrow with a recorded reason where needed. A finale cannot introduce a tool for the first time. Support is not yet runtime-enabled or accepted.

Status: **O001–O003 authored prototypes; O004–O060 planned.** O001–O003 have automated Regular EN Play Mode captures, but each entry still needs normal manual and shipping-input ARIA acceptance before release. Start with [O001 player-ready integration](../O001_PLAYER_READY_IMPLEMENTATION.md). Read [shared implementation](../MISSION_IMPLEMENTATION.md), [strategic rules](../STRATEGIC_RULES.md) and [acceptance](../ACCEPTANCE.md) first. All target names in graphs are scenario role aliases to bind to typed map anchors/entities, not existing GameObject names.

| District | Mission IDs | Detailed briefs |
|---|---|---|
| D01 Old Quarter | O001–O010 | [Old Quarter](D01_old_quarter.md) |
| D02 Civic Center | O011–O020 | [Civic Center](D02_civic_center.md) |
| D03 Industrial Belt | O021–O030 | [Industrial Belt](D03_industrial_belt.md) |
| D04 River Crossing | O031–O040 | [River Crossing](D04_river_crossing.md) |
| D05 Highland Approach | O041–O050 | [Highland Approach](D05_highland_approach.md) |
| D06 Airport Perimeter | O051–O060 | [Airport Perimeter](D06_airport_perimeter.md) |

## Family coverage

| Family | Authored missions |
|---|---:|
| AIRLIFT | 3 |
| BREACH | 5 |
| DEFENSE | 5 |
| ESCORT | 7 |
| FINALE | 6 |
| INTERDICT | 4 |
| PATROL | 4 |
| RAID | 4 |
| RECON | 6 |
| REPAIR | 6 |
| RESCUE | 5 |
| SEIZE | 5 |
| **Total** | **60** |

Source-of-truth boundaries: CSV owns stable identity/index/status; these briefs own mission parameters; shared documents own default behavior. Update both identity references together when reviewing a proposed unpublished replacement. Never recycle a published ID for different gameplay.

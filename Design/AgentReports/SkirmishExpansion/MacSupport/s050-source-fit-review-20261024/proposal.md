# S050 existing Campaign source proposal

Base: `97df692e737f03699550ad0eea72d8e19eb36cc0`. Review branch: `codex/mac-s050-source-fit-review-20261024`. This is a calculated logical placement proposal. No mission asset, shared gameplay code, terrain, road, scene, grid, surface or prefab is changed. No native run was assigned or performed.

Assess the existing **Citywide Alert** source, `opmap.ch05.citywide_alert`, for an alternate S050 layout. It is a current Campaign source, with its mission, catalog, definition, grid, surface and scene references traced at the exact base. Keep S050's `MP / BA / G / E` identity and all three fixed size rows. The current Armor Break bindings remain unchanged until a general logical-source/layout seam and its affected consumers are reviewed.

## Width obstruction and proposed construction

The Windows coordinator attributed the historical obstruction to linear widths: Barracks 28 + Refinery 25 + Refinery Module 25 = **78 cells**, sharing vertical slice `[359,369)` inside the 60-cell horizontal envelope `[911,971)`. The reported 174 legal origins and 190,512 checked triples concern that historical finite sector. The original strict run failed; a cached 17-input certificate did not establish fresh compiler closure. These observations do not establish impossibility across Armor Break.

The new source-cell construction gives that row **88 cells including two 5-cell gaps**, within a 110-cell compound. Its explicit disjoint intervals satisfy the width arithmetic without reducing any facility. The five-cell gaps are an engineering choice, not a newly asserted runtime clearance rule.

| Item | Half-open source cells | Dimensions |
|---|---|---|
| Logical sector | `[122,47 .. 432,161)` | 310 × 114 |
| Player compound | `[133,47 .. 243,137)` | 110 × 90 |
| Enemy compound | `[322,47 .. 432,137)` | 110 × 90 |
| Facility source-screen rectangle | `[133,47 .. 432,112)` | 299 × 65 |

The grid is 440 × 220 at origin `(860,0,300)`, with 1 m cells. The logical sector maps to world XZ `[982,347 .. 1292,461)`. Rows occupy the upper part of each compound; the assembly areas use the connected ground below them.

| Facility row | Unrotated configured rectangles | Row width including gaps | Depth |
|---|---|---:|---:|
| 1 | Refinery 25×15; module 25×15; Barracks 28×15 | 88 | 15 |
| 2 | Fabrication 19×16; Ground Staging 20×14; Fuel Bladder 14×10; Helipad 16×15 | 84 | 16 |
| 3 | Oil Pump 15×4; Satellite 10×10; two Watchtowers 3×4 | 46 | 10 |

Five-cell gaps between rows give a 51-cell shelf depth. Standard omits only the second refinery module, retaining its exact ten facilities; War and LargeWar retain all eleven. Helipads remain included despite the zero initial aircraft count.

Ground Staging's historical measured model footprint is 14×7, while its configured footprint is 20×14. The calculation reserves the larger configured rectangle. Other measured model dimensions match the listed configured dimensions. Exact model selection, origins, visual offsets and dependency closure remain native factory gates. These rectangles are source-review reservations, not certified object transforms.

## Fixed budgets and calculated source checks

| Size | Infantry each | Combat vehicles each | Support each | Total actors each | Facilities each | Materials / oil / usable fuel each | Deadline |
|---|---:|---:|---:|---:|---:|---|---:|
| Standard | 20 | 3 | 3 | 26 | 10 | 900 / 240 / 700 | 1080 s |
| War | 28 | 5 | 4 | 37 | 11 | 1350 / 360 / 1050 | 1500 s |
| LargeWar | 40 | 7 | 4 | 51 | 11 | 1800 / 480 / 1400 | 1800 s |

The complete canonical rows, including queues, caps, supply and capacities, are retained in `calculation.json`. Support remains one tanker plus the remaining logistics trucks. Infantry uses verified 1×1 footprints; all combat/support vehicles use verified 3×3 footprints. Loaded vehicle fuel remains separate from usable store fuel.

At the exact existing Citywide grid/surface, all proposed configured facility rectangles pass the conservative serialized-cell screen: playable bounds, authored grid clearance, BuildingPlacement bit, no road flag, finite decoded height at most 0.75 m above grid origin, and slope at most 8 degrees. Each proposed rectangle has zero decoded height spread, within the ordinary 0.5 m foundation bound. The BuildingPlacement bit is an additional conservative screen; it is not the complete ordinary placement predicate. Authored-starting phase foundation bypass and survey-added checks remain distinct.

For all three sizes, the calculated facilities and initial actor footprints are disjoint. The minimum opposing actor-center separation is 130 m; barracks centers are 189 m apart. These are measured construction distances, not a claim that every compiler exclusion or gameplay readability rule is satisfied.

After conservatively reserving all facility rectangles, a four-neighbor graph for complete 3×3 unit footprints remains connected inside the proposed logical sector. It reaches every initial spawn center, a conservative edge approach for every facility, and three separate 84 m centerlines at world Z 355.5, 391.5 and 439.5. The three centerlines have zero decoded center-height steps. The graph excludes current moving-unit occupancy and does not certify native service goals, runtime traversal or tactical route independence.

## Remaining gates

- The current MP constructor/source guard is Armor Break-specific. Review a general typed logical-source/layout input and affected consumers before materializing this alternate; preserve S049 and other coded bindings.
- Validate the actual source-scene runtime binding, packed scenery/static blockers, complete prefab/collider dependencies, model footprint/origin and ordinary placement-cache predicates. The historical survey has no live occupancy certificate. Citywide and Network Collapse were not covered by that survey, and their authored scenes changed after it.
- Validate all three exact-size candidates through normal compilation/materialization, original service-goal selection, repair/rally/approach reservations, seed-dependent spawn/fallback and opposing-force exclusion rules.
- Establish real grounded actor Y, native traversal, three-approach independence and readability. This proposal supplies XZ calculations only; the original ground sampler and factory build-plane policy remain authoritative.
- Keep normal manual/ARIA play, result/return, clean lifecycle, checkpoint/recovery and real player/device acceptance separate. Every readiness flag in this proposal remains false.

Reproduce the bounded calculation without Unity, LFS fetching or geometry writes:

```sh
rtk proxy python3 Design/AgentReports/SkirmishExpansion/MacSupport/s050-source-fit-review-20261024/calculate.py \
  --project "$PWD" --output "$PWD/Logs/s050-source-cell-proposal.json"
```

The output file must be new. `proposal.json`, `source-facts.json` and `calculation.json` carry exact source pins, the complete construction and explicit unresolved scope. The survey payload, screenshots, camera evidence, native logs and compiled binaries are excluded.

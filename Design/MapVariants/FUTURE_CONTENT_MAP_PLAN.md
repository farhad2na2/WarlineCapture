# Future content map plan — Campaign, Skirmish and Operations

Updated 2026-09-29 at the owner's request. **Adopted planning direction; no map or mission implementation, publication or acceptance is implied.**

## Review checkpoint — 2026-10-03

CH05-M04 Last Corridor is ready for player review in the primary project. Native core06, complete normal-input EN08/manual and FA02/ARIA journeys, seven caption panels per locale, actual five deliveries and custody, three-star result/rewards/return, and final packed-content03 passed. All six source hashes are preserved. Both ground lanes and optional Supply approval/collection/decline were exercised. Real player/device acceptance remains pending; voice generation is excluded by request. See the [current readiness record](../AgentReports/CH05M04LastCorridor/review-readiness.md).

## Implementation checkpoint — 2026-10-02

CH05-M04 Last Corridor now has an implemented independent bounded Urban logistics derivative under the approved Frontier fallback. Its two certified ground choices are separate lanes on the same existing artery, preserving five delivery categories, finite Fuel, physical repair and authority custody. Native map/rules/objectives passed `core-validation-03`; six source SHA256 values remain unchanged. Complete normal-input journeys, packed content and real player/device acceptance remain pending at this checkpoint. See the [map audit](../AgentReports/CH05M04LastCorridor/map-preparation-audit.md) and [current mission readiness](../AgentReports/CH05M04LastCorridor/review-readiness.md).

The scope counts below describe the 2026-09-29 planning checkpoint and remain historical; they are not the current remaining-code count.

## Scope and dependencies

This plan assigns physical-source candidates and required authoring work to content not yet coded at this checkpoint. Read [map preparation](HANDOFF_Map_Preparation.md) first. The separate [existing Campaign mission rework](HANDOFF_Existing_Mission_Rework.md) owns seven coded migrations and eleven retained-map regressions; do not apply this plan to those missions as a second assignment.

| Mode | Future scope at this checkpoint | Existing coded content excluded from rebinding |
|---|---|---|
| Campaign | 7: CH04-M04/M05 and CH05-M01–M05 | CH01–CH03 and CH04-M01–M03 (18) |
| Skirmish | 114: S005–S024, S026–S072, S074–S120 | Prototype mappings S001/S025/S073 and expanded candidates S002/S003/S004 (6) |
| Operations | 57: O004–O060 | O001–O003 (3) |

These 178 future entries retain their existing catalog identities and count. Re-audit runtime definitions/publication state before coding: Planned CSV status is not proof that an entry has no code. Skirmish's historical 117-row work queue includes S002–S004 and remains an ordinal/history document, not this task's remaining-code count. Publication, acceptance and evidence fields remain unchanged by this planning update.

Preparation produces qualified physical sources, not mode-specific gameplay. Every implementation pins the physical manifest/hash and adds its own logical bounds, anchors, routes, objectives, camera/minimap and session ownership. Preserve existing logical IDs and saved identities where already specified; use new independent physical-source identities for genuinely changed geometry. No physical runtime IDs or GUIDs are invented here: assign them through the preparation manifest and mode authoring pipeline.

Do not edit the frozen dense-city source or redirect its existing consumers. Urban reuse means a compatible existing source plus independently owned new layout/overlays, or a new derivative source when physical geometry changes. Never mutate the shared world underneath already coded missions. Demo 2 asset/module sourcing remains complementary; it does not override source preparation or create gameplay merely through art.

## Campaign — all seven future missions

| Mission | Planned source / sector | New geometry and mission requirements |
|---|---|---|
| CH04-M04 Grounded Signal | Prepared CityEdgeAirfield derivative: runway, service relay, extraction apron | Continuous operational runway and clear aircraft approach; specialist insertion/drop and recovery areas; military relay distinct from protected civilian airfield; ground-accessible exit and preserved transport-choice/fallback contract. Static aircraft/wrecks cannot occupy active clearance. |
| CH04-M05 Armor Break | Prepared Frontier derivative: bounded refinery-to-airfield military sector | Authored command compound, heavy-asset approaches, Fuel staging, air/ground defense and a protected relief corridor. Use a measured sector, not the entire theatre by default. Requires Frontier device gate; do not introduce new mechanics in the finale. |
| CH05-M01 Citywide Alert | Urban source derivative: two civic districts | Two connected fronts protecting distinct services, readable travel/warnings and separate reserve/production access. Author clinic/power-or-water landmarks and safe reinforcement routes. Frontier's industrial acreage is not a substitute for civic district identity. |
| CH05-M02 Trust Under Fire | Prepared AshLinePort derivative: populated quay edge and city approaches | Two shelter/evacuation routes, safe vehicle boarding, verified broadcast-source compound and civilian exclusion areas. Add shelters/residential context; do not turn evacuation into port capture. Bridges remain traversable land infrastructure, no naval requirement. |
| CH05-M03 Network Collapse | Urban source derivative: civic communications/evidence district | Several verified nodes with distinct approaches, evidence-team access and protected structures; long-range-safe/unsafe geometry consistent with current targeting controls. Preserve node ordering and audit recovery; generic tank farms cannot replace the evidence district. |
| CH05-M04 Last Corridor | Prepared Frontier derivative: bounded logistics-to-city corridor | Connected supply origins, alternative ground delivery route, city-center destination, custody of authority keys and certified optional air delivery. Add the missing civic receiving point. Measure convoy time/Fuel against existing mission limits, keeping playable area and cameras bounded. |
| CH05-M05 Command Node | New Civic Relay complex in an urban source derivative | Recognizable Relay perimeter/core, controlled breach approaches, evidence and specialist access, simultaneous protected city-service interfaces. Reuse qualified modules but author this landmark and route graph; none of the four prototypes supplies the final complex as-is. |

Frontier is conditional, not a release-critical excuse to overrun device budgets. If it cannot qualify, author an independently versioned smaller source for Armor Break or Last Corridor from the prepared industrial/airfield/urban modules with the same objective/route contract. Record the replacement and revalidate; do not silently use a medium-map screenshot as equivalent geometry. Grounded Signal can use its documented runway-unload/APC fallback only under the existing readiness contract, not as an unreported simplification.

Mission identities, story clues, resource budgets, Support unlock/first-use schedule and no-new-mechanic finale rules remain in the Campaign catalog and chapter authorities. These choices are map planning, not permission to change story or current approved command UX.

## Skirmish — five logical maps remain five

Preserve five maps × four objectives × three army profiles × two starts = 120 stable entries. AshLinePort and Frontier are not added as sixth/seventh catalog maps. They may supply qualified modules or later explicitly planned content, but do not increase this catalog's count.

| Map | Future S-IDs | Physical-source plan | Required work beyond prototype |
|---|---|---|---|
| DB Desert Base | S005–S024 | Existing desert source; independent expanded layout/derivative as geometry requires | Keep open-desert/highway identity, two buildable bases and three approaches. Preserve S001–S004 source/version. Do not replace it with a dense industrial layout. |
| CC City Crossroads | S026–S048 | Existing urban source; independent north/south layout/derivative | Retain boulevard/service/courtyard choices, fix floating shelf on derivative, measure curb/vehicle clearance; preserve S025. |
| MP Mountain Pass | S049–S072 | Dedicated new mountain terrain source with qualified existing modules | Two valleys, central pass, genuine heavy-vehicle bypass and separate infantry route. None of the new prototypes satisfies this contract; a Frontier ridge crop is not sufficient. |
| IB Industrial Basin | S074–S096 | Prepared RefineryDistrict-derived expanded industrial source | Two northwest/southeast base yards, freight spine, independent service ring/warehouse flank, truck turning bays, supply expansions and required air infrastructure. Preserve S073's existing source/version. |
| AP Airfield Plains | S097–S120 | Prepared CityEdgeAirfield-derived expanded plains source | Two opposing operational runway compounds, three ground approaches, separated logistics/air returns and ground-capturable objectives. The current single airfield prototype does not satisfy this automatically. |

Existing envelope targets remain DB 600×420, CC approximately 400×615, MP 650×550, IB 700×550 and AP 850×650 usable metres, subject to measured navigation. Refinery/Airfield prototypes have only 600×400 declared playable areas; intentionally author larger connected derivatives and rebake all metadata before certification. Their outer world dressing is not automatically usable expansion space. Respect the 2048×1024 runtime grid and explicit source transforms; do not enlarge just a camera rectangle or scale units/buildings to fit.

The five maps still need twenty total objective layouts across BA/FC/BT/CE, four per map:

- BA: designated original bases and direct/flank assaults; building destruction must preserve identity-based victory and recoverable paths.
- FC: three distinct ground-reachable zones with usable infantry footprints and balanced travel; visual landmarks do not supply capture logic.
- BT: two genuinely independent corridors and the designated survivors' exit; no mandatory route through impassable rubble or an air-only shortcut.
- CE: two legal truck routes, staging/holding/repair locations and final dwell space; both alternatives cannot depend on one chokepoint. No decorative truck may substitute for an objective truck.

Preserve roster, starting packages, force caps, setup numbers, objective semantics and device-gated sizes. A prepared map or a shared ground match does not qualify an Air Mobile/Combined Arms configuration. Test runway/taxi/return and peak entity counts for each exposed size. Do not borrow Campaign small-squad performance evidence for hundreds of units.

Implementation owner: SK-11 consumes physical preparation and authors mode-specific layouts; SK-13 certifies each future entry. The DB shared gameplay slice priority remains. IB/AP preparation can proceed independently without moving already coded scenarios. The generator at `Design/Roadmap/Skirmish_Expansion/Tools/generate_handoff.py` owns packet map-planning text; use `--packets-only` to preserve catalog/setup/publication history.

## Operations — six district layouts, 57 future missions

| District / future IDs | Planned physical source | Required district authoring |
|---|---|---|
| D01 Old Quarter, O004–O010 | Existing urban source, compatible district extension/derivative | Preserve O001–O003 binding and clinic/courtyard geography; author archive, market gate, cargo route, two loops and rescue/safe exit for future entries. |
| D02 Civic Center, O011–O020 | New civic layout derived from qualified urban modules | Plaza, clinic/hospital, records/service annex, separate shelter/vehicle routes and two hold areas; O018 needs an intentional elevated landing pad with a certified ground ramp, not arbitrary rooftop navigation. |
| D03 Industrial Belt, O021–O030 | Prepared RefineryDistrict derivative | Freight spine plus ring road, three separated repair sites, two worker pockets, office/records and depot breach. Do not inherit Campaign production or Oil-chain requirements. |
| D04 River Crossing, O031–O040 | Prepared AshLinePort derivative | Two independent land crossings, near/far approaches, protected aid depot, quayside shelter and routes. Rotate/adapt physical layout if necessary and bind west/east role aliases explicitly; do not rename graphs around prototype compass directions. |
| D05 Highland Approach, O041–O050 | Dedicated highland source; reuse qualified Mountain Pass terrain/modules when available | Switchback, longer truck-safe route, grounded overlooks, aid post, relay compound, landing zone and two ground exits. No dependency on completion of all MP Skirmish scenarios. |
| D06 Airport Perimeter, O051–O060 | Prepared CityEdgeAirfield derivative | Perimeter/service routes, terminal service, hangar breach, separate north/south LZs, boarding areas and ground/air exits. O053 repairs service entities; it does not construct a runway mesh. |

District maps keep their existing planned logical IDs in the catalog (`opmap.operations.*`). Bind qualified physical sources independently. Reusing Campaign/Skirmish geometry does not reuse mission state, resource owners, garrisons, rewards or current session entities. Preserve finite force/enemy packages and named protected entities; use one owner per functional structure or mission socket.

Every future district brief retains its full graph, Partial/Defeat/Withdraw semantics, deadlines, Materials and strategic consequences. Add exact role-to-anchor manifests for its named sites and routes. Existing graphs already distinguish mission targets; do not collapse three scan/repair sites onto a single decorative prop. No naval travel, physical bridge collapse, free rooftop climbing, environmental chain explosions or new production system follows from these maps.

P6 owns district authoring; P2 owns shared tactical rule compatibility. They consume prepared sources directly and need not wait for Skirmish to accept all 120 scenarios. Preserve B12/B30/B60 membership/order and O001–O003's free-intro/full-theater semantics. A physical-source version must be in checkpoint/launch evidence so a changed map cannot reinterpret saved positions; city consequences commit exactly once regardless of asset reuse.

## Common implementation and acceptance amendment

For each future entry, its implementation plan must record:

1. Planned logical map, selected physical manifest/hash, derivative requirements and any unresolved source gate. Proposed map names are not working Addressables references.
2. Bounds/coordinate conversion, typed anchors and routes, surface/blocker/destruction policy, protected areas and runtime owner of every interactive building/vehicle. Preview/minimap and gameplay use the same version.
3. Map-dependent travel, ranges, warning lead time, transport clearance and performance measured with the entry's actual forces. Preserve authored numeric contracts; propose explicit evidence-backed revisions rather than silently changing budgets to fit art.
4. Preparation checks plus native visual review and complete normal-input manual/ARIA journeys, result/return, recovery and separate real-player/device acceptance. Shared source qualification is necessary but not sufficient for any entry.
5. Exact code/config/map hashes, full wrapper logs, pass markers, failed attempts and supported locale/seed/difficulty/size. No readiness or publication changes from planning alone.

Follow AGENTS.md and the preparation handoff for Unity execution and visual-direction review. Retain familiar typography/portraits/controls and visible ARIA Play/Stop; substantial mission UI changes need the required reference-based mockup review before implementation. Current product and optional Support contracts remain in force across modes.

Keep catalog CSV schemas, numerical setup matrices, runtime assets, publication flags and historical evidence unchanged in this documentation update. Later implementations add versioned physical binding data through existing builders. If content is coded after this checkpoint, move that entry into the existing-content re-audit scope before changing its binding.

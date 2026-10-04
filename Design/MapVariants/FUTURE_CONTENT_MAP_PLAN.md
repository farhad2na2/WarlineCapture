# Future content map plan — Campaign, Skirmish and Operations

## Existing Campaign map reuse — owner direction 2026-10-03

For **all Skirmish and Operations mission assignments**, the [Campaign map reuse policy](CAMPAIGN_MAP_REUSE_POLICY.md) now owns physical source selection. Reuse the 11 existing Campaign sources in its inventory; no new map design, enlarged physical derivatives, mountain/highland terrain, road networks or runway compounds. Create mission-specific logical metadata and gameplay placements on existing geometry only. Preserve coded bindings/evidence and stable catalog IDs/counts.

This direction supersedes the September 29 map-authoring instructions for these modes, including their old numeric map envelopes and the exclusion that could leave coded entries governed by older map-building rules. It does not rebind existing content or alter Campaign rework scope. Old scope counts/checkpoints below remain historical.

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

Preparation produces qualified physical sources, not mode-specific gameplay. Every implementation pins the physical manifest/hash and adds its own logical bounds, anchors, routes, objectives, camera/minimap and session ownership. Preserve existing logical IDs and saved identities where already specified. Skirmish/Operations reference existing physical identities and hashes; only independently requested Campaign/preparation work may author changed physical geometry under its own contract. No runtime IDs or GUIDs are invented here.

Do not edit the frozen dense-city source or redirect its existing consumers. For Skirmish/Operations, urban reuse means mission-owned logical layouts/overlays on unchanged existing Campaign geometry. New physical derivatives are outside those mission assignments. Never mutate the shared world underneath already coded missions. Demo 2 asset/module sourcing remains complementary; it does not override source preparation or create gameplay merely through art.

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

## Skirmish — five logical groups, current Campaign geometry

Preserve five groups × four objectives × three army profiles × two starts = 120 stable entries. These are 120 scenarios, not 120 physical maps. Preserve coded S001/S025/S073/S002/S003/S004 bindings and evidence; re-audit current runtime state before identifying uncoded work.

| Group / future entries at the historical checkpoint | First existing Campaign source to assess | Mission-owned work |
|---|---|---|
| DB Desert Base, S005–S024 | Shared desert/dense-city world | Bases, spawn reservations and existing highway/flank route bindings; preserve S004's existing airfield binding |
| CC City Crossroads, S026–S048 | Shared city sectors; Citywide Alert / Network Collapse if suitable | North/south logical frame, existing street routes, zones and valid pads; record terrain defects without changing shared geometry |
| MP Mountain Pass, S049–S072 | Existing Armor Break approach sector, or another inventory source | Existing chokepoint and independent truck/infantry alternatives; retain MP ID, adapt mountain/valley prose to actual geography |
| IB Industrial Basin, S074–S096 | Current RefineryDistrict; existing Armor Break sector if capacity requires | Bases/functional facilities on existing yards, current industrial route alternatives; no expanded industrial environment |
| AP Airfield Plains, S097–S120 | Current CityEdgeAirfield / Grounded Signal | Current runway/landing capacity, bases on legal pads, existing ground alternatives; no second runway compound |

These sources are candidates for mission fit, not accepted full-roster/size claims. Other existing Campaign sources in the [inventory](CAMPAIGN_MAP_REUSE_POLICY.md#current-campaign-source-inventory) may be selected after measurement. Do not automatically adopt the separate Frontier prototype or build another derivative.

Old DB 600×420, CC approximately 400×615, MP 650×550, IB 700×550 and AP 850×650 envelope targets are superseded as map-authoring requirements. Pin actual usable source bounds, coordinate conversion and measured transit times. Outer world dressing is not playable space. Never rescale the world or enlarge only cameras to claim capacity.

Keep twenty logical objective overlays across BA/FC/BT/CE on existing geometry:

- BA binds the designated original bases and distinct existing assault choices, preserving identity-based outcomes and recoverable paths.
- FC binds three distinct ground-reachable zones with usable infantry footprints and measured both-side travel.
- BT binds two independent existing corridors and the designated survivors' reachable exit; air is not the mandatory solution.
- CE binds two existing legal truck routes, staging/holding/repair and final dwell space; the alternatives cannot share the same sole chokepoint.

Preserve roster, starting packages, force caps, numeric setup, objective semantics and device-gated sizes. Select another existing Campaign source if a required footprint, route, air facility or army capacity fails. If none fits, record an unresolved source-fit gate and continue unaffected work. Do not remove required forces or capabilities, change budgets, expose unsupported configurations or create geometry to force acceptance.

SK-11 owns logical layouts and source-fit evidence; SK-13 retains per-entry acceptance. The generator at `Design/Roadmap/Skirmish_Expansion/Tools/generate_handoff.py` owns packet source text; use `--packets-only` to preserve catalogs/setup/publication history. No terrain or environment generation follows from that tool or ticket.

## Operations — six district identities, current Campaign geometry

Preserve O001–O060, six district arcs and all B12/B30/B60 membership/order. Preserve the coded introduction's actual bindings and evidence; model fixtures are not shipping readiness. Each district binds existing Campaign physical geometry through its own logical role/route configuration.

| District / future IDs at the historical checkpoint | First existing Campaign source to assess | Mission-owned role/route binding |
|---|---|---|
| D01 Old Quarter, O004–O010 | Existing Old Quarter/shared city source | Clinic, courtyard, archive, cargo, rescue and safe exit on existing connected sectors |
| D02 Civic Center, O011–O020 | Citywide Alert / Command Node / existing civic sectors | Distinct services/shelters/hold areas; O018 requires an existing reachable LZ, no rooftop/ramp construction |
| D03 Industrial Belt, O021–O030 | RefineryDistrict / existing Armor Break logistics sector | Three separated service sites, worker pockets, records and depot roles on current geometry |
| D04 River Crossing, O031–O040 | AshLinePort / Trust Under Fire | Two independent existing land crossings, aid, shelter and near/far approaches; adapt compass aliases to the source |
| D05 Highland Approach, O041–O050 | Existing Armor Break approach sector or another suitable inventory source | Observation/relay/repair/aid and two ground exits; adapt highland/switchback prose, no new mountain terrain |
| D06 Airport Perimeter, O051–O060 | CityEdgeAirfield / Grounded Signal | Existing service/perimeter routes, hangar, LZ/boarding and ground/air exits; O053 repairs entities, not runway mesh |

Keep planned district logical IDs (`opmap.operations.*`) and checkpoint/launch identity separate from the reused physical source. Do not inherit Campaign garrisons, production, resources, rewards or session entities. Each interactive functional structure or mission socket has one authoritative owner.

Preserve full graphs, Partial/Defeat/Withdraw predicates, distinct routes/targets, deadlines, Materials, protected entities, strategic consequences and Support/product policies. Site/compass/landscape labels refer to semantic roles that must be bound to actual existing geography; they do not authorize new civic, port, highland or airfield environments. Record exact role-to-anchor manifests and existing physical versions for save compatibility.

P6 owns logical district configurations and measured source fit; P2 owns shared tactical compatibility. Neither waits for all Skirmishes to be accepted. If no existing Campaign source satisfies a required role/route/capability, record the failed evidence and unresolved gate instead of creating a map or silently changing the graph. New physical map design needs an explicit owner request.

## Common implementation and acceptance amendment

For each future entry, its implementation plan must record:

1. Planned logical map, selected existing physical definition/scene/hash, logical overlay requirements and any unresolved source-fit gate. Proposed map names are not working Addressables references.
2. Bounds/coordinate conversion, typed anchors and routes, surface/blocker/destruction policy, protected areas and runtime owner of every interactive building/vehicle. Preview/minimap and gameplay use the same version.
3. Map-dependent travel, ranges, warning lead time, transport clearance and performance measured with the entry's actual forces. Preserve authored numeric contracts; propose explicit evidence-backed revisions rather than silently changing budgets to fit art.
4. Preparation checks plus native visual review and complete normal-input manual/ARIA journeys, result/return, recovery and separate real-player/device acceptance. Shared source qualification is necessary but not sufficient for any entry.
5. Exact code/config/map hashes, full wrapper logs, pass markers, failed attempts and supported locale/seed/difficulty/size. No readiness or publication changes from planning alone.

Follow AGENTS.md and the preparation handoff for Unity execution and visual-direction review. Retain familiar typography/portraits/controls and visible ARIA Play/Stop; substantial mission UI changes need the required reference-based mockup review before implementation. Current product and optional Support contracts remain in force across modes.

Keep catalog CSV schemas, numerical setup matrices, runtime assets, publication flags and historical evidence unchanged in this documentation update. Later implementations add versioned physical binding data through existing builders. If content is coded after this checkpoint, move that entry into the existing-content re-audit scope before changing its binding.

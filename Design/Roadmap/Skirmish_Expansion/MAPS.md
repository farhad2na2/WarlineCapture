# Five Skirmish logical battlefield briefs

## Existing Campaign maps only — 2026-10-03

Follow the [Campaign map reuse policy](../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md). All 120 scenarios reuse current Campaign physical maps. Five logical groups and twenty objective overlays provide variety without new environment design. This supersedes older dedicated mountain terrain, enlarged industrial/airfield derivatives, paired-runway compounds and fixed map-envelope authoring requirements. Preserve coded S001/S025/S073/S002/S003/S004 bindings and evidence.

Use [MAP_IMPLEMENTATION](MAP_IMPLEMENTATION.md), the [battle packets](Scenarios/README.md) and [future content plan](../../MapVariants/FUTURE_CONTENT_MAP_PLAN.md). Source choices below are first candidates to measure, not claims of full-roster, size or device readiness. Another source in the existing Campaign inventory can be selected when it better fits. No new physical source is authorized by a map ticket.

## Shared mission layout contract

Bind two legal base/deployment areas, producer/delivery exits, meaningful assault alternatives, two legal convoy routes, two breakthrough corridors, three capture zones, contested supply sites and the air-return/landing infrastructure required by the army profile. These are logical roles and reservations on existing geometry. Base/route/objective aliases resolve through typed assets, not hard-coded AI/ARIA coordinates. Public perception receives only observable route information.

Place required scenario-owned functional facilities and forces on existing legal pads using certified assets, without duplicating scenery owners. Do not manufacture a scenery compound, road opening, berm, bridge, runway or terrain shelf to create capacity. Build pads exclude roads/sidewalks, slopes, water, blocked decoration and reserved objectives/exits. Gates use existing legal openings. Validate full rotated footprints, largest enabled vehicle turns, aircraft clearance and actual connected ground access.

Standard, War and Large War keep stable logical layout identity with measured spawn/staging reservations on the selected source. All force/cost/cap/deadline inputs remain in [MATCH_SETUP](MATCH_SETUP.md); they cannot be reduced silently to fit a smaller map. Existing numeric envelope suggestions are superseded: record actual usable bounds and travel times at normal unit speeds. Never scale the world or enlarge only camera bounds to claim more space.

Two vehicles must pass on main routes, or a tested queue/bypass must exist. Convoy alternatives cannot both depend on the same sole chokepoint. Wreck recovery must be real; no teleports or silent deletion of blockers. Every required ground objective remains reachable without owning aircraft.

## DB — Desert Base

**Source:** existing Campaign desert/dense-city world for future DB layouts. Preserve the current S004 CityEdgeAirfield binding and every other coded entry.

**Tactical identity:** opposing bases connected by exposed direct movement and safer existing flanks. Choose legal pads off the current highway and suitable logistics/producer clearances; no additional highway, rear runway or expanded derivative.

**Objective aliases:** BA designated bases with direct/flank assaults; FC central and two side capture areas; BT two independent existing corridors and far-side exit; CE depot-to-destination on two current truck-safe routes. Highway/north-ruins/south-sweep labels are semantic candidates to fit actual geography, not instructions to construct landmarks.

**Roles and evidence:** infantry covers armor and objectives, scouts observe exposed approaches, APC/transport supports legal movement and siege remains vulnerable to flanks. Verify original-base ownership, producer exits, gate footprint, destruction recovery and fair both-side travel. Record terrain defects; use another valid sector/source rather than repairing shared geometry under this assignment.

## CC — City Crossroads

**Source:** existing shared Campaign city sectors; assess Citywide Alert or Network Collapse if a future layout fits them better. Preserve S025's current north/south binding.

**Tactical identity:** existing exposed street route, vehicle-compatible service alternative and infantry-compatible covered route. Bind bases and deliveries to legal existing yards; no new courtyard openings, city extension or airport approach.

**Objective aliases:** BA street/flank assault; FC three ground-reachable street/service zones; BT two current corridor choices and safe exit; CE two existing truck routes between supply sites. Compass directions may adapt to the physical frame; save final transforms in logical assets.

**Open defect:** the historical northern-base floating shelf remains a failed visual/geometry observation, not accepted terrain. Avoid the affected area or select another existing Campaign source. Mission implementation does not authorize terrain/support repair; separately authorized source maintenance must retain its own regression evidence. Inspect ground/surface/shadow agreement and vehicle corner clearance on the selected sector.

## MP — Mountain Pass (stable internal group ID)

**Source:** assess the already implemented Campaign Armor Break approach sector, or another existing inventory source with the required independent routes. No dedicated mountain terrain or new Frontier derivative.

**Tactical identity:** restrictive approach with a meaningful broad ground route and an independent alternative. Adapt mountain, valley, ridge, lookout and pass copy to the actual existing terrain. The internal MP ID preserves catalog/save identity; it does not promise two newly constructed valleys.

**Objective aliases:** BA approach plus flank; FC three distinct accessible observation/control sites; BT two independent existing ground corridors and exit; CE two actual truck-safe routes between supply roles. Keep infantry-only shortcuts separate from heavy-vehicle routes where the existing source supports them. Do not advertise elevations or ridgeline occlusion absent from the source.

**Roles and evidence:** gunners/anti-armor cover constrained routes; recon, air transport and siege are optional tactical tools as permitted by the unchanged army profile. Verify wreck bypass/queue behavior, ground completion and aircraft/source clearance. If no existing source accommodates a required configuration, leave its source-fit gate open; do not build mountains or drop required capabilities.

## IB — Industrial Basin

**Source:** existing Campaign RefineryDistrict; assess the existing Armor Break logistics sector when more usable capacity is needed. Preserve coded S073's shared-city source.

**Tactical identity:** existing industrial yards and freight/service alternatives with exposed supply access. Bind bases, production and expansions to legal current pads; no added industrial district, ring road, railway crossing, runway or 700×550 enlargement.

**Objective aliases:** BA designated base assault and supply pressure; FC three separated industrial control zones; BT two existing approach corridors; CE two legal supply routes. Rail/refinery/warehouse names must correspond to actual landmarks or be adapted in copy.

**Roles and evidence:** combined ground forces need infantry screening and usable sightlines; logistics uses the existing economy contract. Decorative pipes/rail cannot block required exits, and scenery is not automatically a destructible mission target. Validate legal routes, heaviest footprints, ownership and recovery; no new chain-explosion mechanic.

## AP — Airfield Plains

**Source:** current Campaign CityEdgeAirfield or Grounded Signal. Use current runway, apron and landing infrastructure; do not add a second runway compound or enlarge the airfield to 850×650.

**Tactical identity:** current airfield/perimeter ground routes and certified air access. Place opposing scenario bases on valid existing pads. Measure whether the source can support both factions' required production, sortie, return/refuel and logistics access without unfair infrastructure monopolies.

**Objective aliases:** BA original bases with ground assault choices; FC three ground-capturable sites; BT two existing perimeter corridors and far-side exit; CE two current truck routes separated from active air movements. Existing landmark labels and west/east orientation adapt to actual source geography.

**Roles and evidence:** aircraft, radar, AA, vehicles and infantry keep their existing capability/counter contracts. Certify the largest allowed aircraft, taxi/landing/return queues, ground access, fuel recovery, mixed-domain commands and selection visibility. A single current runway is not automatically sufficient for the full matrix; try another existing Campaign source or keep the unsupported configuration gated. No aircraft capability may be removed silently to declare a mission accepted.

## Source fit and acceptance for every group

Submit the selected existing definition path, scene/hash, physical bounds, coordinate frame, source-fit evidence and all four logical objective overlays. Inspect bases/routes/objectives at minimum/maximum zoom with full camera movement and EN/FA HUD safe areas. Record terrain joins, surface/navigation agreement, shadow defects, occlusion, existing ramps/crossings, producer exits and wreck behavior.

Drive actual squads, largest enabled vehicles, transport and logistics both ways. Test the entry's real force census and peak queues; Campaign small-squad evidence cannot qualify hundreds of units. Preserve source geometry/hash and measure native runtime/packed parity. Logical setup or overlay changes still require affected scenario checks.

If no existing source fits, retain failed evidence and explicitly record the pending gate. Continue unaffected work; new physical map design requires an explicit owner request. Native visual review, full normal-input manual/ARIA journeys, result/return, recovery and real player/device acceptance remain separate gates. Planning text, metadata validation and screenshots alone do not publish an S-ID.

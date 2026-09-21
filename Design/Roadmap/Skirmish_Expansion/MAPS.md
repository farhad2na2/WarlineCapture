# Five Skirmish battlefield briefs

Planning specification, updated 2026-09-21. Each battlefield hosts 24 candidate scenarios from [BATTLE_CATALOG.md](BATTLE_CATALOG.md). Dimensions and travel times below are authoring targets requiring real navigation measurements. These are not claims about current baked geometry. DB/CC reuse the existing environment, and IB prototype map assets/wiring are now present in the working tree; none certifies the expanded layouts. MP/AP remain new authoring work. A logical battlefield may reuse a shared scene region only if its complete playable geometry meets this brief.

Use [MAP_IMPLEMENTATION](MAP_IMPLEMENTATION.md) for exact proposed builders, typed anchors/routes, normalized placement candidates, five map IDs, twenty objective layouts, defender allocation and geometry tests. The [battle packets](Scenarios/README.md) bind every S-ID to those layouts. Geometry adjustments must be measured in the real map; planning coordinates are not baked-navigation evidence.

## Shared map contract

Art sourcing: the [Demo 2 reuse plan](../../Demo2_Asset_Reuse_Plan.md) selects logistics/utility modules for IB, service-yard accents for CC, perimeter/comms dressing for AP, small checkpoints for MP and sparse props for DB. Retain desert identity and all route/clearance contracts below. Source models are candidates until adapted and validated; do not copy the demo scene or use a new art skin to claim another map. IB is the first candidate pilot, with no alteration required to existing prototypes.

Every map provides two expandable base pads, connected deployment pockets, independent production/delivery exits, three combat approaches, two legal convoy routes, two breakthrough corridors, three capture zones, two contested supply expansion sites and safe air-return/landing spaces. Base and objective anchors have named variants for each objective, not absolute coordinates in AI/ARIA code. A map owns a semantic route graph with usable width, surface type, travel cost, cover, danger exposure and vehicle/air compatibility; only public/observed attributes reach player perception.

Road carriageways stay clear of ordinary buildings. Buildable pads exclude sidewalks, slopes, mountains, water, impassable decoration and reserved exits/objectives. Gates use allowed carriageways or actual wall openings and retain their preview transform. Airfield runways and clearance corridors use measured bounds of the largest allowed aircraft plus a validated safety margin. No plane option ships on a map without its required infrastructure; redesign the pad or block that scenario until it is supported.

Standard, War and Large War use the same connected map identity with separately authored staging/expansion reservations and tuned transit times. Never enlarge only the camera rectangle to claim a larger battlefield. Paths must accommodate the biggest enabled ground platform. Two vehicles must be able to pass on main routes; single-file sections require a tested bypass/queue behavior. Convoy alternatives may not both depend on the same choke. Emergency recovery must not teleport units or secretly destroy blockers.

## DB — Desert Base

- **Identity:** expanded version of the existing west/east Base Assault. Broad open desert with a highway, low ruins and dispersed cover. Best entry point for mixed ground tactics, not a permanent rifle-only map.
- **Envelope target:** roughly 600 × 420 usable metres, subject to shared-scene placement. Infantry first contact 45–75 seconds from Field Base; slower flank 75–105 seconds. Scale staging for 116/224/340-unit limits before exposing them.
- **Bases:** west and east on flat pads off the highway. Each has a rear logistics/air strip and side exit so deliveries do not cross the assault route. Supply expansions are southwest and northeast; avoid free protected income immediately beside both HQs.
- **Routes:** direct highway for speed but little cover; northern ruin route for infantry/APCs; southern open sweep for vehicles, vulnerable to scouts/air. A berm breaks direct base-to-base fire, without making a floating terrain shelf.
- **BA:** attack either highway access or flank the designated Barracks. **FC:** central crossroads, north ruins, south supply junction. **BT:** choose north gate corridor or south checkpoint; exit behind the east base. **CE:** west depot to east evacuation pad via highway or southern service road.
- **Roles:** gunners cover roads, rocketeers punish exposed armor, tanks need infantry through ruins, transports shorten the safe flank, radar/drone spot open-ground movement, siege risks counter-flanking.
- **Quality gates:** preserve campaign anchors; verify existing gate placement and full rotated footprints; confirm surface visuals match collision. No tree/rock dressing inside authored producer clearance.

## CC — City Crossroads

- **Identity:** existing Skirmish 2's north/south approach through the city, expanded with deliberate side routes. Player stages north, enemy south. Retain the distinct orientation without reusing DB's west/east strategy coordinates.
- **Envelope target:** existing logical window is approximately 400 × 615 metres; first attempt to satisfy the brief inside it. Widen only after scene/navigation review. Field first contact 55–85 seconds; viable flank 85–120 seconds.
- **Bases:** clear north/south yards with rear/side expansion pads and airport approaches outside the dense blocks. No starting barracks on highway or sidewalk. Helicopter deliveries land on clear ground, do not pin the camera, and cannot overlap the public command area.
- **Routes:** central boulevard is short and exposed; east service lane favors armored flanking; west courtyards favor infantry and transports. Add connected openings so decorative walls do not produce traps. Streets support actual vehicle turning radii.
- **BA:** pressure the central approach or cut around either flank. **FC:** market crossing, eastern depot entrance, western civic square. **BT:** clear boulevard barricade or east service checkpoint, then evacuate south. **CE:** northern supply yard to southern yard via boulevard or east ring road.
- **Roles:** breachers and infantry clear defended street corners; APCs move exposed squads; heavy armor controls avenues but cannot bypass every courtyard; helicopters offer a risky shortcut against mobile anti-air; scouts distinguish occupied from empty approaches.
- **Immediate open defect:** inspection found thin ground tiles near the northern base/civic scenery around height 5.85 m with exposed edges above surrounding ground, creating a floating shelf/shadow. Correct the authored terrain transition/support and verify surface traversal/collision together. Do not merely hide the shadow. This is an open visual defect; the previous four ARIA victories do not close it.
- **Quality gates:** inspect low/high camera pitch and all route sides for terrain seams; check infantry/vehicle access at every height transition. Repeat legal placement, delivery, two-way traffic and public-map focus checks after correction.

## MP — Mountain Pass

- **Identity:** two valleys separated by a rocky ridge, with a central pass and two meaningful alternatives. Terrain is restrictive but cannot reduce every battle to an immovable doorway.
- **Envelope target:** roughly 650 × 550 metres. Field first contact 60–90 seconds, longer ground flank 90–135 seconds. Use ridgeline separation for air/recon value, not arbitrary path length padding.
- **Bases:** southwest and northeast valley floors, with buildable terraces connected to logistics roads. Runway pads lie along the outer valley axes; helipads and helicopter drop sites sit clear of cliff geometry.
- **Routes:** central wide pass; western winding vehicle bypass with passing bays; eastern higher infantry route with a separate transport landing pocket. CE uses pass or western bypass; do not send trucks down the infantry-only trail.
- **BA:** threaten the pass while maneuvering around a flank. **FC:** pass junction, west turnout, east lookout reachable by infantry from both sides. **BT:** two distinct ridge crossings unlock the far valley exit. **CE:** move trucks between valley depots, using scouting to choose a route.
- **Roles:** marksmen and gunners support chokepoints, rocketeers threaten tanks in predictable corridors, transport helicopters bypass travel distance but face anti-air, radar/drone improve early warning, siege forces defenders to move.
- **Quality gates:** no mountains containing buildable cells; no aircraft clipping ridges or selecting ground through mountains; fallback route after a wreck; no cliff-top capture point unreachable without a particular aircraft.

## IB — Industrial Basin

- **Identity:** refineries, warehouses, rail/service corridors and exposed supply infrastructure. Economy and protected advances matter as much as raw unit count.
- **Envelope target:** roughly 700 × 550 metres. Field first contact 50–85 seconds; alternate industrial ring 85–120 seconds. Build density must fit the rendering budget without hiding small troops.
- **Bases:** northwest and southeast industrial yards, each with a separated truck depot, production yard and rear runway strip. Expansion supply sites on opposite sides create a choice between secure income and pressure.
- **Routes:** central freight avenue for heavy armor; north/east service ring for supply raids; south/west warehouse lane for infantry and APCs. Rail dressing has explicit passable crossings, not invisible continuous blockers.
- **BA:** isolate supply or breach the main yard. **FC:** rail junction, refinery entrance, warehouse square. **BT:** clear either freight gate or service-ring control point to open the southeast exit. **CE:** deliver trucks between storage terminals along freight avenue or service ring.
- **Roles:** tanks, heavy APCs and siege are useful with infantry protection; anti-armor infantry exploits limited sightlines; repair/supply roles only count once real recovery mechanics exist. Fuel loss creates readable operational limits rather than immobilizing the entire army without recourse.
- **Quality gates:** no decorative pipes blocking all exits, no indestructible scenery advertised as a target, no accidental chain-explosion mechanic. Industrial hazards remain cosmetic unless explicitly designed and exposed in a later rules version.

## AP — Airfield Plains

- **Identity:** broad plains, two airfield bases, dispersed low structures and contested forward landing grounds. Designed for full combined arms, while G variants remain winnable without offensive aircraft.
- **Envelope target:** roughly 850 × 650 metres. Field first contact 60–90 seconds; long vehicle sweep 90–135 seconds. Validate strategic distance at normal unit speeds; do not force a five-minute empty march.
- **Bases:** west/east runway compounds, wide clear approaches and separated runway, ground recruitment and logistics exits. Both have ground-accessible supply and repair/refuel capacity. Airfield destruction must leave a visible recovery plan.
- **Routes:** middle logistics highway; northern dispersed-cover line; southern broad armored sweep. Forward landing sites are optional advantages, never the only route for a ground army.
- **BA:** combine ground pressure with defended air sorties. **FC:** central weather station, north depot, south landing-zone entrance, all capturable by ground infantry. **BT:** open north radar-road or south apron corridor then evacuate across the far perimeter. **CE:** protect a fuel convoy on highway or southern ring road with ground/air escort.
- **Roles:** fighters counter hostile aircraft, strike jets attack exposed ground targets, mobile anti-air forces route planning, radar/drone create useful warning, tanks and infantry remain necessary for objectives. A plane icon alone is not a validated sortie system.
- **Quality gates:** aircraft never occupy UI selection layers or disappear under terrain; jet passes, fuel return, runway queues, carrier loss, mixed-domain orders and large visible battles pass actual runtime tests.

## Geometry acceptance for every map

Inspect both base pads and every route/objective at minimum/maximum zoom, rotating the camera, in EN/FA HUD safe areas. Record physical terrain joins, terrain-to-navigation agreement, shadow artifacts, projectile occlusion, bridges/ramps if any, producer exits and wreck behavior. Building pads must include the model and all entrance clearance, not just the icon footprint. No ground panel can hover without visible supporting terrain or an intentional bridge structure.

A full map review includes walking/dragging the camera as a human would, plus real squads, heaviest vehicles, transports and logistics driving both ways. Map-surface audits complement visual review; they do not replace it. After changed geometry, rebuild the approved navigation/surface metadata and revalidate runtime map binding, source/content hashes and affected campaign regions.

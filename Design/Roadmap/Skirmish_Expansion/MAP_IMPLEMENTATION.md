# Map, anchor and route implementation packets

2026-09-21. Supplements the five authored briefs in [MAPS](MAPS.md). These are authoring targets; this task did not generate geometry or run navigation. Use Unity Editor APIs/checked builders and the Unity CLI skill when implementing; preserve current map-source/presentation/addressable contracts and Campaign anchors.

## Existing vs new map identities

| Code | Expanded logical map ID | Observed source / required action |
|---|---|---|
| DB | `opmap.skirmish.desert_base_01` | Existing shared Desert Base; expand its certified layout, keeping legacy snapshot/regression variant |
| CC | `opmap.skirmish.city_crossroads` | Existing `Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset` with source binding to Desert Base; certify the distinct north/south logical region and repair the documented floating shelf |
| MP | `opmap.skirmish.mountain_pass` | Proposed new map definition/layout; author two valleys, pass and vehicle bypass before accepting any MP entry |
| IB | `opmap.skirmish.industrial_basin` | In-progress `Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset` with shared source binding; preserve owner work and certify expanded industrial route geometry |
| AP | `opmap.skirmish.airfield_plains` | Proposed new map definition/layout; two runway compounds and ground-accessible objectives required |

Asset paths in this table are relative to `Assets/Game/`. An existing logical/source binding is not proof the full target layout is built. If a physical/logical definition is reused unchanged, retain its stable ID; scenario army/start/size changes belong in layout/setup references. Do not publish a new map by relabeling the same inaccessible region.

## New layout schema and files

`SkirmishMapLayoutConfig` contains map ID/version, bounds/grid/content hashes, local authoring frame, typed anchors, route graph, build pads and clearance footprints, objective geometry, initial force/garrison slots and size staging variants. Use `OperationMapDefinition` and `ScenarioSetupConfig.RequiredAnchors` for existing shared binding; add typed extra metadata rather than a new scene loader.

Files: `Assets/Game/Configs/SkirmishExpansion/Maps/<DB|CC|MP|IB|AP>/SkirmishMapLayout_<code>.asset`; `SkirmishLayout_<code>_<BA|FC|BT|CE>.asset` for objective overlays, plus named Standard/War/Large staging arrays inside the config. New Editor owner `SkirmishMapLayoutBuilder` projects semantic roles into actual grid/world coordinates and validates every source hash. `SkirmishMapLayoutValidation` runs full footprint/path/landing checks for all allowed roster entries.

Anchor IDs: `anchor.skirmish.<lower-map>.<role>`; route IDs: `route.skirmish.<lower-map>.<name>`. Examples `anchor.skirmish.cc.base_player`, `anchor.skirmish.cc.fc_a`, `route.skirmish.cc.convoy_b`. IDs are <=60 ASCII bytes, stable and independent of scene/localized names. A renderer may transform IDs to a marker; AI/ARIA cannot infer positions by parsing the name.

## Shared logical frame and initial placement recipe

Use normalized authoring coordinates `(u,v)`: u runs from player rear to enemy rear; v runs across the battlefield. Map owner pins its world origin, forward/across axes and usable metres to actual `OperationMapDefinition` bounds and saves them. This is Editor layout input, never an AI coordinate shortcut. Orient DB/AP west→east, CC north→south, MP southwest→northeast, IB northwest→southeast. Expected envelopes remain DB 600×420 m, CC roughly 400×615 m, MP 650×550 m, IB 700×550 m, AP 850×650 m, subject to certified terrain.

| Required anchor/area | Initial normalized layout target | Binding / clearance |
|---|---|---|
| `base_player`, `base_enemy` | (.12,.50), (.88,.50) | Designated Barracks on legal flat base pads; no road/sidewalk overlap |
| `staging_player`, `staging_enemy` | (.18,.50), (.82,.50) | Infantry/vehicle spawn arrays on connected ground, not one shared cell |
| `supply_player`, `supply_enemy` | (.10,.24), (.90,.76) | Pump/refinery/depot/storage and separate logistics approach |
| `service_player`, `service_enemy` | (.12,.68), (.88,.32) | Repair service pads beside own Ground Staging; clear of combat spawn exits |
| `air_player`, `air_enemy` | (.07,.78), (.93,.22) | Helipad/drone return slots; runway footprint extends along rear usable axis |
| `supply_expansion_a/b` | (.36,.22), (.64,.78) | Contested extraction expansions outside initial protection |
| `fc_a`, `fc_b`, `fc_c` | (.50,.50), (.43,.25), (.57,.75) | Initially neutral ground zones, 16 m radius candidate; custom valid-ground polygon if needed |
| `bt_a`, `bt_b`, `bt_exit` | (.58,.28), (.58,.72), (.94,.50) | Alternative capture footprints, then ground exit; approach/capacity tested |
| `convoy_origin`, `convoy_destination` | (.08,.42), (.92,.58) | Three parking bays at origin, three-truck delivery footprint at destination |
| `defender_tower_a/b` | (.65,.32), (.65,.68) | BT/CE extra towers cover separate approaches, never origin or both complete alternate routes |

These coordinates are candidate layout controls, **not permission to place inside mountains/buildings**. Builder validates actual footprint/ground connectivity; author moves a failing candidate to the nearest same-zone legal pad while preserving the route decision and travel-time envelope. Record final anchor positions, movement proof and why a deviation was needed. No runtime relocation, teleport or automatic deletion of scenery to make a placement pass. If no legal pad exists, fix that layout's geometry as part of the map ticket.

`main`: staging → near approach → central contact → far approach → enemy staging. `flank_a` and `flank_b` have their own middle waypoints and connect both deployment areas. `convoy_a/b`: origin → own holding pocket → separate central segments → far holding pocket → destination. `bt_route_a/b`: muster → corresponding corridor → far-side escape lane → exit. Every waypoint stores surface/movement masks, width, capacity and cover/exposure tags; graph edges store estimated traversal time for infantry/car/heaviest allowed vehicle and aircraft suitability where relevant.

No ordinary structure can be placed in capture/exit/destination footprints, producer exits, runway/landing reservations, required waypoint clearances or the last open route to a required objective. Gates/walls use designated defense slots; legality validates the whole rotated footprint and reachability before charging. Test wreck occupancy, cancellation and blocker updates rather than deleting a blockage invisibly.

## Twenty map/objective binding rows

| Packet | `main` / `flank_a` / `flank_b` context | Objective aliases and default defended areas |
|---|---|---|
| DB BA | highway / north ruins / south sweep | Enemy Barracks east; supply northeast/southwest; two distinct assault lanes |
| DB FC | same | `fc_a` crossroads, `fc_b` north ruins, `fc_c` south supply junction |
| DB BT | same | `bt_a` north gate, `bt_b` south checkpoint; exit behind east base; two towers one/corridor |
| DB CE | highway / south service road for trucks | Origin west depot, destination east pad; towers beyond highway bend and service junction |
| CC BA | central boulevard / east service / west courtyards | Enemy south yard; vehicle flank east, infantry/transport west |
| CC FC | same | Market crossing / east depot entrance / west civic square |
| CC BT | same | Boulevard barricade / east checkpoint; exit south; no forced truck use through courtyards |
| CC CE | boulevard / east ring for trucks | North supply yard → south yard; east passing/holding pockets |
| MP BA | central pass / west vehicle bypass / east infantry trail | Enemy northeast valley; heavy units need pass/bypass; east is ground-infantry reachable |
| MP FC | same | Pass junction / west turnout / east lookout; all capturable by foot without aircraft |
| MP BT | same | Pass crossing / west bypass crossing; exit northeast valley; east infantry trail can approach either without becoming the only solution |
| MP CE | pass / west bypass for trucks | Southwest depot → northeast depot; passing bays and wreck bypass required |
| IB BA | freight avenue / service ring / warehouse lane | Enemy southeast yard; exposed supplies can be raided through ring |
| IB FC | same | Rail junction / refinery entrance / warehouse square |
| IB BT | same | Freight gate / service-ring control; exit southeast; towers may not block both middle segments |
| IB CE | freight avenue / service ring for trucks | Northwest storage → southeast storage; rail crossings explicitly traversable |
| AP BA | middle highway / north cover / south armored sweep | Enemy east runway compound; ground win remains viable in G |
| AP FC | same | Weather station / north depot / south LZ entrance; aircraft cannot capture |
| AP BT | same | North radar-road / south apron; exit east perimeter; dismount requirement unchanged |
| AP CE | highway / south ring for trucks | West fuel depot → east logistics pad; runway and convoy reservations separated |

Army variants use this graph rather than clone geometry: G exploits ground maneuver and optional unarmed lift; A trades tanks/heavy APC/siege for air mobility/offense; C combines both. Field and Established alter base forces/facilities, not objective locations. Size variants expand legal starting/staging/build reservations, not silently move the deadline/objective rules.

For BT/CE, defender initial whole squads distribute A/B/reserve as 40/40/20 of squads rounded down for A/B, remainder reserve. At least one squad per approach; if a small roster leaves zero reserve, keep it zero and use paid replacements. Vehicles: CAR/APC pair to the broader legal approach and reserve; tanks cover separate lanes if two, otherwise main; AA protects rear/counterattack access before being advanced after observed air. Do not put a Heavy APC on MP's infantry trail. The exact resolved per-entry placement arrays are saved by the compiler and visible in developer reports, but concealed units remain concealed in player UI.

## Definition of a finished map packet

Submit the five-map-specific overview and all four objective overlays, typed ID/anchor manifest, actual per-size spawn/staging/facility placements, two truck routes and two BT routes, every FC approach, certified runway/helicopter routes, placement exclusion masks, minimap/camera framing and EN/FA UI views. Drive actual required units over each route and test the heaviest allowed footprint in both directions. Record first-contact/flank timing against MAPS targets and correct bottlenecks with evidence.

CC's thin elevated ground shelf must pass geometry/surface/collision/shadow review from multiple camera pitches before its expanded entries are accepted. MP cannot rely on aircraft to reach a mandatory point; IB decorative pipes/rails cannot secretly block all exits; AP aircraft must pass taxi/landing/return/selection with full camera movement. Device/render cost uses real composition, not cheaper placeholder meshes. A shared-map source update triggers affected Campaign/legacy geometry regression checks.

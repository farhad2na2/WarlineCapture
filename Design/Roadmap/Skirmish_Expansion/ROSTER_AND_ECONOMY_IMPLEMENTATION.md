# Roster, producers and economy: resolved implementation choices

2026-09-21. Source assets were read, not played. [ROSTER_SOURCE_AUDIT.csv](ROSTER_SOURCE_AUDIT.csv) lists the 51 UnitGrid and 23 BuildingDefinition configs observed on this working tree, their display names, content hashes, proposed disposition and certification requirement. A serialized attack/transport flag is not proof of a working capability.

## Canonical role bindings

Paths below are under `Assets/Game/Configs/Prefabs/`. Each row is an explicit source-art/config candidate for a **mode-owned role definition**, not permission to change shared Campaign asset stats. Resolve the runtime prefab key from the config/registry relationship through Editor APIs; assert its GUID and actual capability in the compiled roster report. Do not derive role from substring at runtime.

| Role ID | Source config filename | Stage / producer | G / A / C |
|---|---|---|---|
| `role.rifle` | `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset` | R1 Barracks; 4-person squad | yes / yes / yes |
| `role.gunner` | `Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset` | R1 Barracks; 4-person squad | yes / yes / yes |
| `role.marksman` | `Prefab_UnitGrid_Chr_Soldier_Female_01_Config.asset` | R1 Barracks; 4-person squad | yes / yes / yes |
| `role.breacher` | `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset` | R1 Barracks; 4-person squad | yes / yes / yes |
| `role.rocketeer` | `Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset` | R1 Barracks; 4-person squad | yes / yes / yes |
| `role.car` | `Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset` | R1 Ground Staging | yes / yes / yes |
| `role.apc_fast` | `Prefab_UnitGrid_Veh_APC_Fast_Config.asset` | R1 Ground Staging | yes / yes / yes |
| `role.apc_armored` | `Prefab_UnitGrid_Veh_APC_Slow_Config.asset` | R1 Ground Staging; starting APC alias | yes / yes / yes |
| `role.apc_heavy` | `Prefab_UnitGrid_Veh_APC_Heavy_Config.asset` | R2 Ground Staging | yes / no / yes |
| `role.tank` | `Prefab_UnitGrid_Veh_Tank_USA_Config.asset` | R2 Ground Staging | yes / no / yes |
| `role.aa` | `Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset` | R1 Ground Staging; no Helipad gate | no / yes / yes |
| `role.radar` | `Prefab_UnitGrid_Veh_Radar_Tank.asset` | R2 Ground Staging + working intel | yes / yes / yes |
| `role.siege` | `Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset` | R3 Ground Staging | yes / no / yes |
| `role.transport_heli` | `Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset` | R2 Helipad | yes / yes / yes |
| `role.attack_heli_light` | `Prefab_UnitGrid_Veh_Helicopter_Attack_Small_Config.asset` | R2 Helipad | no / yes / yes |
| `role.attack_heli` | `Prefab_UnitGrid_Veh_Helicopter_Attack_Config.asset` | R2 Helipad | no / yes / yes |
| `role.drone` | `Prefab_UnitGrid_Veh_Drone_Config.asset` | R2 intel station with drone launch pad | yes / yes / yes |
| `role.fighter` | `Prefab_UnitGrid_Veh_Jet_02_Config.asset` | R3 Airport/runway | no / yes / yes |
| `role.strike` | `Prefab_UnitGrid_Veh_Jet_01_Config.asset` | R3 Airport/runway | no / yes / yes |
| `role.transport_plane` | `Prefab_UnitGrid_Veh_Plane_Transport_Config.asset` | R3 Airport/runway | no / yes / yes |
| `role.logistics_truck` | `Prefab_UnitGrid_Veh_Truck_Tray.asset` | R1 Ground Staging logistics queue; no combat Supply | yes / yes / yes |
| `role.tanker` | `Prefab_UnitGrid_Veh_Truck_Tanker.asset` | R1 Ground Staging logistics queue; no combat Supply | yes / yes / yes |
| `role.convoy_objective` | `Prefab_UnitGrid_Veh_Truck_Tray.asset` | Authored CE-only override; never recruitable | CE only |

Canopy Truck is an alternate certified transport/logistics role variant, not a way to bypass the APC/support cap. Rifle/marksman appearance alternatives remain variants inside a role card. Contractor and irregular appearances can become alternate-faction variants after role-equivalence tests; the first 120 configurations use the same certified role capabilities/costs for both factions. Civilian/pilot/leader/bomb-suit assets stay scenario/Sandbox-only until their actual special behavior is separately specified. Do not add fake disposal, healing or commander buffs to close an asset inventory.

Start with source baseline weapon/health/movement data projected into **Skirmish role overlays** and disable the current uniform-rifle overlay for expanded sessions. Establish actual damage/armor/target-domain distinctions in the first role certification packet; configuration names alone do not qualify. Use `role.breacher` for measured short-range anti-infantry/structure pressure, not an invented instant-open gate ability. Ground missile launcher is the siege role; no new artillery model is required. Only fighter/AA candidates advertise anti-air after shared target-domain tests; unarmed transport/recon roles get no weapon.

## Producer decisions (implementation, not another user question)

The observed Barracks config currently produces one infantry appearance; Helipad has three helicopters; Airport has transport plane, drone, strike and fighter; Expert Tent lists infantry/pilots, **not** a vehicle factory. Expand production via mode-owned catalogs and the shared request/delivery boundary.

| Functional producer | Existing visual/config basis | Exact target work |
|---|---|---|
| Main Barracks / infantry | `Buildings/Building_Barrack.prefab` and `Prefab_BuildingDefinition_Building_Barrack_Config.asset` | Mode-owned five-role squad catalog; explicit queue count; designated base role only on startup HQ |
| Ground Staging / logistics | **New** `Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab`; reuse Barracks/service-yard visual modules and simple pad markings | Build through `SkirmishGroundStagingBuilder`; own `BuildingDefinitionAuthoring` config, one vehicle queue plus separate logistics queue, connected ground spawn slots, rally, selection/portrait, health/destruction and Materials costs. No need to wait for bespoke art. Keep clear vehicle exit and treat model as a staging yard, not a completed existing factory. |
| Helipad | `Buildings/Building_Helipad.prefab` | Three helicopter roles; capacity, landing reservations, transport/attack domain and Fuel/return integration |
| Airport | `Buildings/Building_Airport.prefab` | Fighter/strike/transport-plane roles; tested runway, taxi, attack/return and queue release |
| Intel station | `Buildings/Building_Satelite_Dish.prefab` | Public Scan/detection semantics and a small authored drone pad; move expanded drone production here with a mode-specific producer binding; preserve Campaign Airport behavior |
| Oil Pump | `Buildings/Building_OilPump.prefab` | Real extraction and physical oil haul, typed expansion supply sites |
| Fuel Refinery | `Buildings/Building_Refinery.prefab` | Oil → Fuel with real storage and haul; no Materials output in this role |
| Materials fabrication | `Buildings/Building_Ammunition_Depot.prefab` / display name Field Fabrication Depot | Oil → Materials through existing fabrication owner; do not change its global source config to tune Skirmish |
| Fuel storage | `Buildings/Building_Fuel_Bladder.prefab` | Usable stored Fuel, delivery, capacity and loss through shared logic |

Asset paths in the table are relative to `Assets/Game/Prefabs/` except the new full path. Mode-owned data under `Assets/Game/Configs/SkirmishExpansion/Roster/` binds each source and its overlay; builders validate actual registered source keys. Reuse shared placement, footprint, resource, production and selection owners. A new GameObject is a visual/authoring edge; it contains no runtime production policy.

Field has seven initial backbone structures: main Barracks, Ground Staging, Oil Pump, Fuel Refinery, Fuel Bladder, Field Fabrication Depot, Watchtower. Established adds a second tower, Satellite Dish and Helipad (ten structures), plus expanded infantry queue capacity **on the same starting Barracks**. War/Large Established adds a second refinery (eleven). Objective BT/CE enemy adds two towers (9/12/13 total respectively). Queue counts are not duplicate physical bases. Airport begins unbuilt in every catalog start. Starting structures are reported separately from the player-built structure cap; subsequent additional construction is capped.

Ground Staging has one separate logistics queue at every size/start; the vehicle/infantry queue counts are in INITIAL_SETUP_MATRIX. Its replacements obey the existing prices: 100 M/20 s truck, 140 M/25 s tanker. This is the recovery path for lost hauling without giving a free producer or invisible resources. If all facilities/resources are lost, defeat may be strategically unavoidable; no emergency grant is required.

## Economy reconciliation, explicitly superseding conflicting proposals

The old MATCH_SETUP combined recipe proposed 100 Oil → 300 Materials in 45 s, while also targeting 100 Materials/min and listing separate refinery/depot facilities. Those rates cannot all describe one production line. Use the following **two separate real processing lines** for expanded Skirmish. This is new mode-owned tuning, not a claim about current prefabs. Current fabrication source reads 4 Oil → 20 Materials in 30 s; leave Campaign source data unchanged.

| Size | Pump extraction | Fabricator recipe / 30 s | Materials steady output | Refinery recipe / 30 s | Fuel steady output, one refinery | Oil demand with both lines active |
|---|---:|---|---:|---|---:|---:|
| Standard | 120 Oil/min | 10 Oil → 50 M | 100 M/min | 30 Oil → 60 Fuel | 120 Fuel/min | 80 Oil/min |
| War | 180 Oil/min | 16 Oil → 80 M | 160 M/min | 45 Oil → 90 Fuel | 180 Fuel/min | 122 Oil/min |
| Large War | 240 Oil/min | 22 Oil → 110 M | 220 M/min | 60 Oil → 120 Fuel | 240 Fuel/min | 164 Oil/min |

Each facility processes one batch at a time. Inputs are physically delivered/reserved; output waits safely if storage is full. Material output enters canonical faction Materials through the existing fabrication event; Fuel is usable only after shared storage/delivery semantics say so. No display-only balance, direct Oil-as-currency purchase, account Credits/Command, or enemy-only income multiplier. Supply is capacity, not a wallet resource.

The second War/Large Established refinery adds potential capacity, **not free Oil**: full simultaneous operation can outstrip the starting pump, so visible priority and extra extraction are meaningful. Set default logistics priority to keep one Materials batch funded, then serve Fuel. Do not claim aggregate steady-state output with an empty input store. Added structures use the selected size's declared module capacities for both factions. Measure hauling throughput, actual distance, depot buffers and usable output; a recipe's theoretical rate is not measured income.

Starting stocks/capacities, unit/building prices, production/research timings, Supply/category caps and upgrade effects remain MATCH_SETUP values. INITIAL_SETUP_MATRIX expands every row/size into actual per-side quantities and replaces mental arithmetic. Loaded vehicle Fuel is additional **explicit listed starting endowment**, not silently subtracted from the published usable store; derive each full tank quantity from certified role config and report it per side. Identical role means identical full-tank value at every difficulty. No invented Fuel number is added to a source role before its consumption model exists.

## Role and air certification tasks

For each canonical role, implement/verify this chain: actual producer → price/quantity/queue → connected delivery → group selection → allowed commands → expected combat/transport/recon result → death/capacity release → checkpoint restore → EN/FA portrait/help. Record damage/health/range/speed/fuel, targets/counters, footprint, seats/compatibility, delivery and stat-overlay hash. Common rifle aliases must not overwrite marksman/gunner/rocketeer behavior.

Aircraft share a state model: Queued → Arriving/Taxi → Ready → Ordered/AttackPass/Transport → Returning → Landing/Refueling → Ready; failure/death/cancel transitions have explicit reservations and receipts. Preserve a safe return Fuel margin; lack of usable Fuel produces a refusal/return warning, not frozen invisible aircraft. Jets need a real pass and return loop. Transport planes/helicopters count as tactical air when player-controlled; automatic delivery carriers count only as bounded support carriers. Unit boarding/unloading must remain observable and commandable at dense overlap.

Recon uses the same visibility authority for rendering, selection, target eligibility, minimap, AI and ARIA. Visible contacts can expose live IDs; last-seen contacts carry age/uncertain position only. Radar/drone/Scan do not bypass hidden-target validation. Ground profile supports transport/recon air but no offensive aircraft; it must win all objectives without offensive air. A/C exposes an affordable R1 ground anti-air role before offensive air reaches it. Use existing source fields as inputs, not evidence these contracts already work.

## Build and tuning acceptance

`SkirmishRosterValidation` (proposed) validates role/prefab/config GUID binding, legal profile availability, producer reachability, price/quantity/caps, counters and ability affordances. `SkirmishEconomyValidation` (proposed) runs isolated paid-growth and supply-loss fixtures with physical logistics. Record first counter 45–90 s, first vehicle 1–3 min, Field offensive helicopter 4–7 min, advanced air 8–12 min, viable War force 12–18 min as **targets to test**. Failure triggers narrow shared price/rate/layout tuning with evidence and new config version, not a request for the owner to choose how to code the mechanic.

Use the initial costs/rates above until measurement warrants correction. Adjust one bottleneck at a time; retain both-faction parity and immutable scenario snapshots. Change an objective, faction count, profile availability, catalog count, paid progression rule or legacy save semantics only by an explicit design revision, not a balance tweak. Per-battle packets do not secretly carry separate weapon balance or income bonuses.

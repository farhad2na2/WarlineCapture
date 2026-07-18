# WarlineCapture 3D Single-Map Gameplay Direction

2026-07-10 narrative amendment: `Campaign_Narrative_Bible.md` owns named factions, setting, and character casting. Unique civilian, soldier, contractor, pilot, specialist, and insurgent configs retain consistent faction/story identities.

Date: 2026-05-21
Status: Active source of truth

## Decision

WarlineCapture is returning to a full 3D mobile RTS direction.

The active gameplay target is one large continuous 3D map per operation, with many soldiers, vehicles, aircraft, civilians, hostile cells, defensive structures, civilian buildings, and support buildings present in the same playable world. Planning, intel, minimap, mission briefing, objective focus, threat warnings, and deployment setup are UI layers over that same world. They are not separate strategic and tactical maps.

This supersedes the 2.5D isometric macro-tile direction and the prior strategic-map/tactical-map split for future gameplay and art decisions.

## Product Fantasy

WarlineCapture is a mobile-first 3D command RTS about preparing and executing operations in a fictional Middle Eastern-inspired city where armed terrorist and insurgent cells exploit populated infrastructure, roads, compounds, markets, and district systems.

The player is a field commander preparing to attack, raid, intercept, contain, evacuate, reinforce, or defend based on imperfect intel. The pressure is not only defeating hostiles. The commander must protect civilians, avoid reckless collateral damage, keep infrastructure usable, and live with district consequences after the mission.

The core fantasy remains:

```text
Read the city.
Identify the hostile position.
Prepare the right force.
Deploy into the same 3D operation map.
Strike with tactical control.
Protect civilians and infrastructure.
Live with the district consequences.
```

## Gameplay Model

WarlineCapture should play as a single-world RTS, not a dual-layer strategy/tactics game.

| Layer | New Meaning |
|---|---|
| Operation map | The actual playable 3D town/base/district scene. It contains routes, compounds, civilian zones, hostile positions, deployment zones, resources, and objective anchors. |
| Planning view | A zoomed-out camera, briefing overlay, or command-table representation of the same operation. It previews intel, deployment options, objective risk, and available units. |
| Battle view | A closer camera state inside the same 3D map. It is used for selecting units, issuing movement/attack/build/support commands, and reading local threats. |
| Minimap | A compact projection of the same 3D world, with objective/threat/selection/viewport markers. |
| District consequence | Result data produced by the same operation: trust, security, intel confidence, heat, infrastructure, civilian risk, and hostile influence. |

The game can still use authored missions, campaign nodes, operation days, and skirmish presets. Those modes now choose setup, rules, loadout, objectives, and rewards for a 3D operation map instead of moving the player between separate strategic and tactical maps.

## World And Encounter Direction

The 3D world should be built around Middle Eastern-inspired towns and forward operating spaces, using fictional geography and fictional hostile networks.

Required world ingredients:

- dense town blocks with houses, shops, city hall, market shops, alleys, walls, gates, and road barriers
- forward-base areas with barracks, guard towers, ammunition depots, helipads, fuel, water, tents, and command infrastructure
- civilian presence that must remain readable during combat
- hostile cells embedded in routes, compounds, buildings, rooftops, alleys, convoys, or ambush zones
- roads wide enough for APCs, trucks, tanks, missile launchers, and pathfinding stress
- air and support lanes for drones, helicopters, transport planes, and strike aircraft
- metadata-backed zones for deployment, civilian density, restricted fire, objectives, hostile influence, build placement, vehicle lanes, and camera bounds

Avoid naming real armed groups, real governments, or real-world conflicts in general design docs. The active first Campaign uses the fictional Ash Line and Vanguard Brigade working names. Civilian appearance is never evidence of hostility; use confirmed weapons, action, Intel, zone/objective context, and faction data.

## Canonical Unit And Building Source

The gameplay and UI roster should use the display names and descriptions already stored in:

```text
Assets/Game/Configs/Prefabs
```

Design docs, mission briefings, store cards, unit inspectors, build drawers, commander recommendations, and tooltips should not invent separate public names when a prefab config already has `displayName` and `description`.

Current catalog groups include:

| Group | Current Display-Name Examples |
|---|---|
| Commander and infantry | Field Commander, Rifleman Male II, Rifleman Female II, Heavy Gunner Male I, Marksman Male I, Marksman Female I, Advanced Rifleman Male II, Assault Breacher Female II, Sidearm Specialist Male I, Bomb Suit Specialist, Ghillie Rocketeer |
| Civilians | Civilian Male I, Civilian Male II, Civilian Female I, Civilian Female II |
| Contractors and pilots | Security Contractor Male I, Security Contractor Male II, Security Contractor Female I, Pilot Male I, Pilot Female I |
| Hostile force | Insurgent Rifleman Male V, Insurgent Rifleman Female I, Insurgent Gunner Male II, Insurgent Rocketeer Male I, Insurgent Raider Male III, Insurgent Sniper Male IV, Insurgent Sidearm Fighter Female II |
| Ground vehicles | Light Armored Car, Fast APC, Armored APC, Heavy APC, Battle Tank, Radar Tank, Cargo Truck, Canopy Truck, Tanker Truck |
| Air and support vehicles | Recon Drone, Attack Helicopter, Light Attack Helicopter, Transport Helicopter, Transport Plane, Strike Jet, Fighter Jet |
| Heavy support | Air Missile Launcher, Ground Missile Launcher |
| Military buildings | Barracks, Guard Tower, Heavy Guard Tower, Field Fabrication Depot, Helipad, Airport, Satellite Dish, Soldier Tent, Contractor Tent, Expert Tent |
| Civilian and town buildings | House, Shop, Market Shop, City Hall, Refugee Tent, Portable Toilet |
| Resource and utility buildings | Oil Pump, Oil Refinery, Large Oil Refinery, Fuel Bladder, Water Tank |
| Barriers and perimeter | Road Barrier, Dirt Wall, Fence Wall |

The design implication is that WarlineCapture already has enough roster breadth for a AAA-style mobile RTS presentation: infantry, civilians, hostile variants, contractors, armor, logistics, aircraft, drones, anti-air, ground missiles, base infrastructure, civilian town assets, and resource buildings.

## Mode Alignment

| Mode | 3D Single-Map Role |
|---|---|
| Campaign | Authored missions that teach command, civilian protection, base setup, radar warnings, airlift, convoy pressure, breach assault, and hostile-cell raids in curated 3D operation maps. |
| Operations | Persistent multi-day command layer that sends the player into the same kind of 3D operation maps, then applies district consequences to trust, security, intel, infrastructure, heat, supply, and hostile influence. |
| Skirmish | Configurable replay mode using the same 3D maps, unit catalog, AI settings, objective presets, and economy knobs for fast testing and replay. This replaces the player-facing "Quick Custom Game" label. |

The code can keep internal names such as Quick Custom while product-facing menu text should move toward Skirmish.

## Movement And Scale

The original large-scale grid-based movement goal remains valid, but it should be expressed as 3D navigation and command readability:

- squads and vehicles move across a large 3D town/base grid or nav mesh
- individual soldiers remain visible, but command should scale through squads, groups, transports, and control selections
- streets, alleys, walls, rooftops, courtyards, compounds, and vehicle lanes need readable tactical metadata
- camera zoom should support wide command reading and close unit control without changing maps
- many units on screen require LODs, pooled markers, simplified far-distance animation, readable selection rings, and strict mobile performance budgets

## UI Menus And Screens That Need Updating

The new menu art style is a 3D forward command-base presentation: dark military frame, olive/black/gold command panels, realistic command tent, base yard, tactical table, mode cards, commander status, and a strong `DEPLOY OPERATION` CTA.

Update these UI targets and screen docs:

| UI Surface | Required Update |
|---|---|
| `SCN-02 Main Menu` | Replace the old teal/cyan mode-select target with the command-base style. Left rail should read Campaign, Operations, Skirmish, Store, Commander, Settings. The persistent header shows Credits and Command only. Primary CTA should read Deploy Operation. |
| `SCN-05 Saga/Campaign Map` | Stop presenting a separate strategic map as the product truth. Present campaign nodes as operations selected from a command-table or world overlay that launches the same 3D operation-map model. |
| `SCN-06 Mission Briefing` | Use 3D operation preview, deployment-zone overlay, intel confidence, civilian-risk indicators, target cell description, and prefab-backed unit/building names. |
| `SCN-07 Loadout/Squad Prep` | Show available units and supports from `Assets/Game/Configs/Prefabs`, with role tags and descriptions from config data. |
| `SCN-08 RTS Battle HUD` | Update HUD assumptions to a 3D camera: squad tray, selected unit panel, objective tracker, minimap, threat jump, build/support controls, civilian-risk warnings, and command feedback over the same map. |
| `SCN-09 Build Drawer` | Use building config display names/descriptions for base, town, resource, defensive, and utility structures. Placement feedback must read 3D terrain/metadata. |
| `SCN-10 Unit Command Wheel` | Support 3D selection context: move, attack, hold, board, disembark, support, breach, scan, defend, and cancel where available. |
| `SCN-11 Operations Dashboard` | Reframe as command planning and district consequence management that sends the player into 3D operation maps. |
| `SCN-12 District Detail` | Replace pure strategic-district language with town/sector readiness, hostile-cell intel, civilian density, infrastructure, and deployment options. |
| `SCN-13 Skirmish Setup` | Preserve configurable rules but align visuals to the new command-base style. |
| `SCN-14 Store` | Keep economy guardrails, but update visual chrome to the command-base material system and resource labels. |
| `SCN-19 Armory` | Use the config-backed unit, vehicle, aircraft, support, and building roster as the inspection source. |
| `POP-01 Threat Alert` | Treat alerts as jumps inside the same 3D map, not as separate strategic-route previews. |
| `POP-02 Confirm Raid` | Use hostile-cell intel, civilian risk, deployment choice, and confidence data. |
| `POP-03 Build Placement` | Update to 3D placement validity, footprint projection, blocked terrain, route obstruction, and civilian-zone warnings. |
| `POP-05 Mission Result` | Report tactical success plus district consequence from the single 3D operation. |
| `POP-06 End Of Day Report` | Show how completed 3D operations changed district state. |
| `POP-10 Assistant Takeover` | ARIA should act on selected units, camera jumps, and metadata anchors inside the same 3D map. |
| `POP-11 Commander Identity` | Align portrait/status panels to the new commander-side-panel visual language. |
| `PREFAB-01 Objective Tracker` | Read objective anchors and state from the 3D operation map. |
| `PREFAB-02 Squad Tray` | Scale to many soldiers and grouped units while keeping mobile readability. |
| `PREFAB-03 Build Drawer` | Use config-backed building names and descriptions. |
| `PREFAB-05 Assistant Panel` | Reference 3D world anchors, unit groups, civilian-risk zones, and hostile-cell intel. |

## Design Rules

- Do not describe the active gameplay target as 2.5D isometric.
- Do not describe the active gameplay target as a separate strategic map plus tactical map.
- Do not bake stateful gameplay into background art.
- Do use 3D runtime entities for soldiers, civilians, vehicles, aircraft, buildings, markers, projectiles, VFX, and selection feedback.
- Do use metadata as gameplay truth for routes, zones, anchors, objective positions, build validity, civilian risk, and camera bounds.
- Do keep civilian protection and collateral risk central to the command fantasy.
- Do preserve stable roles for unique character configs. Civilian configs remain civilians, insurgent configs remain hostile actors, and named recurring models are not randomly reused for contradictory roles in one operation.
- Do keep the AAA mobile target: readable input, strong camera control, performance budgets, polished menu art, clear unit roles, and crisp feedback.

## Implementation Implications

This document changes product/design direction, not runtime code by itself.

Future implementation work should:

1. preserve the current Unity ECS tactical simulation as the runtime foundation
2. route Campaign, Operations, and Skirmish into 3D operation scenes
3. treat `Assets/Game/Configs/Prefabs` as the public unit/building catalog source
4. update menu visuals and UI target docs before rebuilding screens
5. validate large-map scale with performance probes before adding production content
6. keep M01 early gameplay simple unless PM explicitly expands it beyond the current playable-slice gate

## Superseded Direction

Superseded 2D/isometric, macro-tile, strategic/tactical-map, FG01 request, and pre-3D UI audit documents have been moved to `Design/Archive/Legacy2D_2026-05-21`. They are retained for process history and lessons learned, not active gameplay/art direction.

When archived docs conflict with this document, this document wins.

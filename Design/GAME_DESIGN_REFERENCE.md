# WarlineCapture Game Design Reference

Last updated: 2026-05-01

This document summarizes the implemented game design for WarlineCapture. It is intended as a compact reference that can be given to ChatGPT or another design tool for future work on game modes, balance, AI tuning, difficulty settings, and feature planning.

The companion implementation-planning document is `AI_CONTROLLER_DESIGN.md`.

## Summary

WarlineCapture is a grid-based real-time strategy game about building and defending a military/civilian base inside a generated city. The player controls faction 0 by default. Enemy AI controls faction 1, and the same AI stack can optionally control the player faction in Auto Mode.

Active presentation direction:

- Premium 2D isometric mobile RTS.
- Original design docs in `Design` are the source of truth.
- Visual references and production spike outputs live under `Design/VisualReferences`.
- The first validated golden asset spike is `Design/VisualReferences/2DIsometricProduction`.
- Unity imported 2D iso sprites and Tile assets live under `Assets/Game/Art/Generated/2DISO`.

The game combines:

- Base building: tents, barracks, resource buildings, aircraft facilities, walls, gates, and civilian support structures.
- Unit production: infantry, contractors, civilians, APCs, tanks, missile launchers, drones, helicopters, jets, and transport aircraft.
- Tactical combat: units can move on a grid, auto-engage, receive commanded attack orders, breach base walls/gates, and fight buildings or units.
- Logistics and transport: APCs and transport helicopters can carry soldiers; helicopters can land for pickup and rope-drop passengers; transport planes support runway-based delivery.
- Economy: AI factions own money, oil, and fuel, sell stored resources, and spend money on buildings and units.
- AI control: AI builds, produces, forms squads, selects targets, issues attack orders, and can be adjusted through difficulty and behavior settings.
- Threat warning: radar/satellite systems detect approaching ground or air threats and trigger tactical warnings.

High-level current game loop:

1. The player starts at a configured base with initial units and resources.
2. The city/grid environment is spawned with roads, buildings, blockers, and decorations.
3. Player requests buildings and unit production from UI.
4. AI-controlled factions build their camp, produce preferred units, form attack squads, and attack enemy targets.
5. Combat resolves through ECS movement, targeting, attack, base-breach, transport, and health systems.

## Core Runtime Model

WarlineCapture is implemented as a hybrid Unity/ECS RTS:

- Grid position is stored with `UnitGrid`.
- Movement uses `UnitMove`, `UnitTarget`, `UnitPathRequest`, and path-follow components.
- Ownership uses `Faction`.
- Combat uses `UnitHealth`, `UnitCombat`, `UnitAttack`, `EngageTarget`, and `BaseBreachOrder`.
- Buildings are represented through runtime building data inside `BuildingPlacementSystem` plus ECS combat/blocker components.
- AI is faction-agnostic. The same systems work for enemy factions and player Auto Mode.

Primary factions:

| Faction | Role | Default behavior |
|---|---|---|
| 0 | Player | Manual control unless Player Auto AI is enabled |
| 1 | Enemy | AI controlled by default |

The UI supports selecting 1 to 3 enemy AIs, although the current scene config has a primary enemy AI config.

## Current Starting Setup

Initial spawn config:

- Player faction 0 spawn cell: `(180, 180)`.
- Enemy faction 1 spawn cell: `(500, 180)`.
- Initial player resources: `$300000`, `50 oil`, `100 fuel`.
- Initial faction bases are created automatically.
- Base wall prefab: `Wall_Dirt_Straight`.
- Base gate prefab: `Road_Barrier`.
- Base core building prefab: `Building_Barrack`.
- Base half size: `120 x 80` cells.
- Minimum units per faction: `18`.

The starting armies include infantry squads, ground vehicles, aircraft, helicopters, and support units. Enemy faction uses insurgent infantry mixed with similar vehicle/air assets.

## Economy

Economy data is per faction:

- `Money`
- `Oil`
- `Fuel`
- `OilIncomeRate`
- `FuelIncomeRate`
- sell policy: oil sell price, fuel sell price, income multiplier, sell interval

AI economy behavior:

- Resource-producing buildings update faction resource snapshots.
- AI sells whole stored barrels of oil/fuel on its sell interval.
- Revenue is added to faction money.
- AI spends money when placing buildings or queuing units.

Default AI sell prices:

| Resource | Sell price |
|---|---:|
| Oil | 150 |
| Fuel | 220 |

Important implemented resource buildings:

| Building | Oil/day | Oil storage | Fuel/day | Fuel storage | Notes |
|---|---:|---:|---:|---:|---|
| Oil Pump | 50 | 200 | 0 | 0 | Basic oil extractor |
| Oil Refinery | 0 | 5000 | 100 | 5000 | Processes crude into fuel/resources |
| Large Oil Refinery | 0 | 10000 | 200 | 10000 | High-capacity refinery |
| Heavy Guard Tower | 0 | 10000 | 0 | 10000 | Also acts as a resource storage structure in config |

## AI Architecture

The AI is split into small ECS systems:

| System | Responsibility |
|---|---|
| `AIFactionControlSystem` | Applies `AIControlledTag` or `ManualControlledTag` by faction and clears manual orders from AI-controlled factions |
| `AIEconomySystem` | Reads faction resource buildings, sells oil/fuel, adds revenue, logs economy |
| `AIBuildPlannerSystem` | Places missing preferred buildings around the faction base center if affordable |
| `AIProductionSystem` | Queues preferred units/vehicles from owned production buildings if affordable |
| `AISquadSystem` | Groups idle AI-controlled combat units into attack squads |
| `AITargetingSystem` | Scores enemy targets and assigns squad targets |
| `AICombatOrderSystem` | Issues attack/base-breach orders to squad members |

AI validation logs use these tags:

- `[AIControlMode]`
- `[AIEconomy]`
- `[AIBuild]`
- `[AIProduction]`
- `[AISquad]`
- `[AITarget]`
- `[AICombat]`

### AI Controller Configs

Enemy AI config:

| Field | Value |
|---|---|
| Enabled | true |
| Role | Enemy |
| Faction | 1 |
| Difficulty | Normal |
| Starting money | 75000 |
| Income multiplier | 1.15 |
| Build interval | 8 seconds |
| Unit production interval | 6 seconds |
| Attack interval | 45 seconds |
| Max active attack groups | 2 |
| Defense radius | 45 cells |
| Aggression | 0.65 |
| Preferred buildings | Tent_Regular, Building_Barrack, Building_OilPump, Building_Fuel_Bladder, Building_Ammunition_Depot, Building_Airport |
| Preferred units | Unit_Chr_Soldier_Male_02_Alt_04, Unit_Chr_Ghillie_Male_01 |
| Preferred vehicles | Unit_Veh_APC_Fast, Unit_Veh_APC_Heavy, Unit_Veh_Tank_USA |

Player Auto AI config:

| Field | Value |
|---|---|
| Enabled | true |
| Role | PlayerAuto |
| Faction | 0 |
| Auto-controls player faction | true |
| Difficulty | Normal |
| Starting money | 300000 |
| Income multiplier | 1.0 |
| Build interval | 10 seconds |
| Unit production interval | 8 seconds |
| Attack interval | 60 seconds |
| Max active attack groups | 1 |
| Defense radius | 55 cells |
| Aggression | 0.35 |
| Preferred buildings | Tent_Regular, Building_Barrack, Building_OilPump, Building_Fuel_Bladder, Building_Ammunition_Depot |
| Preferred units | Unit_Chr_Soldier_Male_02_Alt_04, Unit_Chr_Soldier_Female_02 |
| Preferred vehicles | Unit_Veh_APC_Heavy, Unit_Veh_Light_Armored_Car |

### AI Difficulty and Tuning Knobs

Runtime AI dropdowns:

- Difficulty: Easy, Normal, Hard, Brutal
- Starting Credits (tactical Money): Low, Normal, High
- Income Multiplier: 0.75x, 1x, 1.25x, 1.5x, 2x
- Build Speed: Slow, Normal, Fast
- Unit Production Speed: Slow, Normal, Fast
- Attack Group Size: Small, Normal, Large
- Attack Frequency: Rare, Normal, Frequent
- Aggression: Defensive, Balanced, Aggressive
- Expansion: Off, Slow, Normal, Fast
- Target Priority: Balanced, Units, Economy, Production
- Player Auto: Off, On
- Enemy Count: 1, 2, 3

Implemented difficulty math for enemy AI:

| Setting | Effect |
|---|---|
| Easy | money x0.75, income x0.8, intervals x1.25, smaller squads, fewer active squads |
| Normal | baseline |
| Hard | money x1.25, income x1.2, intervals x0.85, larger squads, more active squads |
| Brutal | money x1.6, income x1.5, intervals x0.65, much larger squads, more active squads |
| Starting Credits Low/High | additional x0.75 or x1.5 tactical Money multiplier |
| Speed Slow/Fast | build/production intervals x1.35 or x0.7 |
| Expansion Off | disables enemy building expansion |
| Expansion Slow/Fast | build interval x1.4 or x0.75 |
| Attack Group Size Large | increases max squad size by 4 and min squad size by 1 |
| Attack Frequency Frequent | increases max active squads by 1 |
| Aggressive | lowers min squad threshold by 1 and adds one active squad |
| Defensive | raises min squad threshold by 1 |

### AI Build Logic

Current AI build planner:

- Runs only after play is requested.
- Requires faction to be AI-controlled.
- Uses the faction spawn cell as base center.
- Iterates preferred building IDs.
- Skips buildings already owned by that faction.
- Places buildings in a five-point pattern around the base center: center, right, left, up, down, with wider rings later.
- Checks configured spawnable, requestability, and cost.
- Deducts money on success.

Known limitation: the current planner wants one of each preferred building. It does not yet reason about build ratios, tech dependencies, defense placement, rebuild priority, or economy targets.

### AI Production Logic

Current AI production:

- Runs only after play is requested.
- Requires faction to be AI-controlled.
- Cycles through preferred unit and vehicle IDs.
- Uses `BuildingPlacementSystem.TryQueueFactionUnitProduction`.
- Checks if a compatible owned producer exists.
- Avoids overproducing above `TargetProducedUnits = 3`.
- Avoids queueing more than `MaxQueuedUnits = 3`.
- Deducts money when production is queued.

Known limitation: AI does not yet adapt production to opponent composition, losses, map control, air threat, or current economy.

### AI Squad and Targeting Logic

Current squad creation:

- Uses idle AI-controlled units with health > 0.
- Ignores units already in squads, already engaged, static blockers, or units with active path requests.
- Creates attack squads once enough idle units are available.
- Default target faction is the opposite primary faction: faction 0 attacks faction 1 and faction 1 attacks faction 0.

Current target scoring:

- Base score: `100 - distance + healthValue`.
- Threat units get +45.
- Buildings get +35.
- Ordinary units get +10.
- Resource haulers get +20.
- Target Priority can further bias:
  - Units: boosts units/threats, reduces buildings.
  - Economy: boosts resource haulers and buildings.
  - Production: boosts buildings, reduces ordinary units.

Current combat orders:

- AI orders squad units to attack selected target.
- Orders refresh every 2 seconds if needed.
- Units that cannot attack are skipped.
- If target is behind an enemy base perimeter, units receive a `BaseBreachOrder` to attack a wall/gate breach target first, then continue to the final target.

## Player Control and Auto Mode

Manual player control:

- Player selects units and issues move/attack/transport commands.
- Player faction receives `ManualControlledTag`.

Player Auto Mode:

- Player faction receives `AIControlledTag`.
- Manual movement and commanded attack orders are cleared from AI-controlled factions.
- Same AI build, production, squad, targeting, and combat systems can control faction 0.

Design implication: future game modes can use Auto Mode for simulation, AI-vs-AI matches, tutorials, assistant commanders, or hands-off strategic testing.

## Combat and Movement

Units support:

- Grid movement with speed, road-speed multiplier, and arrive distance.
- Infantry-style and vehicle-style movement behavior.
- Ground and air movement.
- Auto-engage and commanded engage.
- Health, damage, cooldown, range, and attack trace visuals.
- Turret aiming for selected vehicles.
- Idle wandering if allowed.

Base breach behavior:

- Walls and gates are runtime combat targets.
- If a final target is inside an enemy base perimeter, attackers can be redirected to a breach target first.
- Road Barrier gates are preferred breach/passability points when available.
- If no gate exists, enemy units can attack a wall segment.
- Destroyed gates clear blockers and stop future breach redirection for that opening.
- Friendly units can pass through friendly road barrier gates when close enough; gates visually open/close.

## Transport and Logistics

Transport features:

- Ground transport: APCs can board nearby soldiers.
- Air transport: helicopters must be landed before boarding.
- Helicopter pickup logic can command a flying helicopter to land near passengers before boarding.
- Helicopters can rope-disembark passengers one at a time while airborne.
- Rope drops disperse passengers to clear nearby cells.
- Transport planes are production transport units and require runway/airport behavior.

Important transport rules:

- Transport capacity is currently 10 soldiers for APCs and transport helicopters.
- Passengers are hidden while inside transports.
- Transport boarding respects landing state for air transports.
- Rope disembark keeps passengers attached until each drop is processed.

## Threat Detection and Tactical Warnings

Implemented detectors:

| Detector | Kind | Radius | Behavior |
|---|---|---:|---|
| Radar Tank | Ground | 240 cells | Detects enemy ground vehicles moving toward player sensors; cannot attack |
| Satellite Dish | Air | 240 cells | Detects hostile aircraft approaching the base; cannot attack |

Threat detection logic:

- Only player faction sensors currently trigger warnings.
- Ground radar ignores soldiers and focuses on ground vehicles.
- Air sensors focus on air targets.
- Warnings only fire for new threats entering detection state.
- Targets must be moving toward the sensor/base, based on target/path/engage/base-breach goal.
- Estimated time to base is calculated from distance and target speed.

## Buildings

| ID | Display Name | Cost | HP | Role/Function | Production / Special |
|---|---:|---:|---:|---|---|
| Airport | Airport | 120000 | 1800 | Air operations | Transport Plane, Drone, Strike Jet, Fighter Jet |
| Ammunition_Depot | Ammunition Depot | 45000 | 900 | Combat supplies | No current production |
| Building_Barrack | Barracks | 40000 | 1200 | Military housing/training | No current production |
| Building_Satelite_Dish | Satellite Dish | 20000 | 450 | Air threat detection | Air detector, radius 240 |
| Fuel_Bladder | Fuel Bladder | 18000 | 500 | Vehicle/fuel support | APCs, armored car, tank, missile launchers, trucks, Radar Tank |
| GuardTower | Guard Tower | 22000 | 700 | Base defense/spotting | No current attack config in building asset |
| GuardTower_Big | Heavy Guard Tower | 30000 | 950 | Defensive/resource support | Stores 10000 oil and 10000 fuel |
| Hall_01 | City Hall | 50000 | 1200 | Civilian/city admin | City role |
| Helipad | Helipad | 35000 | 700 | Helicopter facility | Light Attack Helicopter, Attack Helicopter, Transport Helicopter |
| House_01 | House | 9000 | 350 | Civilian residence | House role |
| OilPump | Oil Pump | 50000 | 650 | Oil extraction | 50 oil/day, 200 storage |
| OilRefinery | Oil Refinery | 80000 | 1400 | Resource processing | 100 fuel/day, 5000 oil storage, 5000 fuel storage |
| OilRefinery_Big | Large Oil Refinery | 140000 | 2200 | Large resource processing | 200 fuel/day, 10000 oil storage, 10000 fuel storage |
| Portaloo_ | Portable Toilet | 1000 | 100 | Field support flavor | No current production |
| Road_Barrier | Road Barrier | 6000 | 800 | Gate/route blocker | Opens for owner units; breach target |
| Shop_01 | Shop | 14000 | 400 | Civilian commerce | Shop role |
| Shop_02 | Market Shop | 16000 | 450 | Civilian commerce | Shop role |
| Tent_Contractor | Contractor Tent | 8000 | 350 | Contractor production | Contractor Female I, Contractor Male I, Contractor Male II |
| Tent_Expert | Expert Tent | 10000 | 400 | Specialist production | Soldier/Pilot/Ghillie specialist units |
| Tent_Refugee | Refugee Tent | 6000 | 300 | Civilian/refugee shelter | Civilian units, refugee capacity 10 |
| Tent_Regular | Soldier Tent | 12000 | 450 | Infantry production | Regular soldier variants |
| Wall_Dirt_Straight | Dirt Wall | 10000 | 2500 | Perimeter wall | Blocks movement; breachable |
| Wall_Fence_Straight | Fence Wall | 7000 | 1800 | Perimeter fence | Blocks movement; breachable |
| WaterTank | Water Tank | 14000 | 700 | Utility/air support | Helicopter variants |

## Units

### Infantry, Contractors, Civilians

| ID | Display Name | Cost | HP | Speed | Damage | Range | Role |
|---|---:|---:|---:|---:|---:|---:|---|
| Chr_Bombsuit_Male_01 | Bomb Suit Specialist | 18000 | 180 | 2.8 | 18 | 4.5 | Slow durable hazardous-objective specialist |
| Chr_Civilian_Female_01 | Civilian Female I | 0 | 50 | 3.2 | 1 | 1.5 | Non-combatant targetable civilian |
| Chr_Civilian_Female_02 | Civilian Female II | 0 | 50 | 3.2 | 1 | 1.5 | Non-combatant targetable civilian |
| Chr_Civilian_Male_01 | Civilian Male I | 0 | 50 | 3.2 | 1 | 1.5 | Non-combatant targetable civilian |
| Chr_Civilian_Male_02 | Civilian Male II | 0 | 50 | 3.2 | 1 | 1.5 | Non-combatant targetable civilian |
| Chr_Contractor_Female_01 | Security Contractor Female I | 9000 | 110 | 4.2 | 7 | 7 | SMG contractor, base security/patrol |
| Chr_Contractor_Male_01 | Security Contractor Male I | 9000 | 110 | 4.2 | 8 | 6 | Pistol contractor, base security/patrol |
| Chr_Contractor_Male_02 | Security Contractor Male II | 9500 | 115 | 4.2 | 14 | 10 | Rifle contractor |
| Chr_Ghillie_Male_01 | Ghillie Rocketeer | 16000 | 90 | 4.0 | 70 | 13 | Anti-vehicle/fortification ambusher |
| Chr_Insurgent_Female_01 | Insurgent Rifleman Female I | 8500 | 95 | 4.4 | 14 | 10 | Enemy irregular rifleman |
| Chr_Insurgent_Female_02 | Insurgent Sidearm Fighter Female II | 7500 | 85 | 4.4 | 8 | 6 | Enemy irregular pistol fighter |
| Chr_Insurgent_Male_01 | Insurgent Rocketeer Male I | 15000 | 95 | 4.4 | 65 | 12 | Enemy RPG/anti-vehicle fighter |
| Chr_Insurgent_Male_02 | Insurgent Gunner Male II | 12000 | 115 | 4.4 | 9 | 10 | Enemy machine gunner |
| Chr_Insurgent_Male_03 | Insurgent Raider Male III | 9000 | 105 | 4.4 | 7 | 7 | Enemy SMG raider |
| Chr_Insurgent_Male_04 | Insurgent Sniper Male IV | 13000 | 80 | 4.4 | 45 | 16 | Enemy sniper |
| Chr_Insurgent_Male_05 | Insurgent Rifleman Male V | 8500 | 100 | 4.4 | 14 | 10 | Enemy rifleman |
| Chr_Leader_Male_01 | Field Commander | 20000 | 160 | 4.6 | 10 | 7 | High-value command unit |
| Chr_Pilot_Female_01 | Pilot Female I | 7000 | 80 | 3.6 | 7 | 5.5 | Light aviation personnel |
| Chr_Pilot_Male_01 | Pilot Male I | 7000 | 80 | 3.6 | 7 | 5.5 | Light aviation personnel |
| Soldier rifle variants | Rifleman Male/Female variants | 10000 | 120 | 4.8 | 15 | 10 | Reliable frontline infantry |
| Soldier marksman variants | Marksman Male/Female variants | 11500 | 100 | 4.8 | 48 | 16 | Long-range infantry |
| Soldier heavy/advanced variants | Heavy Gunner, Advanced Rifleman, Assault Breacher | 12500-14000 | 130-140 | 4.8 | 8-17 | 7.5-11 | Heavier infantry options |
| Soldier sidearm variants | Sidearm Specialist variants | 8500 | 100 | 4.8 | 8-9 | 6-6.5 | Cheaper short-range infantry |

### Vehicles and Aircraft

| ID | Display Name | Cost | HP | Footprint | Air | Transport | Speed | Damage | Range | Role |
|---|---:|---:|---:|---|---|---:|---:|---:|---:|---|
| Veh_APC_Fast | Fast APC | 34000 | 550 | 3x3 | No | 10 | 11 | 10 | 2 | Fast infantry transport, cannot attack effectively in config |
| Veh_APC_Heavy | Heavy APC | 45000 | 850 | 3x3 | No | 10 | 8 | 18 | 7 | Armored transport with combat capability |
| Veh_APC_Slow | Armored APC | 38000 | 700 | 3x3 | No | 10 | 7 | 10 | 2 | Durable infantry transport |
| Veh_Drone | Recon Drone | 8000 | 70 | 1x1 | Yes | 0 | 28 | 10 | 8 | Scout/patrol aircraft |
| Veh_Helicopter_Attack | Attack Helicopter | 70000 | 600 | 1x1 | Yes | 0 | 22 | 14 | 12 | Rapid close air support |
| Veh_Helicopter_Attack_Small | Light Attack Helicopter | 52000 | 420 | 1x1 | Yes | 0 | 24 | 12 | 11 | Fast light air support |
| Veh_Helicopter_Transport | Transport Helicopter | 62000 | 650 | 1x1 | Yes | 10 | 20 | 10 | 2 | Air transport, lands for boarding, rope deploys passengers |
| Veh_Jet_01 | Strike Jet | 85000 | 550 | 1x1 | Yes | 0 | 36 | 45 | 14 | Fast precision strike aircraft |
| Veh_Jet_02 | Fighter Jet | 90000 | 520 | 1x1 | Yes | 0 | 36 | 50 | 15 | Air-superiority/strike aircraft |
| Veh_Light_Armored_Car | Light Armored Car | 28000 | 450 | 3x3 | No | 0 | 13 | 16 | 8 | Fast fire support/flanking vehicle |
| Veh_Missle_Launcher_Air | Air Missile Launcher | 46000 | 400 | 3x3 | No | 0 | 7.5 | 80 | 600 | Ground-to-air missile defense |
| Veh_Missle_Launcher_Ground | Ground Missile Launcher | 42000 | 450 | 3x3 | No | 0 | 7.5 | 90 | 600 | Long-range ground attack / base attack |
| Veh_Plane_Transport | Transport Plane | 95000 | 900 | 1x1 | Yes | 0 | 30 | 8 | 6 | Production transport, runway landing/takeoff |
| Veh_Radar_Tank | Radar Tank | 32000 | 500 | 3x3 | No | 0 | 7 | 0 | 0 | Ground threat detector, radius 240, cannot attack |
| Veh_Tank_USA | Battle Tank | 65000 | 1000 | 3x3 | No | 0 | 8.5 | 75 | 10 | Heavy frontline armor |

## Production Relationships

Building production families:

- Soldier Tent produces regular soldier variants.
- Contractor Tent produces security contractors.
- Expert Tent produces soldiers, pilots, and Ghillie Rocketeer.
- Refugee Tent produces civilian variants.
- Fuel Bladder produces ground vehicles and support vehicles, including APCs, tank, missile launchers, trucks, and Radar Tank.
- Helipad produces helicopter variants.
- Water Tank also produces helicopter variants.
- Airport produces Transport Plane, Drone, Strike Jet, and Fighter Jet.

Design implication: production is already grouped by intuitive facility type. Future tech trees can build on this by adding prerequisites such as Airport before jets, Fuel Bladder before armored vehicles, or Oil Refinery before missile launchers.

Canonical progression, upgrade tiers, support abilities, design-ready naval units, and balance/visual config separation now live in `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`, `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`, and `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`.

## Current Balance Shape

Current rough balance:

- Infantry cost range: 0 to 20000.
- Standard rifle infantry: 10000, 120 HP, 15 damage, 10 range.
- Snipers/marksmen: 11500-13000, lower HP, high range/damage.
- Rocketeers: 15000-16000, high damage, good anti-armor role.
- APCs: 34000-45000, 550-850 HP, transport 10.
- Tanks: 65000, 1000 HP, 75 damage.
- Missile launchers: 42000-46000, 400-450 HP, huge range 600, high damage.
- Aircraft: 52000-95000, high speed, medium-to-high damage, vulnerable to air missile/radar design space.
- Buildings: cheap utility starts at 1000-18000; production/resource/air buildings range 35000-140000.

Balance risks to revisit:

- Missile launcher range is extremely high compared with other units.
- Many buildings have no active defensive attack behavior despite defensive descriptions.
- Some vehicles have attack values but `canAttack` disabled or low range, so their practical role is transport.
- Civilian units are targetable and have tiny attack values; decide if that is intended.
- AI production target is currently shallow: three produced/queued per selected unit type.
- AI build strategy is one-of-each preferred building, not strategic scaling.

## Good Future Design Hooks

These are natural extension points for future ChatGPT/design sessions:

- Game modes:
  - Base defense waves.
  - AI skirmish with 1 to 3 enemies.
  - Escort/refugee evacuation.
  - Resource race.
  - Air superiority scenario.
  - City protection / civilian casualty constraints.
  - AI-vs-AI spectator mode.
- Difficulty:
  - Separate economy, aggression, reaction, targeting, air usage, and rebuild sliders.
  - Fog-of-war limits for Easy/Normal AI.
  - Brutal AI with faster rebuilds and larger mixed-arms squads.
- AI improvements:
  - Rebuild destroyed key buildings.
  - Build duplicates based on economy and production pressure.
  - Detect missing tech and build prerequisite chains.
  - Counter-composition logic: anti-air when player uses aircraft, tanks/rocketeers against armor, infantry against weak defenses.
  - Defensive squads and patrol routes.
  - Harassment squads targeting economy/resource haulers.
  - Staging/rally points before attacks.
  - Retreat/repair behavior for damaged vehicles.
- Unit design:
  - Explicit anti-air, anti-armor, anti-infantry tags.
  - Support units: medics, engineers, repair vehicles, scouts.
  - Ammo/fuel consumption for vehicles and aircraft.
  - Morale/civilian reputation for civilian/refugee systems.
- Building design:
  - Active guard towers.
  - Power/water/fuel dependencies.
  - Runtime implementation of the catalog upgrade tracks already designed in `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`.
  - Walls with armor types and breach tools.
- UI/UX:
  - AI debug panel with current plans, target scores, squad states, and economy.
  - Difficulty preset preview showing exact multipliers.
  - Warnings categorized by ground, air, breach, economy under attack, and production under attack.

## ChatGPT Prompt Starter

Use this prompt when asking for design help:

```text
You are helping design WarlineCapture, a grid-based Unity RTS. Use the attached design reference as source of truth. Propose ideas that fit the implemented systems: faction economy, build/production facilities, APC/helicopter/plane transport, base breach combat, radar/satellite threat warnings, and faction-agnostic AI. Avoid assuming features not listed unless clearly marked as new. When suggesting difficulty or mode changes, map them to existing knobs such as AI money, income, build speed, production speed, squad size, attack frequency, aggression, expansion, target priority, and enemy count.
```

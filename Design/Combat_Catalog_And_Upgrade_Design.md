# WarlineCapture Combat Catalog And Upgrade Design

Date: 2026-05-05

## Purpose

This document closes the unit, building, skill, ability, and upgrade-design gap for WarlineCapture. It defines the catalog model that mission designers, economy balancers, UI implementers, and visual artists should use before adding more level-by-level content.

The machine-readable companion files are:

- `BalanceConfigs/Combat_Balance_Config_v0_1.json`
- `VisualConfigs/Combat_Visual_Config_v0_1.json`

The balance config owns gameplay numbers. The visual config owns art references. They are linked only by stable ids and `visualCatalogId`.

## Source Order

Read these files together:

1. `GAME_DESIGN_REFERENCE.md`
2. `Combat_Catalog_And_Upgrade_Design.md`
3. `BalanceConfigs/Combat_Balance_Config_v0_1.json`
4. `VisualConfigs/Combat_Visual_Config_v0_1.json`
5. `Economy_Reward_Design.md`
6. `Automated_Fuel_Logistics_Design.md`
7. `Balancing_Automated_Test_Plan.md`
8. `Level_And_Mission_Content_Plan.md`

`GAME_DESIGN_REFERENCE.md` remains the compact snapshot of what is implemented today. This document and its configs are the design-ready expansion layer for progression, upgrades, balancing, UI, and future asset production.

## Separation Rule

Balance config owns:

- entity id, category, domain, producer relationships, unlock gates, costs, build times, production times, HP, speed, range, damage, cooldown, transport capacity, resource output, upgrade costs, upgrade modifiers, ability charges, ability resource costs, balance tags.

Visual config owns:

- world prefab or sprite path, UI icon path, portrait path, damage-state art paths, VFX cue id, audio cue id, animation set id, silhouette rules, art brief, faction tint expectations, tier badge art.

Neither config should duplicate the other. The balancer can tune economy and combat by editing balance data without touching art, and the art team can replace visuals without changing combat values.

## Catalog Scope

The current catalog contains:

| Type | Count | Status |
|---|---:|---|
| Units | 57 | 51 implemented Unity config anchors plus 6 design-ready sea units. |
| Buildings | 30 | 24 implemented Unity config anchors plus 6 design-ready expansion buildings. |
| Skills and abilities | 27 | Implemented command anchors plus support, Operation, air, breach, repair, and naval abilities. |
| Upgrade tracks | 40 | Four-tier upgrade tracks for character, vehicle, air, sea, building, enemy escalation, and support systems. |
| Visual entries | 154 | One visual entry for every unit, building, ability, and upgrade track. |

Implementation status values are intentional design states:

| Status | Meaning |
|---|---|
| `implemented` | Existing Unity runtime/config anchor exists. |
| `designReadyNeedsUnityPrefab` | Gameplay and visual design are complete, but Unity prefab/runtime asset production is still required. |
| `designReadyNeedsCode` | Gameplay design is complete, but runtime service or command implementation is required. |
| `designReadyNeedsUpgradeService` | Upgrade data is complete, but the upgrade service and persistence path must be implemented. |

## Tier Model

All player-facing unit, building, ability, and support upgrades use four tiers:

| Tier | Name | Gameplay Meaning |
|---:|---|---|
| 1 | Base Issue | Default item as unlocked or produced. |
| 2 | Field Upgrade | First progression sink; small, readable performance improvement. |
| 3 | Advanced Upgrade | Mid-progression role reinforcement. |
| 4 | Elite Upgrade | Late-progression specialization with higher cost and stronger identity. |

Upgrade currencies are `Credits`, `Materials`, and item-specific `BlueprintParts`. Store bundles may grant resources or parts, but upgrade application always happens through the upgrade/progression system. Store purchases never upgrade an item directly inside active combat.

## Unit Families

| Family | Includes | Primary Design Role | Upgrade Track Pattern |
|---|---|---|---|
| Rifleline Infantry | Riflemen and baseline soldier variants. | Reliable frontline anti-infantry and tutorial roster. | Damage, HP, modest range/readiness. |
| Suppression Infantry | Heavy gunner and advanced soldier variants. | Hold lanes, defend civilians, absorb pressure. | HP, cooldown, suppression uptime. |
| Marksman / Recon | Marksmen and sniper-like units. | Long-range pressure and exposed-target control. | Range, damage, scouting value. |
| Breach / Hazard | Bomb suit and assault breacher roles. | Safe wall/gate/objective breach and hazard missions. | HP, breach charge value, structure damage. |
| Anti-Armor | Ghillie and rocketeer roles. | Counter APCs, tanks, towers, and fortified nodes. | Damage, range, cooldown. |
| Contractor Security | Contractors and close security. | Early base security, patrols, Operation escort flavor. | HP, speed, close-range stability. |
| Civilians | Civilian variants. | Protection target, evacuation payload, Operation consequence source. | Safety, evacuation speed, reduced panic penalties. |
| Enemy Irregulars | Insurgent variants. | Enemy roster scaling by threat family. | Enemy escalation tracks for HP, damage, range. |
| Ground Vehicles | APCs, trucks, armored car, tank, missile launchers, radar tank. | Transport, logistics, armor, siege, detection. | Armor, transport, logistics capacity, fire control, sensors. |
| Air Units | Drone, helicopters, jets, transport plane. | Recon, airlift, strike, air superiority. | Sensors, fuel efficiency, survivability, attack package. |
| Sea Units | Patrol boat, interceptor, landing craft, cutter, missile craft, drone boat. | Port/coastal missions, naval patrol, landing, harbor intel. | Hull, transport, command systems, naval fire control, sensors. |

## Building Families

| Family | Includes | Primary Design Role | Upgrade Track Pattern |
|---|---|---|---|
| Training Facilities | Soldier Tent, Contractor Tent, Expert Tent, Barracks. | Unit production and Chapter 1 teaching loop. | Production speed, queue slots, unlock discount. |
| Base Defense | Guard Towers, walls, road barrier. | Breach prevention and defensive preparation. | HP, repair cost, detection support where applicable. |
| Sensor Network | Satellite Dish, Coastal Radar, Radar Tank support path. | Threat warnings, air/ground/naval ETA clarity. | Detection radius and warning lead time. |
| Air Operations | Helipad, Airport, Water Tank. | Helicopter, drone, transport, jet production and air support. | Queue slots, fuel efficiency, air support cooldown. |
| Resource Chain | Oil Pump, Fuel Bladder, Refinery, Large Refinery, Logistics Dock. | Tactical and Operation fuel/material pressure. `Automated_Fuel_Logistics_Design.md` defines the automated tray/tanker loop and stored usable Fuel meaning. | Output, storage, conversion efficiency, logistics throughput. |
| Civic Support | House, Shops, City Hall, Refugee Tent, Medical Station, Portable Toilet. | Civilian density, trust, shelter, recovery. | Capacity, trust gain, civilian penalty reduction. |
| Armory Supply | Ammunition Depot and gear-support path. | Gear modules, parts, support ability tuning. | Parts crafting, material discounts, power budget. |
| Naval Operations | Naval Yard, Dock, Coastal Radar. | Sea-unit production, harbor threats, port missions. | Production, fuel costs, naval support cooldown. |
| Command Post | Forward Command Post. | Rally, readiness, and command support. | Command cooldown, rally radius, readiness gain. |

## Skills And Abilities

Skills are immediate command verbs tied to selected units. Abilities are authored support actions with costs, cooldowns, charges, or Operation effects.

| Group | Examples | Rules |
|---|---|---|
| Core commands | Move, Attack, Stop, Hold Position, Focus Fire. | Free or very low friction; belongs on command wheel/HUD. |
| Transport commands | Load Transport, Unload Transport, Rope Disembark. | Uses valid boarding/drop rules; may spend Fuel for air deployment. |
| Breach abilities | Breach Gate, Breach Charge. | Valid only against walls, gates, and fortified targets. |
| Recon abilities | Drone Scan, Radar Ping, Harbor Scan. | Produces Intel or threat clarity, never auto-completes objectives. |
| Recovery abilities | Field Repair, Casualty Stabilize, Aid Convoy, Repair Convoy. | Converts Materials or OperationSupply into recovery through authored actions. |
| Strike abilities | Precision Strike, Naval Fire Support. | Requires visible targets and cannot bypass hidden objective reveal rules. |
| Production/control abilities | Produce Unit, Rally Order, Readiness Boost, Supply Drop. | Uses existing production, queue, Operation timer, and reward rules. |

## Ability Availability Matrix

Every ability entry in `BalanceConfigs/Combat_Balance_Config_v0_1.json` now has an `availability` block and an `implementationSpec` block. Implementation must treat this table as a human-readable index and the JSON as the source of truth.

| Ability Id | Unlock Moment | Availability Type | Runtime Owner | Main UI Surfaces |
|---|---|---|---|---|
| `ability.aid_convoy` | `persistent_operation.day1.opening_actions` | `operationActionUnlock` | `OperationActionService` | SCN-11_OperationDashboard, SCN-12_DistrictDetailActions, SCN-14_Store_CommandExchange |
| `ability.breach_charge` | `saga.ch03.m05.network_break.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `ability.casualty_stabilize` | `saga.ch02.m04.power_relay.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `ability.drone_scan` | `saga.ch03.m02.safehouse_sweep.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-06_MissionBriefing, SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD +1 |
| `ability.evacuation_corridor` | `saga.ch01.m04.airlift.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel +1 |
| `ability.field_repair` | `saga.ch02.m01.gridlock.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-12_DistrictDetailActions |
| `ability.harbor_patrol` | `saga.ch04.m04.grounded_signal.start` | `navalMissionCommand` | `NavalCommandSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `ability.harbor_scan` | `saga.ch04.m04.grounded_signal.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-06_MissionBriefing, SCN-08_RTSBattleHUD, POP-08_IntelReveal |
| `ability.naval_fire_support` | `saga.ch04.m05.armor_break.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `ability.operation_supply_route` | `saga.ch04.m04.grounded_signal.reward` | `operationActionUnlock` | `OperationActionService` | SCN-11_OperationDashboard, SCN-12_DistrictDetailActions |
| `ability.precision_strike` | `saga.ch04.m03.split_front.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `ability.produce_unit` | `saga.ch01.m02.establish_base.start` | `buildingAction` | `BuildingPlacementSystem` | SCN-09_BuildDrawerProduction, PREFAB-03_BuildDrawer |
| `ability.radar_ping` | `saga.ch01.m03.radar_warning.reward` | `rewardUnlockedSupport` | `ThreatWarningRuntimeState` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel +1 |
| `ability.rally_order` | `saga.ch05.m02.trust_under_fire.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel, SCN-19_Armory |
| `ability.readiness_boost` | `persistent_operation.day1.opening_actions` | `operationActionUnlock` | `OperationActionService` | SCN-11_OperationDashboard, SCN-12_DistrictDetailActions, SCN-14_Store_CommandExchange |
| `ability.repair_convoy` | `saga.ch02.m05.route_reopened.reward` | `operationActionUnlock` | `OperationActionService` | SCN-11_OperationDashboard, SCN-12_DistrictDetailActions, SCN-14_Store_CommandExchange |
| `ability.smoke_screen` | `saga.ch04.m02.steel_push.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `ability.supply_drop` | `saga.ch05.m04.last_corridor.reward` | `rewardUnlockedSupport` | `SupportAbilityService` | SCN-07_LoadoutSquadPrep, SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.attack` | `saga.ch01.m01.first_contact.start` | `defaultCommand` | `RTSSelectionSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.breach_gate` | `saga.ch01.m05.breach_assault.start` | `missionTeachingCommand` | `BaseBreachOrderSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.focus_fire` | `saga.ch01.m05.breach_assault.start` | `tacticalCommandUnlock` | `RTSSelectionSystem` | SCN-10_UnitCommandWheel |
| `skill.hold_position` | `saga.ch01.m03.radar_warning.start` | `tacticalCommandUnlock` | `RTSSelectionSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.load_transport` | `saga.ch01.m04.airlift.start` | `unitCapabilityCommand` | `RTSSelectionSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.move` | `saga.ch01.m01.first_contact.start` | `defaultCommand` | `RTSSelectionSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.rope_disembark` | `saga.ch01.m04.airlift.start` | `unitCapabilityCommand` | `TransportDisembarkSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.stop` | `saga.ch01.m01.first_contact.start` | `defaultCommand` | `RTSSelectionSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |
| `skill.unload_transport` | `saga.ch01.m04.airlift.start` | `unitCapabilityCommand` | `RTSSelectionSystem` | SCN-08_RTSBattleHUD, SCN-10_UnitCommandWheel |

Ability implementation fields required in JSON:

| Field | Implementation Use |
|---|---|
| `availability.unlockMoment` | Exact progression moment, profile gate, Operation gate, or AI profile gate that exposes the ability. |
| `availability.availableModes` | Modes allowed to show or execute the ability. |
| `availability.uiSurfaces` | Screens, HUDs, popups, or panels that may show the ability. |
| `availability.precondition` | Runtime condition checked before execution. |
| `availability.lockedOrDisabledState` | Player-facing locked/disabled reason used by UI. |
| `implementationSpec.runtimeOwner` | Runtime system or planned service that executes the command. |
| `implementationSpec.stateOwner` | Save/runtime state owner that stores ownership, charges, or command state. |
| `implementationSpec.validationTests` | Required tests for config shape and invalid target/cost handling. |

## Upgrade Availability Matrix

Every upgrade track in `BalanceConfigs/Combat_Balance_Config_v0_1.json` now has an `availability` block, an `implementationSpec` block, and a `resolvedItemIds` list. Player upgrades apply before mission launch or during Operation prep. Enemy escalation tracks apply only during encounter setup and stay hidden from player-facing upgrade UI.

| Upgrade Track Id | Unlock Moment | Availability Type | Apply Window | Resolved Items |
|---|---|---|---|---|
| `upgrade.air.drone_sensor` | `saga.ch03.m02.safehouse_sweep.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Drone |
| `upgrade.air.helicopter_avionics` | `saga.ch04.m01.air_corridor.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Helicopter_Attack, Unit_Veh_Helicopter_Attack_Small, Unit_Veh_Helicopter_Transport |
| `upgrade.air.jet_weapons` | `saga.ch04.m03.split_front.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Jet_01, Unit_Veh_Jet_02 |
| `upgrade.air.transport_aircraft` | `saga.ch01.m04.airlift.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Plane_Transport |
| `upgrade.building.air_operations` | `saga.ch04.m01.air_corridor.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_Airport, Building_Helipad, Building_WaterTank |
| `upgrade.building.armory_supply` | `saga.ch03.m03.false_front.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_Ammunition_Depot |
| `upgrade.building.base_defense` | `saga.ch01.m03.radar_warning.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_GuardTower, Building_GuardTower_Big, Building_Road_Barrier, Wall_Dirt_Straight +1 |
| `upgrade.building.civic_support` | `saga.ch02.m05.route_reopened.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_House_01, Building_Shop_01, Building_Shop_02 |
| `upgrade.building.command_post` | `saga.ch05.m02.trust_under_fire.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_CommandPost |
| `upgrade.building.infrastructure` | `saga.ch02.m01.gridlock.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_Hall_01 |
| `upgrade.building.medical_support` | `saga.ch02.m04.power_relay.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_MedicalStation |
| `upgrade.building.naval_operations` | `saga.ch04.m04.grounded_signal.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_Dock, Building_NavalYard |
| `upgrade.building.resource_chain` | `saga.ch02.m02.supply_line.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_Fuel_Bladder, Building_OilPump, Building_Refinery, Building_Refinery_Big |
| `upgrade.building.sensor_network` | `saga.ch01.m03.radar_warning.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_CoastalRadar, Building_Satelite_Dish |
| `upgrade.building.training_facilities` | `saga.ch01.m02.establish_base.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_Barrack, Tent_Contractor, Tent_Expert, Tent_Portaloo +2 |
| `upgrade.building.vehicle_workshop` | `saga.ch02.m03.sabotage_trace.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Building_FieldWorkshop |
| `upgrade.enemy.irregular_escalation` | `ai_profile.threat_family.irregular.enabled` | `enemyScalingOnly` | `EncounterSetupOnly` | Unit_Chr_Insurgent_Female_01, Unit_Chr_Insurgent_Female_02, Unit_Chr_Insurgent_Male_02, Unit_Chr_Insurgent_Male_03 +1 |
| `upgrade.enemy.rocketeer_escalation` | `ai_profile.threat_family.armored_column.enabled` | `enemyScalingOnly` | `EncounterSetupOnly` | Unit_Chr_Insurgent_Male_01 |
| `upgrade.enemy.sniper_escalation` | `ai_profile.threat_family.hidden_cell.enabled` | `enemyScalingOnly` | `EncounterSetupOnly` | Unit_Chr_Insurgent_Male_04 |
| `upgrade.sea.cutter_command` | `saga.ch04.m05.armor_break.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Sea_Coastal_Cutter |
| `upgrade.sea.drone_boat_sensor` | `saga.ch04.m04.grounded_signal.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Sea_Drone_Boat |
| `upgrade.sea.landing_craft_armor` | `saga.ch04.m05.armor_break.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Sea_Landing_Craft |
| `upgrade.sea.missile_fire_control` | `saga.ch05.m05.command_node.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Sea_Missile_Craft |
| `upgrade.sea.patrol_hull` | `saga.ch04.m04.grounded_signal.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Sea_Interceptor_Boat, Unit_Sea_Patrol_Boat |
| `upgrade.unit.aircrew_survival` | `saga.ch01.m04.airlift.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Pilot_Female_01, Unit_Chr_Pilot_Male_01 |
| `upgrade.unit.breacher_hazard` | `saga.ch03.m05.network_break.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Bombsuit_Male_01, Unit_Chr_Soldier_Female_02_Alt_02 |
| `upgrade.unit.civilian_safety` | `saga.ch02.m01.gridlock.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Civilian_Female_01, Unit_Chr_Civilian_Female_02, Unit_Chr_Civilian_Male_01, Unit_Chr_Civilian_Male_02 |
| `upgrade.unit.close_security` | `saga.ch01.m02.establish_base.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Soldier_Male_02_Alt_01, Unit_Chr_Soldier_Male_02_Alt_03 |
| `upgrade.unit.contractor_security` | `saga.ch01.m02.establish_base.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Contractor_Female_01, Unit_Chr_Contractor_Male_01, Unit_Chr_Contractor_Male_02 |
| `upgrade.unit.general_training` | `profile.level.2.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Leader_Male_01 |
| `upgrade.unit.marksman_recon` | `saga.ch03.m02.safehouse_sweep.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Soldier_Female_01, Unit_Chr_Soldier_Female_01_Alt_02, Unit_Chr_Soldier_Male_01_Alt_01 |
| `upgrade.unit.rifleline` | `saga.ch01.m02.establish_base.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Soldier_Female_01_Alt_01, Unit_Chr_Soldier_Female_02, Unit_Chr_Soldier_Female_02_Alt_01, Unit_Chr_Soldier_Male_01_Alt_02 +3 |
| `upgrade.unit.rocketeer_antiarmor` | `saga.ch01.m05.breach_assault.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Ghillie_Male_01 |
| `upgrade.unit.suppression_infantry` | `saga.ch01.m02.establish_base.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Chr_Soldier_Male_01 |
| `upgrade.vehicle.apc_armor` | `saga.ch01.m05.breach_assault.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_APC_Fast, Unit_Veh_APC_Heavy, Unit_Veh_APC_Slow |
| `upgrade.vehicle.armored_car` | `saga.ch04.m02.steel_push.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Light_Armored_Car |
| `upgrade.vehicle.battle_tank` | `saga.ch04.m02.steel_push.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Tank_USA |
| `upgrade.vehicle.logistics_trucks` | `saga.ch02.m02.supply_line.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Truck_Canopy, Unit_Veh_Truck_Tanker, Unit_Veh_Truck_Tray |
| `upgrade.vehicle.missile_fire_control` | `saga.ch04.m03.split_front.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Missle_Launcher_Air, Unit_Veh_Missle_Launcher_Ground |
| `upgrade.vehicle.radar_sensor` | `saga.ch01.m03.radar_warning.reward` | `playerUpgradeTrack` | `PreMissionLoadoutOrOperationPrep` | Unit_Veh_Radar_Tank |

Upgrade implementation fields required in JSON:

| Field | Implementation Use |
|---|---|
| `resolvedItemIds` | Concrete units/buildings affected by the track, derived from item `upgradeTrackId`. |
| `availability.unlockMoment` | Exact Saga/profile/Operation/AI moment that exposes the track. |
| `availability.sourceRewardTypes` | Reward/store sources allowed to grant parts or apply enemy scaling. |
| `availability.storeEligibility` | Store guardrail; player-facing upgrades allow parts only after the earn path unlocks. |
| `availability.applyWindow` | Runtime window for applying the tier. Active-combat mutation is blocked for player upgrades. |
| `availability.requiresProductionPath` | Implementation dependency such as existing tactical simulation or water-lane/naval pathing. |
| `implementationSpec.runtimeOwner` | `UpgradeService` for player upgrades, `AIEncounterScaler` for enemy tracks. |
| `implementationSpec.targetResolution` | How runtime resolves affected units/buildings. |
| `implementationSpec.validationTests` | Required tests for availability and apply-window enforcement. |

## Progression Rules

- Chapter 1 teaches infantry, basic build/produce, radar warning, transport, and first breach/vehicle roles.
- Chapter 2 expands infrastructure, logistics, repair, and city recovery.
- Chapter 3 expands Intel, hidden-network tools, drone scan, evidence, and investigation.
- Chapter 4 expands air, armor, naval/coastal pressure, and heavier support.
- Chapter 5 expands citywide command, mixed-arms mastery, and late-tier upgrade pressure.

Skirmish can expose wider catalogs through presets, but every preset must still reference valid catalog ids and balance tags. Internal config ids may continue to use QuickCustom naming until runtime migration.

Operations unlock catalog access through district state, outpost tier, armory tier, airfield tier, coastal tier, and Command Post progression.

## UI Surface Rules

- `SCN-19 Armory` is the owned roster and upgrade-track inspection screen for player-facing units, buildings, support abilities, BlueprintParts, and Gear Modules.
- `POP-09 Ability / Upgrade Detail` is the shared detail popup for any ability or upgrade track exposed from Mission Briefing, Loadout, RTS HUD, Command Wheel, Store, Reward Unlock, Intel Reveal, or Armory.
- Ability and upgrade `uiSurfaces` in the balance config list where an item is surfaced for selection, use, reward, store, or inspection. The detail popup is a secondary inspection route and must bind the same selected ability or upgrade id rather than inventing a separate catalog id.
- Store, Reward Unlock, and Armory must display the same target id, unlock moment, availability state, disabled reason, source reward type, and duplicate-conversion rule as the balance/economy docs.
- Active combat never applies permanent upgrade tiers. Upgrade CTAs remain disabled during active combat and route to Armory or POP-09 explanation instead.

## Reward And Store Rules

- `UnitUnlock`, `BuildingUnlock`, `SupportAbilityUnlock`, `GearModule`, and `BlueprintParts` target ids must exist in the balance config.
- Duplicate unlocks convert through explicit item-specific `BlueprintParts` rules from `Economy_Reward_Design.md`.
- Store items may grant resources, parts, cosmetics, support supplies, or fixed unlocks only when those items have a non-paid earn path.
- Store purchases cannot grant direct Operation metrics, Campaign stars, hidden objective completion, active-match combat cooldown removal, or direct tier application.

## Visual Rules

- Runtime unit/building art must align with `3D_SingleMap_Gameplay_Direction.md` and the visual config/prefab-catalog roster.
- Gameplay buildings are runtime entities on sockets. They are not baked into terrain.
- Building visuals need intact, damaged, and destroyed states that do not change gameplay footprint.
- Unit portraits, icons, world sprites, VFX, and audio cue ids live in the visual config, not in balance config.
- Upgrade tier badges must keep tier number/icon layers separate so UI can localize or restyle without regenerating balance data.

## Balance Validation Rules

Add data sanity tests before implementing the runtime upgrade service:

| Test | Required Behavior |
|---|---|
| `CombatBalanceConfig_AllIdsUnique` | Units, buildings, abilities, and upgrade tracks have unique ids. |
| `CombatBalanceConfig_AllVisualRefsExist` | Every balance entry has a matching visual config entry. |
| `CombatBalanceConfig_NoVisualPathsInBalanceData` | Balance config contains no world/icon/portrait asset paths. |
| `CombatVisualConfig_NoBalanceValues` | Visual config contains no costs, HP, damage, cooldown, range, production time, or upgrade cost values. |
| `CombatBalanceConfig_AllAbilityRefsExist` | Unit/building ability references point to valid ability ids. |
| `CombatBalanceConfig_AllUpgradeTrackRefsExist` | Unit/building upgrade tracks point to valid upgrade track ids. |
| `CombatAbilityConfig_AllAvailabilitySpecsComplete` | Every ability has unlock moment, modes, UI surfaces, precondition, locked/disabled state, runtime owner, state owner, and validation tests. |
| `CombatUpgradeTrackConfig_AllAvailabilitySpecsComplete` | Every upgrade track has unlock moment, source reward types, store eligibility, apply window, runtime owner, target resolution, and validation tests. |
| `CombatUpgradeTrackConfig_AllPlayerTracksResolveItems` | Every player-facing upgrade track resolves to one or more units/buildings. Enemy escalation tracks resolve enemy units only. |
| `CombatAbilityUpgradeUnlocks_AlignWithSagaOperationAndStoreRules` | Ability and upgrade unlock moments match Saga/Operation pacing and store parts remain gated by earn path. |
| `CombatBalanceConfig_ProducerRelationshipsValid` | Producer buildings and produced units exist and agree by id. |
| `CombatBalanceConfig_UpgradeCostsNonNegative` | Upgrade costs and modifiers are valid numeric data. |

Balance probes can then use catalog tags such as `CurrentRuntimeAnchor`, `DesignReady`, `NavalExpansion`, `CombatCatalog`, `BuildCatalog`, `UpgradeTrack`, and role tags to report army value, unlock pressure, and resource sinks.

## Design Recommendations

1. Implement the catalog loader before adding more mission data. Mission configs should reference catalog ids instead of hard-coded prefab names.
2. Add upgrade service, inventory state, and duplicate-parts conversion before shipping store bundles that mention upgrade parts.
3. Keep sea units design-ready but locked behind Chapter 4/coastal Operation content until water-lane metadata and naval pathing exist.
4. Treat civilians as protection/progression entities, not as combat power. Their upgrade track improves safety and evacuation outcomes.
5. Fix the missile range balance risk before late-game content uses missile launchers heavily. The config preserves current runtime values, but probes should flag extreme range pressure.
6. Use visual config entries as art-production work orders. An unproduced Unity asset is an implementation task, not a design gap, when the visual entry has a concrete path and art brief.

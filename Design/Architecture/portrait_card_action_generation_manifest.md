# Portrait Card/Action Generation Manifest

Workflow: `Design/Architecture/portrait_card_action_generation_workflow.md`

Status key:

- `[x]` Card and Action generated, imported as Sprite, inspected, and assigned to config.
- `[ ]` Pending secondary portrait generation or assignment. Remaining building/vehicle targets now require Card only.
- `[-]` Skipped because config has no base `portraitSprite`.
- `[!]` Blocked after repeated unrelated image-generation outputs; not assigned.

## Characters

- [x] `Prefab_UnitGrid_Chr_Bombsuit_Male_01_Config.asset` - Bomb Suit Specialist - weapon: none - Card/Action assigned
- [x] `Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset` - Civilian Female I - weapon: none - Card/Action assigned
- [x] `Prefab_UnitGrid_Chr_Civilian_Female_02_Config.asset` - Civilian Female II - weapon: none - Card/Action assigned
- [x] `Prefab_UnitGrid_Chr_Civilian_Male_01_Config.asset` - Civilian Male I - weapon: none - Card/Action assigned
- [x] `Prefab_UnitGrid_Chr_Civilian_Male_02_Config.asset` - Civilian Male II - weapon: none - Card/Action assigned; Action: crouched cover + water supply checkpoint
- [x] `Prefab_UnitGrid_Chr_Contractor_Female_01_Config.asset` - Security Contractor Female I - weapon: SMG - Card/Action assigned; Action: braced SMG at armored vehicle hangar doorway
- [x] `Prefab_UnitGrid_Chr_Contractor_Male_01_Config.asset` - Security Contractor Male I - weapon: Pistol - Card/Action assigned; Action: rooftop radio call + low-ready pistol
- [x] `Prefab_UnitGrid_Chr_Contractor_Male_02_Config.asset` - Security Contractor Male II - weapon: Rifle - Card/Action assigned; Action: kneeling rifle overwatch at refinery-yard sandbags
- [x] `Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset` - Ghillie Rocketeer - weapon: Rocket Launcher - Card/Action assigned; Action: crouched rocket ambush on desert ridge
- [x] `Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset` - Insurgent Rifleman Female I - weapon: Rifle - Card/Action assigned; Action: magazine check at barricade-line checkpoint
- [x] `Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset` - Insurgent Sidearm Fighter Female II - weapon: Pistol - Card/Action assigned; Action: fast pistol-ready turn at command-room doorway
- [x] `Prefab_UnitGrid_Chr_Insurgent_Male_01_Config.asset` - Insurgent Rocketeer Male I - weapon: RPG - Card/Action assigned; Action: low sprint with RPG across convoy-road culvert
- [x] `Prefab_UnitGrid_Chr_Insurgent_Male_02_Config.asset` - Insurgent Gunner Male II - weapon: Machine Gun - Card/Action assigned; Action: braced machine-gun support in repair bay
- [x] `Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset` - Insurgent Raider Male III - Card/Action assigned with neutral prompt wording; Action pose: kneeling crate cover, compact short rifle, market checkpoint
- [x] `Prefab_UnitGrid_Chr_Insurgent_Male_04_Config.asset` - Insurgent Sniper Male IV - Card/Action assigned with neutral prompt wording; Action pose: rooftop parapet brace, scoped long rifle
- [x] `Prefab_UnitGrid_Chr_Insurgent_Male_05_Config.asset` - Insurgent Rifleman Male V - Card/Action assigned with neutral prompt wording; Action pose: convoy-road barrier cover, rifle ready
- [x] `Prefab_UnitGrid_Chr_Leader_Male_01_Config.asset` - Field Commander - weapon: Metal Pistol - Card/Action assigned; Action: command-table brace in smoky operations bunker
- [x] `Prefab_UnitGrid_Chr_Pilot_Female_01_Config.asset` - Pilot Female I - weapon: Compact Pistol - Card/Action assigned; Action: helicopter-door crouch in rotor wash
- [x] `Prefab_UnitGrid_Chr_Pilot_Male_01_Config.asset` - Pilot Male I - weapon: Compact Pistol - Card/Action assigned; Action: cockpit-ladder climb on rainy flight deck
- [x] `Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_01_Config.asset` - Rifleman Female I - weapon: Rifle - Card/Action assigned; Action: kneeling checkpoint aim behind concrete barrier
- [x] `Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_02_Config.asset` - Marksman Female II - weapon: Sniper Rifle - Card/Action assigned; Action: prone rooftop overwatch at night
- [x] `Prefab_UnitGrid_Chr_Soldier_Female_01_Config.asset` - Marksman Female I - weapon: Sniper Rifle - Card/Action assigned; Action: seated ridge aim over convoy road
- [x] `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_01_Config.asset` - Rifleman Female I - weapon: Rifle - Card/Action assigned; Action: crouched supply-crate ready turn
- [x] `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset` - Assault Breacher Female II - weapon: SMG - Card/Action assigned; Action: concrete-doorway fast entry
- [x] `Prefab_UnitGrid_Chr_Soldier_Female_02_Config.asset` - Rifleman Female II - weapon: Rifle - Card/Action assigned; Action: low sprint through motor-pool lane
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_01_Config.asset` - Marksman Male I - weapon: Sniper Rifle - Card/Action assigned; Action: kneeling rooftop scoped overwatch
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_02_Config.asset` - Advanced Rifleman Male II - weapon: Advanced Rifle - Card/Action assigned; Action: braced hangar-crate aim
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset` - Heavy Gunner Male I - weapon: Machine Gun - Card/Action assigned
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_01_Config.asset` - Sidearm Specialist Male I - weapon: Metal Pistol - Card/Action assigned; Action: crouched checkpoint barrier aim
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_02_Config.asset` - Rifleman Male II - weapon: Rifle - Card/Action assigned; Action: braced convoy-door cover aim
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_03_Config.asset` - Sidearm Specialist Male III - weapon: Pistol - Card/Action assigned; Action: crouched market-stall reload
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset` - Rifleman Male IV - weapon: Rifle - Card/Action assigned; Action: kneeling hangar-crate doorway aim
- [x] `Prefab_UnitGrid_Chr_Soldier_Male_02_Config.asset` - Rifleman Male II - weapon: Rifle - Card/Action assigned; Action: refinery-yard radio advance

## Vehicles

- [x] `Prefab_UnitGrid_Veh_APC_Fast_Config.asset` - Fast APC - Card/Action assigned; Action: dusty checkpoint turn
- [x] `Prefab_UnitGrid_Veh_APC_Heavy_Config.asset` - Heavy APC - Card/Action assigned; Action: eight-wheel berm climb
- [x] `Prefab_UnitGrid_Veh_APC_Slow_Config.asset` - Armored APC - Card/Action assigned; Action: tracked repair-yard push
- [x] `Prefab_UnitGrid_Veh_Drone_Config.asset` - Recon Drone - Card/Action assigned; Action: fixed-wing ridge bank
- [x] `Prefab_UnitGrid_Veh_Helicopter_Attack_Config.asset` - Attack Helicopter - Card/Action assigned; Action: rotor-wash barricade bank
- [x] `Prefab_UnitGrid_Veh_Helicopter_Attack_Small_Config.asset` - Light Attack Helicopter - Card/Action assigned; Action: compact runway-apron low bank
- [x] `Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset` - Transport Helicopter - Card/Action assigned; Action: sling-load supply lift
- [x] `Prefab_UnitGrid_Veh_Jet_01_Config.asset` - Strike Jet - Card/Action assigned; Action: loaded runway takeoff bank
- [x] `Prefab_UnitGrid_Veh_Jet_02_Config.asset` - Fighter Jet - Card/Action assigned; Action: clean high desert turn
- [x] `Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset` - Light Armored Car - Card/Action assigned; Action: roof-gunner checkpoint road cut
- [x] `Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset` - Air Missile Launcher - Card/Action assigned; Action: air-defense rack deployment
- [x] `Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset` - Ground Missile Launcher - Card/Action assigned; Action: raised box-launcher firing position
- [x] `Prefab_UnitGrid_Veh_Plane_Transport_Config.asset` - Transport Plane - Card/Action assigned; Action: cargo aircraft climb-out
- [x] `Prefab_UnitGrid_Veh_Radar_Tank.asset` - Radar Tank - Card/Action assigned; Action: communications ridge dish scan
- [x] `Prefab_UnitGrid_Veh_Tank_USA_Config.asset` - Battle Tank - Card/Action assigned; Action: berm-crossing dust push
- [x] `Prefab_UnitGrid_Veh_Truck_Canopy.asset` - Canopy Truck - Card/Action assigned; Action: dusty supply-depot lane turn
- [x] `Prefab_UnitGrid_Veh_Truck_Tanker.asset` - Tanker Truck - Card/Action assigned; Action: dusty checkpoint service-lane turn
- [x] `Prefab_UnitGrid_Veh_Truck_Tray.asset` - Cargo Truck - Card/Action assigned; Action: supply-depot loading-yard maneuver

## Buildings

- [x] `Prefab_BuildingDefinition_Airport_Config.asset` - Airport - Card/Action assigned; Action: evening runway beacon scene
- [x] `Prefab_BuildingDefinition_Ammunition_Depot_Config.asset` - Ammunition Depot - Card/Action assigned; Action: night floodlit logistics yard
- [x] `Prefab_BuildingDefinition_Building_Barrack_Config.asset` - Barracks - Card/Action assigned; Action: early morning base activity
- [x] `Prefab_BuildingDefinition_Building_Satelite_Dish_Config.asset` - Satellite Dish - Card/Action assigned; Action: night communications ridge scan
- [x] `Prefab_BuildingDefinition_Fuel_Bladder_Config.asset` - Fuel Bladder - Card/Action assigned; Action: dusk field refueling setup
- [x] `Prefab_BuildingDefinition_GuardTower_Big_Config.asset` - Heavy Guard Tower - Card/Action assigned; Action: stormy floodlit perimeter watch
- [x] `Prefab_BuildingDefinition_GuardTower_Config.asset` - Guard Tower - Card/Action assigned; Action: windy dusk perimeter lookout
- [-] `Prefab_BuildingDefinition_Hall_Config.asset` - City Hall - skipped, no base portrait assigned
- [-] `Prefab_BuildingDefinition_Helipad_Config.asset` - Helipad - skipped, no base portrait assigned
- [x] `Prefab_BuildingDefinition_House_Config.asset` - House - Card/Action assigned; Action: golden-hour dusty neighborhood lane
- [x] `Prefab_BuildingDefinition_OilPump_Config.asset` - Oil Pump - Card/Action assigned; Action: dusk oil-field operation
- [x] `Prefab_BuildingDefinition_OilRefinery_Big_Config.asset` - Large Oil Refinery - Card/Action assigned; Action: night industrial processing yard
- [x] `Prefab_BuildingDefinition_OilRefinery_Config.asset` - Oil Refinery - Card/Action assigned; Action: golden-hour single-tank service scene
- [x] `Prefab_BuildingDefinition_Portaloo__Config.asset` - Portable Toilet - Card/Action assigned; Action: windy evening camp sanitation area
- [x] `Prefab_BuildingDefinition_Road_Barrier_Config.asset` - Road Barrier - Card/Action assigned; Action: night checkpoint lane
- [x] `Prefab_BuildingDefinition_Shop_Config.asset` - Shop - Card assigned; Card: market-lane storefront
- [x] `Prefab_BuildingDefinition_Tent_Contractor_Config.asset` - Contractor Tent - Card assigned; Card: repair-bay work-camp edge
- [x] `Prefab_BuildingDefinition_Tent_Expert_Config.asset` - Expert Tent - Card assigned; Card: command-area equipment edge
- [x] `Prefab_BuildingDefinition_Tent_Refugee_Config.asset` - Refugee Tent - Card assigned; Card: aid-tent lane shelter
- [x] `Prefab_BuildingDefinition_Tent_Regular_Config.asset` - Soldier Tent - Card assigned; Card: supply-depot military camp lane
- [x] `Prefab_BuildingDefinition_Wall_Dirt_Straight_Config.asset` - Dirt Wall - Card assigned; Card: desert checkpoint perimeter
- [x] `Prefab_BuildingDefinition_Wall_Fence_Straight_Config.asset` - Fence Wall - Card assigned; Card: controlled checkpoint perimeter
- [x] `Prefab_BuildingDefinition_WaterTank_Config.asset` - Water Tank - Card assigned; Card: desert utility yard

## Final Atlas

- [x] Create/update secondary portrait atlas after all non-skipped targets are complete. `Portraits_Secondary.spriteatlas` created with all secondary portrait sprites packable.

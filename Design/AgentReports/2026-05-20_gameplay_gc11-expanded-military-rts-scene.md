# GC11 Expanded Military RTS Scene

Lane: Gameplay
Task: Convert the approved expanded 2048 RTS blueprint into a Unity review scene with roads, walkable masks, multiple enemy camps, proof units, and no object placement on roads.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc11ExpandedRtsSceneBuilder.cs`
- `Assets/Game/Scenes/Generated/GC11_ExpandedMilitaryRts_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_topdown_layout_proof_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_rts_playable_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_city_start_route_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC11_ExpandedMilitaryRts_2048/gc11_enemy_camps_route_1920x1080.png`

Contracts touched: GC11 visual playable scene blockout contract from `Design/Blueprints/gc11_military_rts_2048_expanded_walkable_blueprint.svg`.
User-visible behavior: none in shipped flow; generated Unity scene and captures are available for visual review.
Validation run: Unity batchmode `WarlineCaptureGc11ExpandedRtsSceneBuilder.BuildGc11ExpandedMilitaryRts2048`.
Validation result: passed scene generation validation.
Known gaps: this is a readable first Unity blockout, not final art dressing. Roads/walkable masks are intentionally visible so layout can be reviewed before decoration.
Cross-lane impacts: Art/Design can now review Unity screenshots and approve whether to replace pads with richer Demo-authored modules.
Next recommended task: if GC11 layout is approved, replace pad-level buildings with richer Demo-scene clusters while preserving the same road/walkable masks.

Validation log:
- PASS: GC11 scene generated with multiple enemy camps, explicit empty roads/walkable masks, and object placement limited to pads.

Placement log:
- CityCore_House_01: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab at (-960, 900) rot=6 scale=2.2
- CityCore_Shop_06: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_06.prefab at (-790, 900) rot=-8 scale=2.2
- CityCore_House_05: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab at (-620, 720) rot=91 scale=2
- CityCore_Shop_08: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_08.prefab at (-170, 720) rot=0 scale=2.1
- CityCore_Shop_12: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_12.prefab at (-960, 350) rot=-4 scale=2.05
- CityMarket_Hall: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab at (-790, 350) rot=3 scale=2.1
- CityMarket_GasStation: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasStation_01.prefab at (-620, 350) rot=90 scale=1.9
- SouthTown_House_03: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab at (-960, -820) rot=0 scale=2.2
- SouthTown_Shop_03: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_03.prefab at (-790, -820) rot=-2 scale=2.2
- SouthTown_House_07: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_07.prefab at (-620, -820) rot=3 scale=2
- Airfield_Hangar: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab at (805, 740) rot=90 scale=2.3
- Airfield_ControlTower: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_ControlTower_01.prefab at (760, 540) rot=0 scale=1.9
- Airfield_TentNorth: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab at (550, 720) rot=0 scale=2.1
- Airfield_Jet: Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Jet_01.prefab at (880, 500) rot=14 scale=1.7
- Command_Barracks: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab at (390, 35) rot=-4 scale=2.2
- Command_GuardTower: Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GuardTower_01.prefab at (555, 50) rot=0 scale=1.8
- Command_RadarTank: Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Radar_Tank_01.prefab at (470, -80) rot=18 scale=1.75
- VehicleCamp_Tank: Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Tank_Russian_01.prefab at (600, -805) rot=-8 scale=1.9
- VehicleCamp_APC: Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_APC_01.prefab at (760, -805) rot=8 scale=1.9
- VehicleCamp_Fuel: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Fuel_Bladder_01.prefab at (900, -795) rot=0 scale=2.3
- VehicleCamp_PipelineTank: Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Tank_01.prefab at (705, -795) rot=0 scale=1.9
- Airfield_NorthBarrier_0: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (545, 815) rot=0 scale=1.55
- Vehicle_SouthBarrier_0: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (555, -880) rot=0 scale=1.55
- Airfield_NorthBarrier_1: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (607, 815) rot=0 scale=1.55
- Vehicle_SouthBarrier_1: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (617, -880) rot=0 scale=1.55
- Airfield_NorthBarrier_2: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (669, 815) rot=0 scale=1.55
- Vehicle_SouthBarrier_2: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (679, -880) rot=0 scale=1.55
- Airfield_NorthBarrier_3: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (731, 815) rot=0 scale=1.55
- Vehicle_SouthBarrier_3: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (741, -880) rot=0 scale=1.55
- Airfield_NorthBarrier_4: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (793, 815) rot=0 scale=1.55
- Vehicle_SouthBarrier_4: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (803, -880) rot=0 scale=1.55
- Airfield_NorthBarrier_5: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (855, 815) rot=0 scale=1.55
- Vehicle_SouthBarrier_5: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (865, -880) rot=0 scale=1.55
- Command_WestBarrier_0: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (300, -120) rot=90 scale=1.45
- Vehicle_EastBarrier_0: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (930, -850) rot=90 scale=1.45
- Command_WestBarrier_1: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (300, -58) rot=90 scale=1.45
- Vehicle_EastBarrier_1: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (930, -788) rot=90 scale=1.45
- Command_WestBarrier_2: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (300, 4) rot=90 scale=1.45
- Vehicle_EastBarrier_2: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (930, -726) rot=90 scale=1.45
- Command_WestBarrier_3: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (300, 66) rot=90 scale=1.45
- Vehicle_EastBarrier_3: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (930, -664) rot=90 scale=1.45
- Command_WestBarrier_4: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (300, 128) rot=90 scale=1.45
- Vehicle_EastBarrier_4: Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_02.prefab at (930, -602) rot=90 scale=1.45
- EdgeDune_NW: Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab at (-920, 920) rot=18 scale=2.6
- EdgeDune_SW: Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab at (-930, -925) rot=-22 scale=2.5
- EdgeDune_SE: Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab at (930, -930) rot=12 scale=2.4
- Player_Soldier_01: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab at (-820, -575) rot=48 scale=1.65
- Player_Soldier_02: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab at (-775, -545) rot=48 scale=1.65
- Player_Soldier_03: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab at (-730, -515) rot=48 scale=1.65
- MidRoute_Soldier: Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab at (-90, -235) rot=32 scale=1.65
- Enemy_Airfield_01: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab at (735, 360) rot=226 scale=1.65
- Enemy_Command_01: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab at (480, 0) rot=248 scale=1.65
- Enemy_VehicleCamp_01: Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab at (710, -585) rot=238 scale=1.65

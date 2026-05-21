# WarlineCapture Handoff - Gameplay 3D Scene Kit Audit

Date: 2026-05-19
Lane: Gameplay
Status: audit complete, no scene generation performed
Priority: exploratory scene assembly groundwork

## Lane

Gameplay

## Task

Review existing `Game_Legecy`, `Demo`, and available 3D model kit before any procedural scene generation.

## Files changed

- `Assets/Game/Scripts/Editor/WarlineCaptureSceneKitAudit.cs`
- `Design/AgentReports/2026-05-19_gameplay_3d-scene-kit-audit.md`
- `Design/AgentReports/Captures/scene_kit_audit.json`

## Scene Inventory

### `Assets/Game/Scenes/Demo.unity`

- Roots: 5
- GameObjects: 7360
- Renderers: 7337
- MeshFilters: 7298
- Animators: 174
- Cameras: 1
- Lights: 8
- Bounds center: (-84.505, 744.943, -109.332)
- Bounds size: (6862.285, 2342.085, 6123.991)
- Root samples: `Directional Light`, `Directional Light (1)`, `Global Volume`, `Main Camera`, `Scene`
- Top prefab references:
  - 370x `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Window_01.prefab`
  - 144x `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Window_02.prefab`
  - 105x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Corner_01.prefab`
  - 90x `Assets/Game/Prefabs/Environment/Decorations/SM_Env_Grass_01.prefab`
  - 81x `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_03.prefab`
  - 80x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Sidewalk_Straight_02.prefab`
  - 78x `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Door_01.prefab`
  - 78x `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Powerline_02.prefab`
  - 76x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Straight_01.prefab`
  - 75x `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Pipe_Small_Straight_01.prefab`
  - 72x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_01.prefab`
  - 72x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_Edge_01.prefab`

### `Assets/Game/Scenes/Game_Legecy.unity`

- Roots: 10
- GameObjects: 1191
- Renderers: 5
- MeshFilters: 2
- Animators: 73
- Cameras: 1
- Lights: 1
- Bounds center: (0, 744.943, 0)
- Bounds size: (10000, 2342.086, 10000)
- Root samples: `Bootstrap`, `Decorations`, `Directional Light (1)`, `EventSystem`, `GameSubScene`, `Global Volume`, `Ground`, `Main Camera`, `SM_Skydome_01`, `UI_Canvas`
- Top prefab references:
  - 22x `Assets/Synty/InterfaceMilitaryCombatHUD/Prefabs/Log/_Parts/HUD_MilitaryCombat_EventLog_Item_01_Kill_01.prefab`
  - 16x `Assets/Game/Prefabs/RankBadges/HUD_MilitaryCombat_Badge_Rank_01_Tier_01.prefab`
  - 7x `Assets/Game/Prefabs/UI/Parallax.prefab`

### `Assets/PolygonMilitary/Scenes/Demo.unity`

- Roots: 5
- GameObjects: 7360
- Renderers: 7337
- MeshFilters: 7298
- Animators: 174
- Cameras: 1
- Lights: 8
- Bounds center: (-84.505, 744.943, -109.332)
- Bounds size: (6862.285, 2342.085, 6123.991)
- Root samples: `Directional Light`, `Directional Light (1)`, `Global Volume`, `Main Camera`, `Scene`
- Top prefab references:
  - 370x `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Window_01.prefab`
  - 144x `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Window_02.prefab`
  - 105x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Corner_01.prefab`
  - 90x `Assets/Game/Prefabs/Environment/Decorations/SM_Env_Grass_01.prefab`
  - 81x `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_03.prefab`
  - 80x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Sidewalk_Straight_02.prefab`
  - 78x `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Door_01.prefab`
  - 78x `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Powerline_02.prefab`
  - 76x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Straight_01.prefab`
  - 75x `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Pipe_Small_Straight_01.prefab`
  - 72x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_01.prefab`
  - 72x `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_Edge_01.prefab`

## Prefab Kit Summary

- `building`: 187 prefabs
- `building_destroyed`: 45 prefabs
- `cover`: 44 prefabs
- `debris`: 45 prefabs
- `environment`: 121 prefabs
- `prop`: 509 prefabs
- `road`: 16 prefabs
- `soldier_or_weapon`: 14 prefabs
- `vehicle`: 102 prefabs
- `vehicle_destroyed`: 34 prefabs

## Largest Footprints By Role

### `building`
- `Assets/Game/Prefabs/Buildings/Building_Airport.prefab` size=(209.343, 30.389, 56.721) renderers=10 colliders=0 materials=4
- `Assets/Game/Prefabs/Buildings/Building_Refinery_Big.prefab` size=(52.151, 13.339, 40.11) renderers=41 colliders=0 materials=3
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasTower_01.prefab` size=(33.841, 10.972, 33.841) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab` size=(30.633, 12.519, 36.256) renderers=11 colliders=11 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_Open_01.prefab` size=(30.633, 12.519, 36.256) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Buildings/Building_Hall_01.prefab` size=(29.882, 22.471, 36.343) renderers=12 colliders=0 materials=7
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab` size=(22.469, 22.177, 35.005) renderers=4 colliders=1 materials=3
- `Assets/Game/Prefabs/Buildings/Building_Ammunition_Depot.prefab` size=(20.917, 6.295, 20.959) renderers=14 colliders=0 materials=2
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_02.prefab` size=(22.469, 22.177, 17.858) renderers=4 colliders=1 materials=3
- `Assets/Game/Prefabs/Buildings/Building_Refinery.prefab` size=(24.022, 15.098, 15.786) renderers=26 colliders=0 materials=3

### `building_destroyed`
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasTower_Destroyed_01.prefab` size=(35.28, 12.64, 34.111) renderers=1 colliders=5 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_Destroyed_01.prefab` size=(30.633, 12.59, 36.256) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_06_Destroyed.prefab` size=(21.467, 10.357, 12.781) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_08_Destroyed.prefab` size=(21.397, 8.504, 12.557) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_GasTower_Destroyed_02.prefab` size=(15.34, 12.256, 14.525) renderers=1 colliders=5 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Destroyed_01.prefab` size=(14.108, 10.053, 11.655) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_07_Destroyed.prefab` size=(15.2, 7.142, 9.192) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01_Destroyed.prefab` size=(9.102, 6.778, 12.84) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_11_Destroyed.prefab` size=(9.455, 10.05, 12.196) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_04_Destroyed.prefab` size=(9.478, 12.286, 12.121) renderers=1 colliders=1 materials=1

### `cover`
- `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Fence_Damaged_04.prefab` size=(11.673, 3.183, 2.92) renderers=1 colliders=10 materials=2
- `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_Tall_Group_03.prefab` size=(3.701, 3.273, 8.749) renderers=1 colliders=4 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Dirt_02.prefab` size=(12.386, 0.664, 2.413) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Group_04.prefab` size=(8.954, 2.194, 3.08) renderers=1 colliders=4 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Group_03.prefab` size=(8.954, 2.218, 3.08) renderers=1 colliders=4 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Group_02.prefab` size=(5.802, 2.218, 3.44) renderers=1 colliders=4 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Dirt_01.prefab` size=(6.555, 0.664, 2.407) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Gaspump_Cover_01.prefab` size=(5.811, 4.512, 2.427) renderers=1 colliders=4 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Row_03.prefab` size=(10.956, 1.053, 1.234) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Runway_Barrier_02.prefab` size=(1.03, 1.977, 10.752) renderers=1 colliders=2 materials=1

### `debris`
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_01.prefab` size=(8.111, 1.702, 3.799) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Crater_01.prefab` size=(5.011, 0.595, 4.775) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_03.prefab` size=(6.296, 1.645, 3.755) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_02.prefab` size=(6.734, 1.907, 2.719) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_04.prefab` size=(6.734, 1.907, 2.719) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_06.prefab` size=(3.153, 0.994, 2.948) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Crater_02.prefab` size=(2.882, 0.342, 2.916) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_05.prefab` size=(4.764, 0.779, 1.478) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_04.prefab` size=(3.038, 0.751, 2.294) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Rubbish_Pile_01.prefab` size=(2.825, 0.945, 2.238) renderers=1 colliders=1 materials=1

### `environment`
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab` size=(115.948, 7.208, 70.58) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Runway_01.prefab` size=(209.343, 1.485, 33.292) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab` size=(78.067, 7.422, 54.843) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_01.prefab` size=(63.711, 13.969, 64.377) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab` size=(72.558, 7.742, 55.675) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_05.prefab` size=(77.428, 11.12, 51.411) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_06.prefab` size=(60.927, 15.842, 62.815) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_02.prefab` size=(47.573, 11.091, 51.18) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_07.prefab` size=(47.573, 8.299, 51.18) renderers=1 colliders=1 materials=1
- `Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_04.prefab` size=(46.2, 12.49, 43.324) renderers=1 colliders=1 materials=1

### `prop`
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_SmokeStack_Background_01.prefab` size=(67.253, 56.592, 68.123) renderers=1 colliders=10 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Pipe_Large_Section_01.prefab` size=(4.876, 4.937, 60) renderers=1 colliders=10 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Fuel_Bladder_02.prefab` size=(9.038, 1.518, 13.213) renderers=1 colliders=5 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Fuel_Bladder_01.prefab` size=(9.038, 1.981, 13.068) renderers=1 colliders=5 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_SmokeStack_Platform_01.prefab` size=(10.436, 1.203, 10.436) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_SmokeStack_03.prefab` size=(10.005, 52.736, 10.005) renderers=1 colliders=2 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_Tank_01.prefab` size=(13.356, 24.085, 6.876) renderers=1 colliders=3 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipline_OilPump_01.prefab` size=(3.747, 7.621, 14.486) renderers=5 colliders=4 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_SmokeStack_Platform_02.prefab` size=(9.037, 1.203, 5.235) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Props/PipeLine/SM_Prop_Pipeline_SmokeStack_01.prefab` size=(6.077, 49.574, 6.077) renderers=1 colliders=2 materials=1

### `road`
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_01.prefab` size=(15.597, 1.102, 50) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_02.prefab` size=(15.597, 1.102, 25) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_End_01.prefab` size=(11.51, 0.563, 16.064) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Exit_01.prefab` size=(15.187, 1.366, 9.384) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Exit_02.prefab` size=(13.714, 1.366, 9.481) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Corner_01.prefab` size=(7.017, 0.414, 9.623) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Sidewalk_Corner_02.prefab` size=(5.057, 1.053, 5.029) renderers=1 colliders=2 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_Edge_01.prefab` size=(0.478, 0.666, 50) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Straight_01.prefab` size=(2.678, 0.313, 8.433) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Environment/SM_Env_DirtRoad_Slope_Up_01.prefab` size=(2.678, 0.982, 8.377) renderers=1 colliders=1 materials=1

### `soldier_or_weapon`
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierAssaultRifleF.prefab` size=(2.423, 1.903, 0.699) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierBazookaF.prefab` size=(2.423, 1.903, 0.699) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierDualGunF.prefab` size=(2.423, 1.903, 0.699) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierGunF.prefab` size=(2.423, 1.903, 0.699) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierRifleF.prefab` size=(2.423, 1.903, 0.699) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierAssaultRifleM.prefab` size=(2.517, 1.987, 0.648) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierBazookaM.prefab` size=(2.517, 1.987, 0.648) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierDualGunM.prefab` size=(2.517, 1.987, 0.648) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierGunM.prefab` size=(2.517, 1.987, 0.648) renderers=7 colliders=0 materials=2
- `Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierRifleM.prefab` size=(2.517, 1.987, 0.648) renderers=7 colliders=0 materials=2

### `vehicle`
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab` size=(56.434, 22.265, 58.704) renderers=22 colliders=0 materials=5
- `Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_TransportPlane_01.prefab` size=(47.028, 14.899, 48.889) renderers=19 colliders=19 materials=2
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab` size=(16.42, 5.768, 21.469) renderers=14 colliders=0 materials=5
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_02.prefab` size=(14.872, 11.462, 23.369) renderers=19 colliders=0 materials=5
- `Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Jet_02.prefab` size=(14.872, 6.349, 22.826) renderers=16 colliders=3 materials=2
- `Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Helicopter_Transport_01.prefab` size=(16.42, 5.505, 20.075) renderers=11 colliders=3 materials=2
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_01.prefab` size=(12.431, 6.913, 19.844) renderers=24 colliders=1 materials=5
- `Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Jet_01.prefab` size=(12.431, 5.697, 19.097) renderers=21 colliders=3 materials=2
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack.prefab` size=(13.629, 5.492, 17.08) renderers=15 colliders=0 materials=5
- `Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Helicopter_Attack_01.prefab` size=(13.629, 4.42, 17.063) renderers=12 colliders=3 materials=2

### `vehicle_destroyed`
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_TransportPlane_Destroyed_Fuselage_01.prefab` size=(45.694, 8.837, 24.554) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_TransportPlane_Destroyed_Tail_01.prefab` size=(16.217, 9.418, 25.962) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Jet_Destroyed_02.prefab` size=(8.334, 9.678, 22.991) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Jet_Destroyed_01.prefab` size=(9.598, 5.642, 19.844) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Helicopter_Transport_01_Destroyed.prefab` size=(8.8, 4.394, 17.939) renderers=1 colliders=5 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Helicopter_Attack_01_Destroyed.prefab` size=(9.569, 5.492, 16.341) renderers=1 colliders=6 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Drone_Destroyed_01.prefab` size=(12.927, 4.868, 11.595) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Helicopter_Transport_01_Destroyed_Front.prefab` size=(8.772, 3.527, 10.853) renderers=1 colliders=1 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Helicopter_Attack_02_Destroyed.prefab` size=(7.412, 4.009, 10.199) renderers=1 colliders=6 materials=1
- `Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Rocket_Truck_01_Destroyed.prefab` size=(7.119, 4.272, 10.489) renderers=1 colliders=2 materials=1

## Contracts touched

- None. This pass audits scene/model data only.

## User-visible behavior

- None. No runtime scene or prefab placement was changed.

## Validation run

- Unity editor asset/scene audit through `WarlineCaptureSceneKitAudit.Run`.

## Validation result

- Audited 3 scenes and 1117 prefabs.
- JSON output: `Design/AgentReports/Captures/scene_kit_audit.json`

## Known gaps

- This is a bounds/catalog pass. It does not yet generate screenshot contact sheets or classify road sockets.
- Mesh triangle counts are not included yet; Unity import mesh access needs a second pass.
- Scene composition quality has not been generated or judged yet.

## Cross-lane impacts

- Provides the asset catalog foundation needed before Designer/Gameplay procedural scene direction.

## Next recommended task

Generate visual contact sheets for the top building, road, vehicle, cover, debris, and soldier prefabs, then define road sockets and mission-layout grammar before creating any scene candidate.

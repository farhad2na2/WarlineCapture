# GC13 Blueprint-First RTS Scene

Lane: Gameplay
Task: Rebuild the 2048 RTS scene as a blueprint-first pass that follows the GC11 road, city, enemy airfield, command outpost, vehicle/fuel camp, spawn, and objective layout before visual dressing.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc13BlueprintRtsSceneBuilder.cs`
- `Assets/Game/Scenes/Generated/GC13_BlueprintRts_2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GC13_BlueprintRts_2048/gc13_prefab_footprint_catalog.json`
- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_topdown_blueprint_match_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_town_route_soldiers_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_base_route_soldiers_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_2048_coverage_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_dense_city_review_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC13_BlueprintRts_2048/gc13_rts_dense_base_review_1920x1080.png`

Contracts touched: GC11 expanded 2048 RTS blueprint layout contract.
User-visible behavior: none in shipped flow; generated scene is available for PM/gameplay review.
Validation run: Unity batchmode `WarlineCaptureGc13BlueprintRtsSceneBuilder.BuildGc13BlueprintRts2048`.
Validation result: passed blueprint-first road and footprint validation.
Known gaps: GC13 prioritizes blueprint match and walkability over beauty. It uses individual prefabs and legal dressing, so the next visual pass should replace simple lots with authored modules that preserve the same footprint contract.
Cross-lane impacts: PM/Design can review the workflow and proof captures; runtime ECS flow and UI are untouched.
Next recommended task: compare the GC13 top-down capture against the GC11 blueprint; if accepted, replace individual lot fillers with authored visual modules without moving the roads or gameplay masks.

Coverage metrics:
- walkable roads: 27.7%
- buildings/base structures: 1.5%
- blockers/decor/industrial: 0.6%
- spawns/objectives: 2.9%
- empty/unreserved desert: 67.4%
- measured prefab catalog entries: 51

Validation log:
- PASS: GC13 blueprint-first layout has no building/blocker overlap on walkable roads; spawns/objectives connect to the GC11 road contract; proof soldiers are on walkable streets.

Placement log:
- catalog: measured 51 prefab footprints before layout placement.
- building: CityCore_Hall at (-879.03, 0, 869.82) footprint=center=(-879.03, 869.82) size=(62, 46) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hall_01.prefab
- building: CityCore_CommandShop at (-616.7, 0, 874.43) footprint=center=(-616.7, 874.43) size=(78, 44) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_11.prefab
- building: CityCore_Block_01 at (-904.34, 0, 653.52) footprint=center=(-904.34, 653.52) size=(46, 42) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_01.prefab
- building: CityCore_Block_02 at (-681.13, 0, 651.22) footprint=center=(-681.13, 651.22) size=(54, 40) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab
- skipped building: CityCore_Block_03 touched walkable road and was omitted. footprint=center=(-455.62, 655.82) size=(52, 40)
- building: CivilMarket_West at (-879.03, 0, 207.1) footprint=center=(-879.03, 207.1) size=(54, 42) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_07.prefab
- building: CivilMarket_Mid at (-651.22, 0, 207.1) footprint=center=(-651.22, 207.1) size=(62, 42) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_09.prefab
- skipped building: CivilMarket_East touched walkable road and was omitted. footprint=center=(-416.5, 211.7) size=(54, 42)
- building: SouthTown_Block_01 at (-872.13, 0, -671.93) footprint=center=(-872.13, -671.93) size=(58, 46) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab
- building: SouthTown_Block_02 at (-600.59, 0, -669.63) footprint=center=(-600.59, -669.63) size=(72, 46) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_06.prefab
- skipped building: SouthTown_Block_03 touched walkable road and was omitted. footprint=center=(-326.76, -669.63) size=(58, 46)
- skipped building: MarketMid_ObjectiveBuilding touched walkable road and was omitted. footprint=center=(-50.62, 416.5) size=(90, 42)
- building: SouthObjective_Building at (-87.44, 0, -665.02) footprint=center=(-87.44, -665.02) size=(86, 42) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_04_Destroyed.prefab
- town detail: BlueprintTownDetail_00 at (-805.39, 0, 752.47) footprint=center=(-805.39, 752.47) size=(26, 24.4) asset=Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Shack_01.prefab
- skipped optional town detail: BlueprintTownDetail_01 touched walkable road and was omitted. footprint=center=(-734.06, 506.25) size=(26, 24)
- skipped optional town detail: BlueprintTownDetail_02 touched walkable road and was omitted. footprint=center=(-552.27, 508.55) size=(26, 24)
- skipped optional town detail: BlueprintTownDetail_03 touched walkable road and was omitted. footprint=center=(-329.06, 508.55) size=(26, 24.68)
- town detail: BlueprintTownDetail_04 at (-784.68, 0, 34.52) footprint=center=(-784.68, 34.52) size=(26, 24) asset=Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_01.prefab
- skipped optional town detail: BlueprintTownDetail_05 touched walkable road and was omitted. footprint=center=(-568.38, 34.52) size=(26, 24)
- skipped optional town detail: BlueprintTownDetail_06 touched walkable road and was omitted. footprint=center=(-324.46, 34.52) size=(26, 24.4)
- town detail: BlueprintTownDetail_07 at (-757.07, 0, -789.29) footprint=center=(-757.07, -789.29) size=(26, 24) asset=Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Shack_02.prefab
- town detail: BlueprintTownDetail_08 at (-462.53, 0, -789.29) footprint=center=(-462.53, -789.29) size=(26, 24) asset=Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_Well_01.prefab
- skipped optional town detail: BlueprintTownDetail_09 touched walkable road and was omitted. footprint=center=(85.14, 333.66) size=(26, 24.68)
- town detail: BlueprintTownDetail_10 at (-177.19, 0, 333.66) footprint=center=(-177.19, 333.66) size=(26, 24) asset=Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_01.prefab
- base: Airfield_Hangar at (510.85, 0, 740.96) footprint=center=(510.85, 740.96) size=(120, 78) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Hangar_01.prefab
- base: Airfield_ControlTower at (513.15, 0, 575.28) footprint=center=(513.15, 575.28) size=(56, 48) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_ControlTower_01.prefab
- base: Airfield_HQ at (706.44, 0, 200.2) footprint=center=(706.44, 200.2) size=(74, 52) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab
- skipped base: Command_HQ touched walkable road and was omitted. footprint=center=(317.56, 41.42) size=(76, 54)
- base: Command_Supply at (324.46, 0, -103.55) footprint=center=(324.46, -103.55) size=(88, 38) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Desert_01.prefab
- skipped base: VehicleCamp_WestPad touched walkable road and was omitted. footprint=center=(430.31, -467.13) size=(56, 42)
- skipped base: VehicleCamp_EastPad touched walkable road and was omitted. footprint=center=(623.6, -467.13) size=(56, 42)
- base: VehicleCamp_Barracks at (522.36, 0, -651.22) footprint=center=(522.36, -651.22) size=(124, 48) asset=Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab
- skipped base: FuelCamp_Utility touched walkable road and was omitted. footprint=center=(791.59, -531.56) size=(42, 112)
- base detail: BlueprintBaseDetail_00 at (379.69, 0, 835.31) footprint=center=(379.69, 835.31) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Wall_Fence_Straight.prefab
- base detail: BlueprintBaseDetail_01 at (623.6, 0, 812.3) footprint=center=(623.6, 812.3) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab
- base detail: BlueprintBaseDetail_02 at (453.32, 0, 287.64) footprint=center=(453.32, 287.64) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab
- base detail: BlueprintBaseDetail_03 at (471.73, 0, 218.61) footprint=center=(471.73, 218.61) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab
- base detail: BlueprintBaseDetail_04 at (154.18, 0, 204.8) footprint=center=(154.18, 204.8) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab
- base detail: BlueprintBaseDetail_05 at (471.73, 0, -154.18) footprint=center=(471.73, -154.18) size=(42, 42) asset=Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab
- skipped optional base detail: BlueprintBaseDetail_06 touched walkable road and was omitted. footprint=center=(278.44, -296.84) size=(34, 34)
- base detail: BlueprintBaseDetail_07 at (816.9, 0, -246.22) footprint=center=(816.9, -246.22) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab
- base detail: BlueprintBaseDetail_08 at (839.91, 0, -849.11) footprint=center=(839.91, -849.11) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab
- base detail: BlueprintBaseDetail_09 at (688.04, 0, 342.87) footprint=center=(688.04, 342.87) size=(34, 34) asset=Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab
- dressing: NorthWest_NoBuildRock at (-897.44, 0, 936.56) footprint=center=(-897.44, 936.56) size=(76, 58) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_05.prefab
- dressing: SouthWest_NoBuildRock at (-876.73, 0, -862.92) footprint=center=(-876.73, -862.92) size=(78, 70) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_03.prefab
- dressing: NorthEast_NoBuildRock at (950.36, 0, 890.53) footprint=center=(950.36, 890.53) size=(72, 60) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab
- dressing: SouthEast_NoBuildRock at (908.94, 0, -913.55) footprint=center=(908.94, -913.55) size=(78, 64) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_06.prefab
- skipped optional dressing: MidWalk_RockCluster_01 touched walkable road and was omitted. footprint=center=(57.53, -299.15) size=(50, 44)
- dressing: AirfieldEdge_RockCluster at (683.43, 0, 287.64) footprint=center=(683.43, 287.64) size=(46, 38) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_01.prefab
- dressing: WalkAreaPalmEdge_00 at (-945.76, 0, -172.58) footprint=center=(-945.76, -172.58) size=(24, 24) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab
- skipped optional dressing: WalkAreaPalmEdge_01 touched walkable road and was omitted. footprint=center=(-729.46, -89.74) size=(24, 24)
- dressing: WalkAreaPalmEdge_02 at (-531.56, 0, -140.37) footprint=center=(-531.56, -140.37) size=(24, 24) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab
- dressing: WalkAreaPalmEdge_03 at (-232.41, 0, -94.35) footprint=center=(-232.41, -94.35) size=(24, 24) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_02.prefab
- skipped optional dressing: WalkAreaPalmEdge_04 touched walkable road and was omitted. footprint=center=(126.56, -2.3) size=(24, 24)
- skipped optional dressing: WalkAreaPalmEdge_05 touched walkable road and was omitted. footprint=center=(246.22, 411.9) size=(24, 24)
- dressing: WalkAreaPalmEdge_06 at (513.15, 0, 200.2) footprint=center=(513.15, 200.2) size=(24, 24) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab
- skipped optional dressing: WalkAreaPalmEdge_07 touched walkable road and was omitted. footprint=center=(388.89, -342.87) size=(24, 24)
- dressing: WalkAreaPalmEdge_08 at (724.85, 0, -278.44) footprint=center=(724.85, -278.44) size=(24, 24) asset=Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab
- skipped optional dressing: WalkAreaPalmEdge_09 touched walkable road and was omitted. footprint=center=(853.72, -430.31) size=(24, 24)
- player soldier 1: placed on walkable street at (-862.92, 0, -453.32)
- player soldier 2: placed on walkable street at (-835.31, 0, -430.31)
- player soldier 3: placed on walkable street at (-835.31, 0, -476.33)
- player soldier 4: placed on walkable street at (-807.69, 0, -453.32)
- enemy soldier 1: placed on walkable street at (798.49, 0, 462.53)
- enemy soldier 2: placed on walkable street at (347.47, 0, 34.52)
- enemy soldier 3: placed on walkable street at (595.99, 0, -467.13)
- enemy soldier 4: placed on walkable street at (623.6, 0, -499.34)

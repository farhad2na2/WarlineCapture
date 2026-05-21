# GC30 Demo2 Road Green Terrain Scene

Lane: Gameplay
Task: Build a green/dirt terrain RTS visual scene using Demo2 asphalt road combination prefabs for the road network, authored Demo military/city modules between roads, and off-road decoration that stays out of road masks.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc30Demo2RoadGreenTerrainBuilder.cs`
- `Assets/Game/Scenes/Generated/GC30_Demo2RoadGreenTerrain_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_topdown_blueprint_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_south_base_1920x1080.png`

Contracts touched: GC30 generated visual contract only. Demo modules and Demo2 asphalt road prefabs are visual dressing only and are legalized against road/spawn/objective masks.
User-visible behavior: no shipped runtime behavior changed; generated scene and captures are available for visual review.
Validation run: Unity batchmode `WarlineCaptureGc30Demo2RoadGreenTerrainBuilder.BuildGc30Demo2RoadGreenTerrain2048`.
Validation result: passed generation validation.
Known gaps: This is still a visual proof scene. It does not yet convert the layout to ECS/pathfinding data. Demo2 asphalt road prefabs are placed by measured source-scale bounds and use controlled road/edge fallback materials so missing Demo2 road-marking materials do not render magenta in captures.
Cross-lane impacts: Art/Design can review the authored-module composition before Gameplay locks any movement grid or ECS conversion.
Next recommended task: visual review of GC30 captures; if accepted, promote this authored-module plus measured-road workflow into the reusable scene-generation contract.

Source roots scanned: 14451
Source roots cloned: 14451
Accepted authored roots: 13597
Skipped road/reserved roots: 854
Demo2 ground prefab pieces: 98
Demo2 road prefab pieces: 976
Demo2 off-road decorations: 12

Module placement:
- CityCore_NorthWestBlock: sourceRoots=2892, target=(-678.83, 0, 678.83), rotationY=2, scale=1
- CityMarket_WestBlock: sourceRoots=2892, target=(-637.41, 0, 154.18), rotationY=-7, scale=1
- SouthTown_WestBlock: sourceRoots=2892, target=(-632.81, 0, -480.93), rotationY=8, scale=1
- SouthGate_PlayerCamp: sourceRoots=2455, target=(-126.56, 0, -572.98), rotationY=12, scale=1
- CentralTentBarracks_InnerBlock: sourceRoots=832, target=(-94.35, 0, -121.96), rotationY=-4, scale=1
- CommandDepot_CentralEast: sourceRoots=976, target=(421.11, 0, 57.53), rotationY=-10, scale=1
- Airfield_NorthEastApron: sourceRoots=765, target=(642.01, 0, 632.81), rotationY=-8, scale=1
- VehicleYard_SouthEast: sourceRoots=581, target=(549.97, 0, -536.16), rotationY=18, scale=1
- FuelUtility_EastService: sourceRoots=166, target=(793.89, 0, -329.06), rotationY=-52, scale=1

Validation log:
- PASS: GC30 placed 13597 source-scale Demo-authored roots around a green/dirt terrain contract, added 98 Demo2 grass/circle ground prefab pieces, 976 measured Demo2 road prefab pieces and 12 off-road decorations, with 854 illegal road/reserved roots omitted.

Build log:
- RockNorthCommand_A: skipped terrain dressing because it overlaps road/reserved masks.
- RockNorthCommand_B: skipped terrain dressing because it overlaps road/reserved masks.
- TreeMarketGap_A: skipped terrain dressing because it overlaps road/reserved masks.
- TreeSouthGap_A: skipped terrain dressing because it overlaps road/reserved masks.
- MountainSouthGap_A: skipped terrain dressing because it overlaps road/reserved masks.
- GrassPatchNorth_A: skipped terrain dressing because it overlaps road/reserved masks.
- CityCore_NorthWestBlock: accepted 2866/2892 roots at blueprint/world (-678.83, 0, 678.83), skipped 26 by road/reserved/overlap legality.
- CityMarket_WestBlock: accepted 2861/2892 roots at blueprint/world (-637.41, 0, 154.18), skipped 31 by road/reserved/overlap legality.
- SouthTown_WestBlock: accepted 2866/2892 roots at blueprint/world (-632.81, 0, -480.93), skipped 26 by road/reserved/overlap legality.
- SouthGate_PlayerCamp: accepted 2348/2455 roots at blueprint/world (-126.56, 0, -572.98), skipped 107 by road/reserved/overlap legality.
- CentralTentBarracks_InnerBlock: accepted 540/832 roots at blueprint/world (-94.35, 0, -121.96), skipped 292 by road/reserved/overlap legality.
- CommandDepot_CentralEast: accepted 604/976 roots at blueprint/world (421.11, 0, 57.53), skipped 372 by road/reserved/overlap legality.
- Airfield_NorthEastApron: accepted 765/765 roots at blueprint/world (642.01, 0, 632.81), skipped 0 by road/reserved/overlap legality.
- VehicleYard_SouthEast: accepted 581/581 roots at blueprint/world (549.97, 0, -536.16), skipped 0 by road/reserved/overlap legality.
- FuelUtility_EastService: accepted 166/166 roots at blueprint/world (793.89, 0, -329.06), skipped 0 by road/reserved/overlap legality.

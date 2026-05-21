# GC15 Demo Cluster RTS Scene

Lane: Gameplay
Task: Continue from GC14 by placing authored Demo-scene visual modules/building clusters around the 2048 road contract, without random individual-prefab scattering.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc15DemoClusterRtsSceneBuilder.cs`
- `Assets/Game/Scenes/Generated/GC15_DemoClusterRts_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC15_DemoClusterRts_2048/gc15_topdown_blueprint_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC15_DemoClusterRts_2048/gc15_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC15_DemoClusterRts_2048/gc15_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC15_DemoClusterRts_2048/gc15_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC15_DemoClusterRts_2048/gc15_rts_south_base_1920x1080.png`

Contracts touched: GC14/GC11 2048 road, spawn, and objective masks. Demo modules are visual dressing only and are legalized against those masks.
User-visible behavior: no shipped runtime behavior changed; generated scene and captures are available for visual review.
Validation run: Unity batchmode `WarlineCaptureGc15DemoClusterRtsSceneBuilder.BuildGc15DemoClusterRts2048`.
Validation result: passed generation validation.
Known gaps: This is still a visual proof scene. It does not yet convert the layout to ECS/pathfinding data, and visual acceptance requires PM/user review of the attached captures.
Cross-lane impacts: Art/Design can now review composed Demo-cluster placement against the blueprint before Gameplay locks this into ECS walkability.
Next recommended task: visual review of GC15 captures; if accepted, promote the road/reserved masks and cluster exclusion data into a reusable scene-generation contract.

Source roots scanned: 18756
Source roots cloned: 18756
Accepted authored roots: 17922
Skipped road/reserved roots: 834

Module placement:
- CityCore_NorthWestBlock: sourceRoots=2892, target=(-803.09, 0, 738.66), rotationY=2, scale=1
- CityCentral_WestInnerBlock: sourceRoots=2892, target=(-204.8, 0, 711.05), rotationY=-6, scale=0.62
- CityMarket_WestBlock: sourceRoots=2892, target=(-632.81, 0, 172.58), rotationY=-9, scale=0.92
- CitySouth_WestBlock: sourceRoots=2892, target=(-729.46, 0, -729.46), rotationY=7, scale=0.96
- SouthGate_PlayerCamp: sourceRoots=2455, target=(-803.09, 0, -642.01), rotationY=34, scale=0.78
- CentralTentBarracks_InnerBlock: sourceRoots=832, target=(-227.81, 0, -227.81), rotationY=-4, scale=0.74
- WestTentBarracks: sourceRoots=832, target=(-103.55, 0, -734.06), rotationY=-4, scale=0.92
- Airfield_NorthEastApron: sourceRoots=765, target=(724.85, 0, 623.6), rotationY=-12, scale=1.12
- CommandDepot_CentralEast: sourceRoots=976, target=(347.47, 0, -154.18), rotationY=-20, scale=0.78
- VehicleYard_NorthEastLot: sourceRoots=581, target=(416.5, 0, 292.24), rotationY=132, scale=0.78
- VehicleYard_SouthEast: sourceRoots=581, target=(595.99, 0, -595.99), rotationY=132, scale=1.04
- FuelUtility_EastService: sourceRoots=166, target=(775.48, 0, -342.87), rotationY=-62, scale=0.82

Validation log:
- PASS: GC15 placed 17922 Demo-authored roots around the GC14 road contract, with 834 illegal road/reserved roots omitted.

Build log:
- CityCore_NorthWestBlock: accepted 2866/2892 roots at blueprint/world (-803.09, 0, 738.66), skipped 26 by road/reserved/overlap legality.
- CityCentral_WestInnerBlock: accepted 2866/2892 roots at blueprint/world (-204.8, 0, 711.05), skipped 26 by road/reserved/overlap legality.
- CityMarket_WestBlock: accepted 2862/2892 roots at blueprint/world (-632.81, 0, 172.58), skipped 30 by road/reserved/overlap legality.
- CitySouth_WestBlock: accepted 2866/2892 roots at blueprint/world (-729.46, 0, -729.46), skipped 26 by road/reserved/overlap legality.
- SouthGate_PlayerCamp: accepted 2429/2455 roots at blueprint/world (-803.09, 0, -642.01), skipped 26 by road/reserved/overlap legality.
- CentralTentBarracks_InnerBlock: accepted 812/832 roots at blueprint/world (-227.81, 0, -227.81), skipped 20 by road/reserved/overlap legality.
- WestTentBarracks: accepted 377/832 roots at blueprint/world (-103.55, 0, -734.06), skipped 455 by road/reserved/overlap legality.
- Airfield_NorthEastApron: accepted 728/765 roots at blueprint/world (724.85, 0, 623.6), skipped 37 by road/reserved/overlap legality.
- CommandDepot_CentralEast: accepted 962/976 roots at blueprint/world (347.47, 0, -154.18), skipped 14 by road/reserved/overlap legality.
- VehicleYard_NorthEastLot: accepted 580/581 roots at blueprint/world (416.5, 0, 292.24), skipped 1 by road/reserved/overlap legality.
- VehicleYard_SouthEast: accepted 561/581 roots at blueprint/world (595.99, 0, -595.99), skipped 20 by road/reserved/overlap legality.
- FuelUtility_EastService: accepted 13/166 roots at blueprint/world (775.48, 0, -342.87), skipped 153 by road/reserved/overlap legality.

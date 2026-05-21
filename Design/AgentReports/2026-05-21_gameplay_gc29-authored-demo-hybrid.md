# GC29 Authored Demo Hybrid Scene

Lane: Gameplay
Task: Build a higher-quality RTS visual scene from authored Demo military/city modules and Demo2-style continuous ground/road surfaces, avoiding individual prefab scale guessing.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc29AuthoredDemoHybridSceneBuilder.cs`
- `Assets/Game/Scenes/Generated/GC29_AuthoredDemoHybrid_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC29_AuthoredDemoHybrid_2048/gc29_topdown_blueprint_proof_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC29_AuthoredDemoHybrid_2048/gc29_rts_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC29_AuthoredDemoHybrid_2048/gc29_rts_city_readable_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC29_AuthoredDemoHybrid_2048/gc29_rts_airfield_command_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC29_AuthoredDemoHybrid_2048/gc29_rts_south_base_1920x1080.png`

Contracts touched: GC29 generated visual contract only. Demo modules are visual dressing only and are legalized against road/spawn/objective masks.
User-visible behavior: no shipped runtime behavior changed; generated scene and captures are available for visual review.
Validation run: Unity batchmode `WarlineCaptureGc29AuthoredDemoHybridSceneBuilder.BuildGc29AuthoredDemoHybrid2048`.
Validation result: passed generation validation.
Known gaps: This is still a visual proof scene. It does not yet convert the layout to ECS/pathfinding data. Demo2 road/ground is represented as continuous flat surfaces using the Demo2 visual language because isolated Demo2 road prefabs previously produced broken material/scale results in generated captures.
Cross-lane impacts: Art/Design can review the authored-module composition before Gameplay locks any movement grid or ECS conversion.
Next recommended task: visual review of GC29 captures; if accepted, promote this authored-module workflow into the reusable scene-generation contract.

Source roots scanned: 14451
Source roots cloned: 14451
Accepted authored roots: 13597
Skipped road/reserved roots: 854

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
- PASS: GC29 placed 13597 source-scale Demo-authored roots around a Demo2-style road/ground contract, with 854 illegal road/reserved roots omitted.

Build log:
- CityCore_NorthWestBlock: accepted 2866/2892 roots at blueprint/world (-678.83, 0, 678.83), skipped 26 by road/reserved/overlap legality.
- CityMarket_WestBlock: accepted 2861/2892 roots at blueprint/world (-637.41, 0, 154.18), skipped 31 by road/reserved/overlap legality.
- SouthTown_WestBlock: accepted 2866/2892 roots at blueprint/world (-632.81, 0, -480.93), skipped 26 by road/reserved/overlap legality.
- SouthGate_PlayerCamp: accepted 2348/2455 roots at blueprint/world (-126.56, 0, -572.98), skipped 107 by road/reserved/overlap legality.
- CentralTentBarracks_InnerBlock: accepted 540/832 roots at blueprint/world (-94.35, 0, -121.96), skipped 292 by road/reserved/overlap legality.
- CommandDepot_CentralEast: accepted 604/976 roots at blueprint/world (421.11, 0, 57.53), skipped 372 by road/reserved/overlap legality.
- Airfield_NorthEastApron: accepted 765/765 roots at blueprint/world (642.01, 0, 632.81), skipped 0 by road/reserved/overlap legality.
- VehicleYard_SouthEast: accepted 581/581 roots at blueprint/world (549.97, 0, -536.16), skipped 0 by road/reserved/overlap legality.
- FuelUtility_EastService: accepted 166/166 roots at blueprint/world (793.89, 0, -329.06), skipped 0 by road/reserved/overlap legality.

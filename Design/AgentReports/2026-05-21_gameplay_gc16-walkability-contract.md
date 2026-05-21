# GC16 Walkability Contract

Lane: Gameplay
Task: Define the gameplay walkability contract missing from GC15: macro roads, local city/camp soldier walkable areas, vehicle walkable pockets, blocker masks, connection points, and sample routes.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc16WalkabilityContractBuilder.cs`
- `Assets/Game/Scenes/Generated/GC16_WalkabilityContract_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048/gc16_topdown_walkability_contract_2048x2048.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048/gc16_city_walkability_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048/gc16_base_walkability_close_1920x1080.png`

Contracts touched: new GC16 walkability layers on top of the GC14/GC15 macro layout: MacroRoadWalkable, CityLocalWalkable, SoldierBlocker, VehicleWalkable, VehicleBlocker, ConnectionPoint, SampleRoute.
User-visible behavior: no shipped runtime behavior changed; generated proof scene shows where soldiers/vehicles may move inside cities/camps.
Validation run: Unity batchmode `WarlineCaptureGc16WalkabilityContractBuilder.BuildGc16WalkabilityContract2048`.
Validation result: passed walkability contract validation.
Known gaps: contract proof only; it still needs export into runtime ECS/pathfinding data and reconciliation against final accepted visual clusters.
Cross-lane impacts: Design/Art must keep authored visual clusters compatible with these local walkable corridors and blocker footprints.
Next recommended task: convert the accepted GC16 zones into a reusable data asset and make GC17 generate visual clusters around these masks instead of after-the-fact fitting.

Layer counts:
- MacroRoadWalkable: 5
- CityLocalWalkable/SoldierWalkable: 28
- VehicleWalkable: 11
- SoldierBlocker: 32
- VehicleBlocker: 8
- ConnectionPoint: 12
- Soldier sample routes: 3
- Vehicle sample routes: 1

Validation log:
- PASS: GC16 defines 5 macro roads, 28 soldier walk zones, 11 vehicle zones, 40 blocker zones, and 12 road-to-local connection points.

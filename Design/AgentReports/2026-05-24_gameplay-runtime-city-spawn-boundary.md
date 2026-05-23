# WarlineCapture Handoff

Lane: Gameplay

Task: Step 2 architecture migration: move RuntimeCitySpawnerSystem off direct BuildingPlacementSystem facade calls for generated city building spawn/delete work.

Files changed:
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeCitySpawnSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeCitySpawnSystem.cs.meta
- Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/Chapter01TacticalRuntimeBindingTests.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay-runtime-city-spawn-boundary.md

Contracts touched:
- Added BuildingRuntimeCitySpawnSystem as the runtime city generated building spawn/delete/deferred-side-effect boundary.
- RuntimeCitySpawnerSystem now receives BuildingRuntimeCitySpawnSystem through gameplay feature composition and no longer names or calls BuildingPlacementSystem.
- BuildingPlacementSystem exposes only an internal composition/context seam for city spawn until the wider facade is retired.
- GameplayArchitectureContractTests now prevents RuntimeCitySpawnerSystem from drifting back to BuildingPlacementSystem or _buildingPlacement facade references.
- gameplay_solid_ecs_contract.md now records BuildingRuntimeCitySpawnSystem ownership for generated city building spawn/delete/deferred-side-effect bridging.

User-visible behavior:
- No intended gameplay or visual behavior change.
- Runtime city generation still uses synchronous spawn results so existing footprint reservation, overlap rejection, and cleanup behavior remain stable.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-cityspawn-architecture.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter Chapter01TacticalRuntimeBindingTests.RuntimeCitySpawner_DoesNotMutateRoadCellsForM01FixedTacticalMission -logFile /private/tmp/warlinecapture-cityspawn-m01.log
- git diff --check

Validation result:
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151826650766450.xml.
- Passed. Chapter01TacticalRuntimeBindingTests.RuntimeCitySpawner_DoesNotMutateRoadCellsForM01FixedTacticalMission reported 1 total, 1 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151826999390240.xml.
- Passed. git diff --check reported no whitespace errors.

Known gaps:
- RuntimeCitySpawnerSystem is off direct BuildingPlacementSystem calls, but city generation is not yet converted to asynchronous BuildingRuntimeSpawnRequest buffers because it requires immediate actual-origin/footprint results and sometimes deletes corrected placements that violate city reservations.
- BuildingPlacementSystem still supplies the internal runtime city spawn context while runtime building spawn/deletion internals continue to be split out.
- CitizenPopulationSystem still depends on BuildingPlacementSystem for building queries and should be migrated by its own focused slice.

Cross-lane impacts:
- Gameplay/architecture can continue facade retirement without breaking runtime city generation.
- QA should expect no city layout behavior changes from this slice.

Next recommended task:
- Migrate CitizenPopulationSystem building read paths off BuildingPlacementSystem onto BuildingRuntimeQuerySystem/ECS read models, or add an ECS runtime building delete/request result contract before converting RuntimeCitySpawnerSystem to fully asynchronous BuildingRuntimeSpawnRequest buffers.

# WarlineCapture Handoff

Lane: Gameplay

Task: Migrate InitialUnitsSpawnSystem away from BuildingPlacementRuntimeComponent and onto the ECS building runtime boundary request/read-model path.

Files changed:
- Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs
- Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-24_gameplay-initial-spawn-boundary-migration.md

Contracts touched:
- InitialUnitsSpawnSystem now requires BuildingRuntimeBoundaryTag plus BuildingConfiguredSpawnableReadModel and BuildingRuntimeSpawnRequest buffers instead of BuildingPlacementRuntimeComponent.
- BuildingConfiguredSpawnableReadModel now publishes footprint cells so initial base layout can calculate gate/wall/building placement without managed placement facade calls.
- BuildingRuntimeSpawnRequest now carries request kind, end origin, wall-overlap policy, and spawned count so the boundary can process building, wall-run, and wall-segment requests.
- InitialUnitsSpawnProgress now separates initial resource application, initial building request issue state, and initial building completion state.
- GameplayArchitectureContractTests now block InitialUnitsSpawnSystem from reintroducing BuildingPlacementRuntimeComponent, BuildingPlacementSystem, buildingPlacement, or TrySpawnRuntimeBuilding.

User-visible behavior:
- Initial faction bases and configured initial buildings are now requested through the ECS boundary.
- Initial unit spawning waits for required initial building requests to complete before placing units, avoiding units spawning against an unfinished base perimeter.
- No intended visual or balance change.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-initialspawn-architecture-tests.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -logFile /private/tmp/warlinecapture-initialspawn-boundary-tests.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter InitialFactionBaseValidationTests -logFile /private/tmp/warlinecapture-initialspawn-base-tests.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BaseBreachValidationTests -logFile /private/tmp/warlinecapture-initialspawn-basebreach-tests.log
- git diff --check

Validation result:
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151790766641720.xml.
- Passed. BuildingRuntimeBoundaryValidationTests reported 1 total, 1 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151791047224960.xml.
- Passed. InitialFactionBaseValidationTests reported 7 total, 7 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151791410534670.xml.
- Passed. BaseBreachValidationTests reported 15 total, 15 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151791772122380.xml.
- Passed. git diff --check reported no whitespace errors.

Known gaps:
- BuildingPlacementRuntimeComponent still exists for remaining bootstrap/runtime consumers; this task only removed InitialUnitsSpawnSystem from that bridge.
- Initial air platform spawn selection now falls back to generic air spawn because there is no ECS production spawn-point/read-model buffer yet.
- RuntimeCitySpawnerSystem, CitizenPopulationSystem, UI-facing composition, and some bootstrap publication still need follow-up migration before BuildingPlacementRuntimeComponent can be retired.
- Initial resource ownership is now projected to FactionEconomy in InitialUnitsSpawnSystem; oil/fuel initial resource ownership should be consolidated into the FactionResourceSystem boundary when that config path is migrated.

Cross-lane impacts:
- AI/building boundary work can continue reducing BuildingPlacementSystem facade dependencies without InitialUnitsSpawnSystem blocking on the managed placement bridge.
- QA should treat initial base/wall/gate spawning as boundary-request driven after this change.

Next recommended task:
- Step 2 of the facade retirement plan: migrate RuntimeCitySpawnerSystem or the next remaining runtime spawn consumer off BuildingPlacementRuntimeComponent and onto BuildingRuntimeSpawnRequest/BuildingRuntimeBoundaryTag.

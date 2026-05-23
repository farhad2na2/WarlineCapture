# WarlineCapture Handoff

Lane: Gameplay

Task: Move BuildingRuntimeBoundarySystem spawn request processing away from BuildingPlacementSystem facade calls.

Files changed:
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-building-runtime-boundary-spawn-direct.md

Contracts touched:
- BuildingRuntimeBoundarySystem now resolves spawn request building definitions through BuildingDefinitionSystem.
- BuildingRuntimeBoundarySystem now completes BuildingRuntimeSpawnRequest through BuildingRuntimeSpawnSystem.TryPlaceRuntimeBuilding.
- BuildingPlacementSystem now passes BuildingDefinitionSystem, BuildingRuntimeSpawnSystem, and the runtime spawn context into the boundary update.
- GameplayArchitectureContractTests now blocks BuildingRuntimeBoundarySystem from calling BuildingPlacementSystem facade spawn/config methods for runtime spawn requests.

User-visible behavior:
- No intended gameplay behavior change.
- AI/runtime spawn requests still return missing-config, blocked, or success results through BuildingRuntimeSpawnRequest.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-boundary-spawn-direct.log

Validation result:
- Passed. TestResults-639151759659275210.xml reports GameplayArchitectureContractTests 89/89 passed, 0 failed.

Known gaps:
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem for resource sell requests.
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem for unit production request completion.
- BuildingRuntimeBoundarySystem still publishes configured unit and runtime production summaries through BuildingPlacementSystem until unit production/read-model ownership moves further out.

Cross-lane impacts:
- AI build spawn requests now cross into runtime building creation through the ECS boundary and BuildingRuntimeSpawnSystem instead of the placement facade.
- Existing UI/player placement facade APIs remain intact for compatibility.

Next recommended task:
- Move BuildingRuntimeBoundarySystem production request completion away from BuildingPlacementSystem.TryQueueFactionUnitProduction by wiring it to BuildingProductionRequestSystem/BuildingProductionSystem ownership.

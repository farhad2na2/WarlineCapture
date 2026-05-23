# WarlineCapture Handoff

Lane: Gameplay

Task: Move BuildingRuntimeBoundarySystem production request completion away from BuildingPlacementSystem.TryQueueFactionUnitProduction.

Files changed:
- Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs
- Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-building-runtime-boundary-production-direct.md

Contracts touched:
- BuildingFactionUnitProductionRequest now owns explicit failure result codes for missing unit config, missing producer building, and producer unavailable.
- BuildingProductionRequestSystem now owns faction production request completion through QueueFactionUnitProductionRequest.
- BuildingProductionRequestSystem.Context now carries BuildingProductionSystem.QueueContext so boundary production requests can queue through BuildingProductionSystem directly.
- BuildingRuntimeBoundarySystem now completes BuildingFactionUnitProductionRequest through BuildingProductionRequestSystem/BuildingProductionSystem instead of BuildingPlacementSystem.TryQueueFactionUnitProduction.
- GameplayArchitectureContractTests now blocks BuildingRuntimeBoundarySystem from returning to BuildingPlacementSystem facade production request calls.

User-visible behavior:
- No intended gameplay behavior change.
- AI production requests still return queued, missing config, missing producer, or producer unavailable results through BuildingFactionUnitProductionRequest.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-boundary-production-direct.log

Validation result:
- Passed. TestResults-639151763266140380.xml reports GameplayArchitectureContractTests 89/89 passed, 0 failed.

Known gaps:
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem for resource sell requests.
- BuildingRuntimeBoundarySystem still publishes configured unit and production summary read models through BuildingPlacementSystem until unit read-model/query ownership moves further out.
- BuildingPlacementSystem.TryQueueFactionUnitProduction remains as compatibility facade debt for any older callers.

Cross-lane impacts:
- AI production now crosses the runtime boundary into production ownership without the placement facade production wrapper.
- Existing UI/player production APIs remain intact for compatibility.

Next recommended task:
- Move BuildingRuntimeBoundarySystem configured unit read-model and production summary publication away from BuildingPlacementSystem by routing through BuildingDefinitionSystem and BuildingRuntimeQuerySystem directly.

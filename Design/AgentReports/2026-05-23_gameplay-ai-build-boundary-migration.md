# WarlineCapture Handoff

Lane: Gameplay

Task: Migrate AIBuildPlannerSystem build spawn requests through BuildingRuntimeSpawnRequest.

Files changed:
- Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs
- Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-ai-build-boundary-migration.md

Contracts touched:
- AIBuildPlannerSystem now reads configured building data through BuildingConfiguredSpawnableReadModel.
- AIBuildPlannerSystem now reads owned/faction building counts through BuildingRuntimeOwnedBuildingSummary and BuildingRuntimeFactionSummary.
- AIBuildPlannerSystem now enqueues BuildingRuntimeSpawnRequest entries and consumes completed boundary results.
- BuildingRuntimeSpawnRequest now carries AI plan correlation fields so the planner can apply economy and plan-index updates after the boundary completes the request.
- GameplayArchitectureContractTests now prevents AIBuildPlannerSystem from returning to BuildingPlacementSystem, BuildingPlacementRuntimeComponent, or direct spawn/config/count helper calls.

User-visible behavior:
- No intended gameplay behavior change.
- AI build placement remains interval driven, skips already-owned planned buildings, deducts money only after a successful boundary spawn, advances the plan after success or missing config, and retries blocked placements later.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-ai-build-boundary.log

Validation result:
- Passed. TestResults-639151755818607720.xml reports GameplayArchitectureContractTests 89/89 passed, 0 failed.

Known gaps:
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem.TrySpawnRuntimeBuilding internally until runtime spawn ownership fully moves out of the facade.
- BuildingRuntimeBoundarySystem still resolves configured spawnables through BuildingPlacementSystem internally until configured building authoring/read-model projection is fully owned outside the facade.

Cross-lane impacts:
- AI build planning now depends on the ECS building runtime boundary buffers instead of direct gameplay facade reads/mutations.
- Building runtime ownership remains responsible for publishing spawnable/owned-building read models and completing runtime spawn request results.

Next recommended task:
- Move BuildingRuntimeBoundarySystem spawn processing internals away from BuildingPlacementSystem by wiring it to BuildingRuntimeSpawnSystem and BuildingDefinitionSystem directly.

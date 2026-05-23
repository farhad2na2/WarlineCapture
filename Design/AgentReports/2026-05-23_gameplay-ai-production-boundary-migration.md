# WarlineCapture Handoff

Lane: Gameplay

Task: Migrate AIProductionSystem production requests through BuildingFactionUnitProductionRequest.

Files changed:
- Assets/Game/Scripts/Systems/AIProductionSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-ai-production-boundary-migration.md

Contracts touched:
- AIProductionSystem now reads configured unit data through BuildingConfiguredUnitReadModel.
- AIProductionSystem now reads produced and queued counts through BuildingRuntimeUnitProductionSummary.
- AIProductionSystem now enqueues BuildingFactionUnitProductionRequest entries and consumes completed boundary results.
- GameplayArchitectureContractTests now prevents AIProductionSystem from returning to BuildingPlacementSystem, BuildingPlacementRuntimeComponent, or direct production helper calls.

User-visible behavior:
- No intended gameplay behavior change.
- AI faction unit production still uses the same queue semantics, with economy money deducted after the boundary returns a queued success result.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-ai-production-boundary-rerun.log

Validation result:
- Passed. TestResults-639151751495375320.xml reports GameplayArchitectureContractTests 88/88 passed, 0 failed.

Known gaps:
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem.TryQueueFactionUnitProduction internally until production ownership fully moves out of the facade.
- AIBuildPlannerSystem still uses BuildingPlacementRuntimeComponent and BuildingPlacementSystem for build/spawn command paths.

Cross-lane impacts:
- AI production now depends on the ECS building runtime boundary buffers instead of direct gameplay facade reads.
- Building runtime ownership remains responsible for projecting configured unit read models, production summaries, and completing production request results.

Next recommended task:
- Migrate AIBuildPlannerSystem build spawn requests to BuildingRuntimeSpawnRequest and configured building/owned-building read models.

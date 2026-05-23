# WarlineCapture Handoff

Lane: Gameplay

Task: Migrate `AIEconomySystem` resource sell mutation to the ECS building runtime boundary.

Files changed:
- `Assets/Game/Scripts/Components/BuildingRuntimeEcsBoundaryComponents.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/AIEconomySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-ai-economy-sell-boundary-migration.md`

Contracts touched:
- Added `BuildingFactionResourceSellRequest` ECS buffer contract.
- `GameBootstrap` now installs the sell request buffer on the building runtime boundary entity.
- `AIEconomySystem` no longer reads `BuildingPlacementRuntimeComponent` or calls `BuildingPlacementSystem`.
- `AIEconomySystem` now enqueues resource sell requests and processes completed sell results from the ECS boundary buffer.
- `BuildingRuntimeBoundarySystem` consumes pending sell requests and writes sold oil/fuel results.
- Architecture tests now prevent `AIEconomySystem` from returning to building facade calls for resource reads or sell mutation.

User-visible behavior:
- No intended behavior change.
- AI economy selling is now asynchronous through ECS buffers. Money is updated when the boundary returns a completed sell result.
- Stored oil/fuel remain sourced from `BuildingRuntimeFactionSummary`, avoiding duplicate local subtraction after the boundary drains resources.

Validation run:
- Unity 6000.4.0f1 batchmode EditMode test run in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Command: `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-ai-economy-sell-boundary.log`

Validation result:
- Passed.
- Result file: `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151746896826650.xml`
- `GameplayArchitectureContractTests`: 87/87 passed.

Known gaps:
- `BuildingRuntimeBoundarySystem` still calls `BuildingPlacementSystem.SellFactionResources` internally because resource storage ownership still lives behind the building facade.
- `AIBuildPlannerSystem` and `AIProductionSystem` still use `BuildingPlacementRuntimeComponent` / `BuildingPlacementSystem` for build and production command paths.

Cross-lane impacts:
- AI lane now has read and sell-mutation access to building resources through ECS boundary buffers.
- Future economy work should not reintroduce direct `BuildingPlacementSystem` access.

Next recommended task:
- Migrate `AIProductionSystem` unit production requests to `BuildingFactionUnitProductionRequest`, then migrate `AIBuildPlannerSystem` build spawn requests to `BuildingRuntimeSpawnRequest`.

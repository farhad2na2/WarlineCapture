# Lane
Gameplay

# Task
Step 9 architecture refactor: remove `MenuView` and building UI code from `BuildingPlacementSystem` nested UI/data contracts.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/UI/MenuView.cs`
- `Assets/Tests/Editor/BuildingUiQuerySystemTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- `BuildingUiCommandSystem` now owns configured spawnable/unit UI contracts and camp request failure codes.
- `BuildingUiQuerySystem` now owns produced-unit and pending-production UI read-model contracts.
- `MenuView` contract now forbids any `BuildingPlacementSystem` reference, including nested UI/data contracts.
- `BuildingPlacementSystem` no longer declares `ProducedUnitUiEntry`, `PendingProductionUiEntry`, `ConfiguredSpawnableEntry`, `ConfiguredUnitEntry`, or `CampRequestFailure`.

# User-visible behavior
No intended behavior change. Camp requests, pending production countdowns, configured unit/building cards, and selected-building production UI should behave the same.

# Validation run
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step9-building-ui-contract-types-rerun.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingUiQuerySystemTests -logFile /private/tmp/warline-step9-building-ui-query-tests.log`

# Validation result
Passed.
- `GameplayArchitectureContractTests`: 91/91 passing in `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152125571311190.xml`.
- `BuildingUiQuerySystemTests`: 3/3 passing in `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152125959447290.xml`.

# Known gaps
- `BuildingPlacementSystem` still exposes temporary facade methods that return the new UI boundary types.
- Remaining nested facade contracts include faction economy and faction production result types.
- `BuildingPlacementSystem` is still 2788 lines and should continue shrinking by public API migration.

# Cross-lane impacts
UI code should reference `BuildingUiCommandSystem` and `BuildingUiQuerySystem` contracts directly, not `BuildingPlacementSystem` nested types. No art, scene, AI balance, or economy behavior was intentionally changed.

# Next recommended task
Move the remaining faction/resource read and command result contracts out of `BuildingPlacementSystem`, starting with `FactionResourceEconomySnapshot`, `FactionUnitProductionResultCode`, and `FactionUnitProductionResult`, then migrate AI/economy callers to the owning runtime boundary systems.

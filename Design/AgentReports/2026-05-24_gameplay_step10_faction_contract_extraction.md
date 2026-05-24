# Lane
Gameplay

# Task
Step 10 architecture refactor: move remaining faction/resource result contracts out of `BuildingPlacementSystem`.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs`
- `Assets/Game/Scripts/Systems/FactionResourceSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- `FactionResourceSystem` now owns `FactionResourceEconomySnapshot`.
- `BuildingProductionRequestSystem` now owns `FactionUnitProductionResultCode` and `FactionUnitProductionResult`.
- `BuildingPlacementSystem` no longer declares those faction/resource result contracts.
- Added contract coverage to prevent these result contracts from drifting back into `BuildingPlacementSystem`.

# User-visible behavior
No intended behavior change. Faction resource economy reads and faction unit-production request results keep the same fields and values.

# Validation run
`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step10-faction-contracts.log`

# Validation result
Passed. `GameplayArchitectureContractTests` completed 92/92 passing in `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152133920037910.xml`.

# Known gaps
- `BuildingPlacementSystem` still has temporary facade methods for faction resource economy and faction unit production. It now returns owning-system contract types, but the methods should be migrated away from the facade later.
- `BuildingPlacementSystem` is still 2734 lines.

# Cross-lane impacts
AI/economy callers should continue using ECS boundary buffers. If any same-assembly gameplay code still needs the temporary facade result, use `BuildingProductionRequestSystem.FactionUnitProductionResult` or `FactionResourceSystem.FactionResourceEconomySnapshot` directly.

# Next recommended task
Move faction unit-production request orchestration itself out of `BuildingPlacementSystem` by extending `BuildingProductionRequestSystem` to own `TryQueueFactionUnitProduction`, then reduce `BuildingPlacementSystem` to a temporary wrapper or remove that wrapper if no callers remain.

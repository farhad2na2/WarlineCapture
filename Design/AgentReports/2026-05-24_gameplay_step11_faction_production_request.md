# Lane
Gameplay

# Task
Step 11 architecture refactor: move faction unit-production request orchestration out of `BuildingPlacementSystem` and into `BuildingProductionRequestSystem`.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- `BuildingProductionRequestSystem` now owns `TryQueueFactionUnitProduction`, faction producer lookup, and faction production request result construction.
- `BuildingPlacementSystem.TryQueueFactionUnitProduction` is now a thin temporary wrapper that delegates to `BuildingProductionRequestSystem`.
- Architecture contract coverage now guards faction producer lookup and faction production request orchestration against drifting back into `BuildingPlacementSystem`.

# User-visible behavior
No intended behavior change. AI/faction unit production should queue through the same producer selection and production queue rules as before.

# Validation run
`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step11-faction-production-request-rerun2.log`

# Validation result
Passed. `GameplayArchitectureContractTests` completed 92/92 passing in `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152139809228410.xml`.

# Known gaps
- `BuildingPlacementSystem` still exposes a temporary `TryQueueFactionUnitProduction` wrapper for same-assembly compatibility.
- `CountRuntimeProducedUnitsForFaction` and `CountPendingProductionsForFaction` are still exposed by `BuildingPlacementSystem` while callers migrate fully to `BuildingRuntimeQuerySystem` or ECS boundary buffers.
- `BuildingPlacementSystem` is still 2671 lines.

# Cross-lane impacts
AI production should continue using `BuildingRuntimeBoundaryTag` ECS buffers. Any same-assembly fallback caller should treat `BuildingPlacementSystem.TryQueueFactionUnitProduction` as compatibility-only.

# Next recommended task
Remove the remaining faction production/count compatibility wrappers from `BuildingPlacementSystem` by migrating any direct callers to `BuildingProductionRequestSystem`, `BuildingRuntimeQuerySystem`, or the ECS boundary buffers.

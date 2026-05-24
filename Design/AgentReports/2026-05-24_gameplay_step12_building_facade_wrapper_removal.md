# Lane
Gameplay

# Task
Step 12: remove the remaining faction production, faction resource, and faction count compatibility wrappers from `BuildingPlacementSystem`, migrate callers/tests to the ECS building runtime boundary, and lock the rule into the architecture contract.

# Files changed
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs`
- `Assets/Tests/Editor/AIBuildPlannerValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/AIProductionValidationTests.cs`
- `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added the architecture rule that `BuildingPlacementSystem` must not expose faction production, faction resource economy/sell, or faction count compatibility wrappers.
- `BuildingPlacementSystem` no longer exposes:
  - `TryQueueFactionUnitProduction`
  - `TryGetFactionResourceEconomy`
  - `SellFactionResources`
  - `CountRuntimeBuildingsForFaction`
  - `CountRuntimeProducedUnitsForFaction`
  - `CountPendingProductionsForFaction`
- Tests now read faction building/production state through `BuildingRuntimeBoundaryTag` buffers instead of the facade.
- `BuildingRuntimeBoundarySystem` now forces read-model publication after successful production requests and publishes owned-building summaries from live runtime buildings before adding configured zero rows.

# User-visible behavior
No intentional gameplay behavior change.

AI build and production validation now reflects the ECS request/result flow:
- AI systems enqueue requests.
- The building runtime boundary processes requests.
- AI systems consume completed request results and log final outcomes.

# Validation run
- `git diff --check`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `GameplayArchitectureContractTests`
  - `AIProductionValidationTests`
  - `AIBuildPlannerValidationTests`
  - `AIEndToEndValidationTests`
  - `InitialFactionBaseValidationTests`

# Validation result
Passed.

- `GameplayArchitectureContractTests`: 92/92 passed.
- `AIProductionValidationTests`: 1/1 passed.
- `AIBuildPlannerValidationTests`: 1/1 passed.
- `AIEndToEndValidationTests`: 1/1 passed.
- `InitialFactionBaseValidationTests`: 7/7 passed.

# Known gaps
- `BuildingPlacementSystem.cs` is reduced to 2,621 lines, but it is not retired yet.
- It still owns remaining UI/placement-facing compatibility surface and should continue shrinking until it is only a temporary facade or can be renamed/removed.
- Initial faction-base validation revealed that the runtime owned-building summary publishes the spawned base buildings under faction `0`; this report does not change faction ownership semantics because Step 12 was facade removal and ECS boundary migration.

# Cross-lane impacts
- AI tests were updated to the request/result cadence of the ECS building runtime boundary.
- No source docs or other lane task files were modified.

# Next recommended task
Step 13: migrate the remaining UI/placement-facing read and command callers away from `BuildingPlacementSystem` toward narrow ECS boundary systems, then reassess whether `BuildingPlacementSystem` can be renamed to a temporary facade or removed.

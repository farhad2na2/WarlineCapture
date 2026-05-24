# Lane
Gameplay

# Task
Step 13: move remaining building UI read-model retrieval away from `BuildingUiCommandSystem`/`BuildingPlacementSystem` and into the `BuildingUiQuerySystem` boundary.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs`
- `Assets/Game/Scripts/Systems/MenuStartupSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/UI/MenuView.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added/locked the rule that `BuildingUiCommandSystem` must not own read-model query delegates or pending-production UI list retrieval.
- `MenuView` now receives a separate `BuildingUiQuerySystem` boundary/context for pending-production UI reads.
- `BuildingPlacementSystem` now delegates selected produced-unit and friendly pending-production UI reads through `BuildingUiQuerySystem.Context`.

# User-visible behavior
No intentional user-visible behavior change. Camp/game request countdown UI should continue reading the same pending production entries.

# Validation run
- `git diff --check`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `GameplayArchitectureContractTests`
  - `BuildingUiQuerySystemTests`

# Validation result
Passed.

- `GameplayArchitectureContractTests`: 92/92 passed.
- `BuildingUiQuerySystemTests`: 3/3 passed.

# Known gaps
- `BuildingPlacementSystem.cs` is reduced to 2,579 lines but still exposes compatibility wrappers for UI and runtime/test workflows.
- `BuildingPlacementSystem.GetFriendlyPendingProductionUiEntries` remains as a temporary facade wrapper; the loop/query ownership has moved to `BuildingUiQuerySystem`.
- `MenuView` still uses `BuildingUiCommandSystem` for commands and scalar command-adjacent reads. Further migration can split additional selected-building display/command reads into narrower UI query/command bindings.

# Cross-lane impacts
- UI lane should be aware that pending-production request countdown data now comes through `BuildingUiQuerySystem`, not `BuildingUiCommandSystem`.
- No unrelated UI architecture documents were modified as part of this Gameplay step.

# Next recommended task
Step 14: continue retiring `BuildingPlacementSystem` UI facade wrappers by moving selected-building production/health/preview command-facing methods behind direct `BuildingUiCommandSystem` and `BuildingPlacementQuerySystem` bindings, then update `MenuView` to stop relying on `BuildingPlacementSystem`-created command delegates where practical.

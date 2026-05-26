# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 13: move placement confirm/cancel/exit commands.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added the rule that placement confirm, cancel, exit, pointer-down, and active-placement cost commands must route through `BuildingPlacementCommandSystem`, not direct session calls in `BuildingGameplaySystem`.
- Added architecture validation for step 13 and included it in `RunBuildingGameplayArchitectureBatchValidation`.
- Updated the BuildingGameplay shell ceiling to the step 13 transition size of 1919 lines.

# User-visible behavior
No intended user-visible behavior change. Confirm, cancel, exit build mode, pointer-down, and active-placement cost behavior still use `BuildingPlacementSessionSystem`; the broad shell now routes through `BuildingPlacementCommandSystem`.

# Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/building_gameplay_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Scoped diff check passed.
- BuildingGameplay architecture validation passed: 15 methods.
- Building runtime boundary validation passed: 1/1.

# Known gaps
- `BuildingGameplaySystem` still exposes temporary public wrappers for confirm/cancel/exit/pointer-down compatibility.
- Placement focus and visual update callbacks remain in `BuildingGameplaySystem`; this is the next roadmap item.
- Existing unrelated dirty files under Game_Terrain4/art reports were not touched.

# Cross-lane impacts
None expected. This is an internal gameplay architecture extraction with no scene, prefab, or art changes.

# Next recommended task
Step 14: move active-placement focus, placement visual update, placement validation for confirm, and placement object handoff into placement lifecycle/preview/commit systems.

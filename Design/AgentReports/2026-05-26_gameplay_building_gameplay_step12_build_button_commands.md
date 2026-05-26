# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 12: extract build-button placement commands.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added the rule that build-button placement commands must live in `BuildingPlacementCommandSystem`, not `BuildingGameplaySystem`.
- Added architecture validation for step 12 and included it in `RunBuildingGameplayArchitectureBatchValidation`.
- Updated the BuildingGameplay shell ceiling to the step 12 transition size of 1919 lines.

# User-visible behavior
No intended user-visible behavior change. Soldier base, soldier tent, factory, and configured-spawnable placement starts still route through the same placement session behavior.

# Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/building_gameplay_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Scoped diff check passed.
- BuildingGameplay architecture validation passed: 14 methods.
- Building runtime boundary validation passed: 1/1.

# Known gaps
- `BuildingGameplaySystem` still exposes temporary public wrappers for build-button commands for compatibility.
- Confirm/cancel/exit placement commands still route through `BuildingGameplaySystem`; this is the next roadmap item.
- Existing unrelated dirty files under Game_Terrain4/art reports were not touched.

# Cross-lane impacts
None expected. This is an internal gameplay architecture extraction with no scene, prefab, or art changes.

# Next recommended task
Step 13: move `ConfirmBuildingPlacement`, `CancelBuildingPlacement`, `ExitBuildMode`, and placement pointer notification to `BuildingPlacementCommandSystem` / `BuildingPlacementInteractionSystem`.

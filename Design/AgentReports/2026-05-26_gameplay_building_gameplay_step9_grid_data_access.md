# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 9: extract grid data access.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingGameplayGridDataSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayGridDataSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added the rule that building grid data access must route through `BuildingGameplayGridDataSystem`, not direct grid query/buffer reads in `BuildingGameplaySystem`.
- Added architecture validation for step 9 and included it in `RunBuildingGameplayArchitectureBatchValidation`.
- Updated the BuildingGameplay line ceiling to the step 9 transition size of 1984 lines.

# User-visible behavior
No intended user-visible behavior change. Existing placement, selection, validation, and runtime tick callers still use the same temporary `BuildingGameplaySystem` wrapper methods while the actual grid data access moved behind a narrow system.

# Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayGridDataSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/building_gameplay_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Scoped diff check passed.
- BuildingGameplay architecture validation passed: 11 methods.
- Building runtime boundary validation passed: 1/1.

# Known gaps
- `BuildingGameplaySystem` still exists as a temporary broad shell with wrapper delegates for context factories.
- Grid data wrappers remain in `BuildingGameplaySystem` until placement, selection, validation, and runtime tick context factories are moved to narrow systems.
- Existing unrelated dirty files under Game_Terrain4/art reports were not touched.

# Cross-lane impacts
None expected. This is an internal gameplay architecture extraction with no scene, prefab, or art changes.

# Next recommended task
Step 10: extract placement invalid-cell cache into `BuildingPlacementInvalidCellSystem`, including prefix arrays, rebuild flags, road footprint mask creation, runtime blocker checks, and cached-footprint validation.

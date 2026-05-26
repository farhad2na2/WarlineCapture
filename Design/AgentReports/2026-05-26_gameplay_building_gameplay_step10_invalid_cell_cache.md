# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 10: extract placement invalid-cell cache.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added the rule that placement invalid-cell cache ownership must live in `BuildingPlacementInvalidCellSystem`, not `BuildingGameplaySystem`.
- Added architecture validation for step 10 and included it in `RunBuildingGameplayArchitectureBatchValidation`.
- Updated the BuildingGameplay shell ceiling to the step 10 transition size of 1958 lines.

# User-visible behavior
No intended user-visible behavior change. Placement validation still uses the same grid roads, dynamic blockers, road footprint mask, runtime blocker filtering, and runtime building overlap checks.

# Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/building_gameplay_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Scoped diff check passed.
- BuildingGameplay architecture validation passed: 12 methods.
- Building runtime boundary validation passed: 1/1.

# Known gaps
- `BuildingGameplaySystem` still exists as temporary composition/shell debt.
- Invalid-cell wrapper methods remain in `BuildingGameplaySystem` until placement/runtime context factories are moved out.
- Existing unrelated dirty files under Game_Terrain4/art reports were not touched.

# Cross-lane impacts
None expected. This is an internal gameplay architecture extraction with no scene, prefab, or art changes.

# Next recommended task
Step 11: move building spawn random state out of `BuildingGameplaySystem` into `BuildingSpawnSystem` or a narrow spawn-random owner, with production/runtime spawn contexts receiving explicit get/set delegates from that owner.

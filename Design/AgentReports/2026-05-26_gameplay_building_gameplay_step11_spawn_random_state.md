# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 11: move building spawn random state.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Added the rule that building spawn random state must live in `BuildingSpawnSystem`, not `BuildingGameplaySystem`.
- Added architecture validation for step 11 and included it in `RunBuildingGameplayArchitectureBatchValidation`.
- Updated the BuildingGameplay shell ceiling to the step 11 transition size of 1951 lines.

# User-visible behavior
No intended user-visible behavior change. Building production and helipad spawn resolution still advance the same random state; the state is now owned by the spawn domain.

# Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/building_gameplay_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Scoped diff check passed.
- BuildingGameplay architecture validation passed: 13 methods.
- Building runtime boundary validation passed: 1/1.

# Known gaps
- `BuildingGameplaySystem` still exists as temporary composition/shell debt.
- Production runtime tick still receives get/set delegates, but they now point at `BuildingSpawnSystem`; later tick/context extraction should remove the broad shell from that wiring entirely.
- Existing unrelated dirty files under Game_Terrain4/art reports were not touched.

# Cross-lane impacts
None expected. This is an internal gameplay architecture extraction with no scene, prefab, or art changes.

# Next recommended task
Step 12: extract build-button placement commands so UI buttons call a placement command boundary instead of `BuildingGameplaySystem`.

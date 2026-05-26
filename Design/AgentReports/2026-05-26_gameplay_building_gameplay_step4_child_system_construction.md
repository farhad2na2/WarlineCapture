# Lane
Gameplay

# Task
BuildingGameplaySystem refactor step 4 - move child system construction into composition.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`

# Contracts touched
- `BuildingGameplayCompositionSystem` now creates `BuildingGameplayCompositionSourceSystem` and passes it into `BuildingGameplaySystem`.
- `BuildingGameplaySystem` child system fields are assigned from the composition source instead of constructing those child systems inline.
- Architecture guard now verifies child system construction lives in composition and the shell does not reintroduce inline child construction.

# User-visible behavior
No intended runtime behavior change.

# Validation run
- `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `BuildingRuntimeBoundaryValidationTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/building_gameplay_system_refactor_roadmap.md`

# Validation result
- Passed: `[BuildingGameplayArchitectureValidation] result=Passed methods=6`.
- Passed: `BuildingRuntimeBoundaryValidationTests` 1/1.
- Passed: `git diff --check`.

# Known gaps
- `BuildingGameplaySystem` still exists and still owns dependency binding, query/context factories, and command wrappers. The parameterless constructor remains for temporary editor test harness compatibility but routes through composition-owned child system creation.

# Cross-lane impacts
- None expected. Runtime city, road build, selection, UI, AI, and art behavior are unchanged.

# Next recommended task
- Step 5: Extract building dependency binding into a narrow system so menu, selection, grid blocker, runtime city, citizen population, faction visual, and day/night references stop living on `BuildingGameplaySystem`.

# Lane
Gameplay

# Task
BuildingGameplaySystem refactor step 1 - add roadmap and baseline architecture guard.

# Files changed
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`

# Contracts touched
- Added `BuildingGameplaySystem` refactor roadmap tracking to the SOLID/ECS architecture contract.
- Added focused building gameplay architecture batch validation.
- Added guards for roadmap existence, 2021-line baseline, and bounded production references.

# User-visible behavior
No runtime behavior changed.

# Validation run
- `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/building_gameplay_system_refactor_roadmap.md`

# Validation result
- Passed: `[BuildingGameplayArchitectureValidation] result=Passed methods=3`.
- Passed: `git diff --check`.

# Known gaps
- This step only establishes the guardrail. `BuildingGameplaySystem.cs` remains temporary debt until later roadmap deletion steps.

# Cross-lane impacts
- None. Road, runtime city, UI, selection, AI, and art behavior are unchanged.

# Next recommended task
- Step 2: Add deletion target contract for `BuildingGameplaySystem.cs` and define temporary debt allowances explicitly.

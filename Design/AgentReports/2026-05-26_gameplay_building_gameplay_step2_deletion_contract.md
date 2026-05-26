# Lane
Gameplay

# Task
BuildingGameplaySystem refactor step 2 - add deletion target contract.

# Files changed
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`

# Contracts touched
- Architecture contract now states the final target is deletion of `BuildingGameplaySystem.cs`.
- Temporary debt is limited to production construction inside `BuildingGameplayCompositionSystem` and editor-only `BuildingGameplayTestHarness`.
- Added `BuildingGameplayDeletionTargetContractMustBeExplicit` to the focused building gameplay architecture validation batch.

# User-visible behavior
No runtime behavior changed.

# Validation run
- `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/building_gameplay_system_refactor_roadmap.md`

# Validation result
- Passed: `[BuildingGameplayArchitectureValidation] result=Passed methods=4`.
- Passed: `git diff --check`.

# Known gaps
- `BuildingGameplaySystem.cs` remains temporary debt by design until later roadmap steps migrate composition and tests.

# Cross-lane impacts
- None. This is architecture contract/test documentation only.

# Next recommended task
- Step 3: Freeze public surface inventory so each remaining public/internal shell member has an assigned target owner before extraction begins.

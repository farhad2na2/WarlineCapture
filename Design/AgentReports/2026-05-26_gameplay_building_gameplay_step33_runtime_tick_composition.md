# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 33: update runtime tick composition.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs`
- `Assets/Tests/Editor/BuildingGameplayTestHarness.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Updated the BuildingGameplay roadmap to mark step 33 complete and record the 1417-line transition ceiling.
- Updated the SOLID/ECS architecture contract so runtime tick composition must use direct child systems and must not use shell runtime tick/input domains or shell runtime state getter delegates.
- Added `BuildingRuntimeTickCompositionMustUseDirectSystems` to the focused architecture batch.

# User-visible behavior
No intended user-visible behavior change. This is an architecture-only runtime tick composition move.

# Validation run
- `git diff --check`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Passed: `/private/tmp/warlinecapture-building-gameplay-arch-step33.log` reports `[BuildingGameplayArchitectureValidation] result=Passed methods=36`.
- Passed: `/private/tmp/warlinecapture-building-runtime-boundary-step33.xml` reports `total="1" passed="1" failed="0"`.
- Passed: `git diff --check`.

# Known gaps
- `BuildingGameplaySystem.cs` remains temporary shell debt at 1417 lines.
- Production consumers still need to migrate off `BuildingGameplaySystem` before the shell can shrink further.

# Cross-lane impacts
- None expected. No gameplay balance, UI behavior, scenes, prefabs, or art changed by this step.

# Next recommended task
Step 34: migrate production consumers off `BuildingGameplaySystem` so production request/update/read paths consume direct owner systems and contexts.

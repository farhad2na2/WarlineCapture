# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 31: move runtime context factories.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Updated the BuildingGameplay roadmap to mark step 31 complete and record the 1446-line transition ceiling.
- Updated the SOLID/ECS architecture contract so runtime tick/runtime city context composition must call `BuildingRuntimeContextSystem` directly for spawn command, runtime visual, combat, runtime query, and barrier contexts.
- Added `BuildingRuntimeContextFactoriesMustRouteThroughRuntimeContextSystem` to the focused architecture batch.
- Updated the public/internal surface inventory to explicitly track `CreateRuntimeContextSystemSource` as temporary context-factory debt.

# User-visible behavior
No intended user-visible behavior change. This is an architecture-only context construction move.

# Validation run
- `git diff --check`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Passed: `/private/tmp/warlinecapture-building-gameplay-arch-step31.log` reports `[BuildingGameplayArchitectureValidation] result=Passed methods=34`.
- Passed: `/private/tmp/warlinecapture-building-runtime-boundary-step31.xml` reports `total="1" passed="1" failed="0"`.
- Passed: `git diff --check`.

# Known gaps
- `BuildingGameplaySystem.cs` remains temporary shell debt at 1446 lines.
- Runtime context source construction still depends on shell-owned callbacks until later runtime tick/composition steps remove the shell entirely.
- Production, UI, and interaction context factories remain for step 32.

# Cross-lane impacts
- None expected. No gameplay balance, UI behavior, scenes, prefabs, or art changed.

# Next recommended task
Step 32: move production and UI context factories into owner context systems so production/runtime tick and menu binding no longer require shell context methods.

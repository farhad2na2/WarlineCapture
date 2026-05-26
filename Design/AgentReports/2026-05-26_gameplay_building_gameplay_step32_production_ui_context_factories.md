# Lane
Gameplay

# Task
BuildingGameplaySystem refactor roadmap step 32: move production and UI context factories.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/building_gameplay_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Updated the BuildingGameplay roadmap to mark step 32 complete and record the 1446-line transition ceiling.
- Updated the SOLID/ECS architecture contract so production, UI, and interaction source construction route through their owner context systems.
- Added `BuildingProductionUiAndInteractionContextSourcesMustRouteThroughOwnerSystems` to the focused architecture batch.

# User-visible behavior
No intended user-visible behavior change. This is an architecture-only context source ownership move.

# Validation run
- `git diff --check`
- Unity batchmode: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests`

# Validation result
- Passed: `/private/tmp/warlinecapture-building-gameplay-arch-step32.log` reports `[BuildingGameplayArchitectureValidation] result=Passed methods=35`.
- Passed: `/private/tmp/warlinecapture-building-runtime-boundary-step32.xml` reports `total="1" passed="1" failed="0"`.
- Passed: `git diff --check`.

# Known gaps
- `BuildingGameplaySystem.cs` remains temporary shell debt at 1446 lines.
- The shell still provides callbacks and source wrappers until runtime tick composition and consumers migrate further.

# Cross-lane impacts
- None expected. No gameplay balance, UI behavior, scenes, prefabs, or art changed.

# Next recommended task
Step 33: update runtime tick composition so `BuildingGameplayCompositionSystem.CreateRuntimeTickSource` uses direct systems and context systems only, then remove shell runtime tick/input domain delegates.

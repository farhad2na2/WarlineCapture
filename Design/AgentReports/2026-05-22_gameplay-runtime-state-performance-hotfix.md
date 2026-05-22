# Lane
Gameplay

# Task
Fix the suspected FPS regression introduced by the runtime-state refactor.

# Files changed
- `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateSystemTests.cs`

# Contracts touched
- Added runtime-state tests that protect against read-side ECS overwrites after the compatibility bridge has already synchronized state.

# User-visible behavior
No intended gameplay behavior change. Runtime state still mirrors legacy static compatibility fields and ECS singleton components, but steady-state reads avoid repeated query creation and read-side ECS writes.

# Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs Assets/Tests/Editor/RuntimeGameplayStateSystemTests.cs`
- Unity EditMode `RuntimeGameplayStateSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `BattleHudGameplayBridgeConnectionTests`
- Unity EditMode `Chapter01M01PlayableRuntimeTests.RoadBuildSetBuildMode_RejectsM01WithoutEnteringBuildMode`

# Validation result
- `git diff --check`: passed.
- `RuntimeGameplayStateSystemTests`: passed 6/6.
- `GameplayArchitectureContractTests`: passed 38/38.
- `BattleHudGameplayBridgeConnectionTests`: passed 6/6.
- `Chapter01M01PlayableRuntimeTests.RoadBuildSetBuildMode_RejectsM01WithoutEnteringBuildMode`: passed 1/1.

# Known gaps
- This removes the obvious hot-path regression from the compatibility bridge, but it does not yet prove the live scene is back to 60 FPS. A runtime profiler capture should confirm frame time in the target scene.
- `RuntimeGameplayStateSystem` remains a compatibility bridge while legacy static callers are migrated.

# Cross-lane impacts
- QA should rerun the live scene performance capture that showed 20 FPS and compare frame time before/after this fix.
- Performance lane should watch for repeated `RuntimeGameplayStateSystem` reads in hot UI/gameplay loops if frame time is still high.

# Next recommended task
Run an in-scene performance validation capture. If FPS is still below target, batch hot-path reads in `RTSSelectionSystem`, `MenuView`, `GameBootstrap`, `RoadBuildSystem`, and `BuildingPlacementSystem` so each update reads `RuntimeGameplayStateComponent` once instead of multiple property calls.

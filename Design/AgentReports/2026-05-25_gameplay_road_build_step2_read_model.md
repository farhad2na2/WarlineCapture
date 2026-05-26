# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
RoadBuildSystem refactor step 2: create `RoadBuildReadModelSystem` and move selection camera read state off the broad road shell.

## Files changed
- `Assets/Game/Scripts/Systems/RoadBuildReadModelSystem.cs`
- `Assets/Game/Scripts/Systems/RoadBuildReadModelSystem.cs.meta`
- `Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystem.cs`
- `Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraContextSystem.cs`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs`
- `Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/road_build_system_refactor_roadmap.md`
- `Design/AgentReports/2026-05-25_gameplay_road_build_step2_read_model.md`

## Contracts touched
- Added `RoadBuildReadModelSystem` as the narrow read-only road interaction boundary.
- `RtsSelectionRuntimeCameraSystem` and `RtsSelectionRuntimeCameraContextSystem` now consume `RoadBuildReadModelSystem`, not `RoadBuildSystem`.
- `SelectionGameplayStartupSystem` receives the read model for camera/read state.
- `ManagedGameplayStartupSystem` composes the read model from the current road shell as a temporary compatibility source.
- Added architecture guard `RoadBuildReadModelMustOwnReadOnlyRoadInteractionState`.

## User-visible behavior
No intended gameplay behavior change. Road build mode, road dragging, and build-mode camera behavior should read the same values through the new boundary.

## Validation run
- `git diff --check --` on touched files.
- Unity batchmode in `WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation`.

## Validation result
- Diff check passed.
- RoadBuild architecture validation passed: `[RoadBuildArchitectureValidation] result=Passed methods=4`.
- `RoadBuildSystem.cs` stayed at 4041 lines; the read-model step did not grow the broad shell.

## Known gaps
- `RoadBuildReadModelSystem` currently reads through delegates backed by the broad shell. Later road extraction steps should replace those delegates with owned state from extracted road systems.
- `RoadBuildSystem` still owns graph mutation, visuals, input/session flow, ECS projection, runtime-city road generation APIs, and legacy building compatibility.

## Cross-lane impacts
- Selection/camera lane: build-mode camera checks no longer require a broad road shell reference.
- Architecture lane: RoadBuild batch validation now includes four guards.

## Next recommended task
Step 3: create `RoadBuildConfigSystem` and move `RoadBuildSystemConfig` projection out of `RoadBuildSystem`.

Lane
Gameplay

Task
Step 21 - move BuildingRuntimeUpdateSystem ownership and context construction out of BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay_step21_building_runtime_update_managed_composition.md

Contracts touched
- Updated the gameplay architecture contract so BuildingRuntimeUpdateSystem ownership and context construction belong in managed composition, not inside BuildingPlacementSystem.
- Updated GameplayArchitectureContractTests to guard against GameBootstrap reaching through BuildingPlacementSystem for runtime update wiring and against BuildingPlacementSystem owning BuildingRuntimeUpdateSystem.

User-visible behavior
- No intended behavior change.
- The managed runtime update loop still invokes BuildingPlacementSystem.Update through BuildingRuntimeUpdateSystem.Context, preserving the existing "BuildingPlacement" performance step order.

Validation run
- Mirrored Step 21 changed files to /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter GameplayArchitectureContractTests.
- Ran focused grep checks confirming BuildingPlacementSystem no longer contains BuildingRuntimeUpdateSystem or CreateBuildingRuntimeUpdateContext.
- Ran focused git diff whitespace check for Step 21 files.

Validation result
- Passed: GameplayArchitectureContractTests, 93 total, 93 passed, 0 failed.
- Passed: BuildingPlacementSystem no longer owns BuildingRuntimeUpdateSystem or its context factory.
- Passed: focused diff whitespace check for Step 21 files.

Known gaps
- BuildingPlacementSystem still exists as the compatibility facade and is currently 2474 lines.
- ManagedGameplayStartupSystem still creates the BuildingRuntimeUpdateSystem.Context from buildingPlacement.Update, so the update behavior still ultimately delegates to the facade.
- GameBootstrap still exposes BuildingPlacementSystem for remaining composition/startup paths.

Cross-lane impacts
- No scene files changed.
- Worktree has unrelated UI-lane changes in WarlineCaptureGameUiSceneBuilder.cs, WarlineCaptureShellEcsBridgeView.cs, WarlineCaptureShellView.cs, and Design/AgentReports/2026-05-24_ui_gameui-scene-step4.md; this Gameplay step did not modify them.

Next recommended task
- Step 22: remove GameBootstrap's public BuildingPlacementSystem exposure by replacing external access with narrower public boundaries, starting with BuildingRuntimeUpdate, BuildingUiCommandSystem/BuildingUiQuerySystem, BuildingPlacementInteractionSystem, and runtime city/query contexts where still needed.

Lane
Gameplay

Task
Step 18 - retire BuildingPlacementRuntimeComponent and stop publishing the managed BuildingPlacementSystem facade through ECS.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Components/BuildingPlacementRuntimeComponent.cs
- Assets/Game/Scripts/Components/BuildingPlacementRuntimeComponent.cs.meta
- Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay_step18_retire_building_placement_runtime_component.md

Contracts touched
- Added the gameplay architecture contract that GameBootstrap must not publish a managed BuildingPlacementSystem facade through ECS component objects.
- Extended GameplayArchitectureContractTests to require the BuildingPlacementRuntimeComponent file to stay retired and to guard GameBootstrap against AddComponentObject/GetComponentObject usage for building runtime boundary setup.

User-visible behavior
- No intended behavior change.
- GameBootstrap still creates/ensures the building runtime ECS boundary entity, but now installs only BuildingRuntimeBoundaryTag and explicit read/request buffers.
- AI/building systems continue to use BuildingRuntimeBoundaryTag buffers.

Validation run
- Mirrored Step 18 changed/deleted files to /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter GameplayArchitectureContractTests.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter AIBuildPlannerValidationTests.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter AIProductionValidationTests.
- Ran focused grep checks confirming Assets/Game/Scripts has no BuildingPlacementRuntimeComponent references.
- Ran focused git diff whitespace check for Step 18 files.

Validation result
- Passed: GameplayArchitectureContractTests, 93 total, 93 passed, 0 failed.
- Passed: AIBuildPlannerValidationTests, 1 total, 1 passed, 0 failed.
- Passed: AIProductionValidationTests, 1 total, 1 passed, 0 failed.
- Passed: production source has no BuildingPlacementRuntimeComponent references.
- Passed: focused diff whitespace check for Step 18 files.
- Note: repository-wide git status still includes unrelated UI lane changes in Assets/Game/Scenes/GameUI.unity, Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs.meta, and Design/AgentReports/2026-05-24_ui_gameui-scene-step3.md.

Known gaps
- BuildingPlacementSystem still exists as the compatibility facade and is currently 2474 lines.
- GameBootstrap still stores a public BuildingPlacementSystem property because managed runtime composition and remaining systems still receive the facade directly.
- RuntimeGameplayStateTestHelper still accepts a BuildingPlacementSystem parameter for compatibility with existing tests, but it no longer stores the facade in ECS.

Cross-lane impacts
- ECS building runtime boundary is now cleaner: BuildingRuntimeBoundaryTag plus explicit buffers only.
- No scene or UI lane files were modified by this Gameplay step.

Next recommended task
- Step 19: reduce direct GameBootstrap/managed composition exposure of BuildingPlacementSystem by moving remaining managed runtime dependencies to narrow systems or composed boundary bundles, starting with CitizenPopulationSystem and GameplayRuntimeUpdateSystem references.

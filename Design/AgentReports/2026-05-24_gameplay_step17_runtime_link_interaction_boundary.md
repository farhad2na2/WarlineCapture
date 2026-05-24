Lane
Gameplay

Task
Step 17 - remove direct BuildingPlacementSystem dependencies from runtime building entity links and unused menu init wiring.

Files changed
- Assets/Game/Scripts/UI/RuntimeBuildingEntityLink.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/MainMenuPlayUI.cs
- Assets/Game/Scripts/Systems/MenuStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay_step17_runtime_link_interaction_boundary.md

Contracts touched
- Added the gameplay architecture contract that runtime building entity-link callbacks must route through BuildingPlacementInteractionSystem instead of storing a BuildingPlacementSystem facade owner.
- Added the contract that MainMenuPlayUI must not accept a BuildingPlacementSystem dependency when it does not use one.
- Extended GameplayArchitectureContractTests coverage to guard RuntimeBuildingEntityLink, BuildingRuntimeCreationSystem, and MainMenuPlayUI against direct BuildingPlacementSystem coupling.

User-visible behavior
- No intended behavior change.
- Runtime building visuals still track their ECS entity transform and still notify building placement or road build systems when the backing entity is destroyed.
- Main menu play state behavior is unchanged; the removed building-placement init argument was unused.

Validation run
- Mirrored Step 17 changed files to /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter GameplayArchitectureContractTests.
- Ran focused grep checks confirming RuntimeBuildingEntityLink.cs, BuildingRuntimeCreationSystem.cs, and MainMenuPlayUI.cs no longer reference BuildingPlacementSystem.
- Ran focused git diff whitespace check for Step 17 files.

Validation result
- Passed: GameplayArchitectureContractTests, 93 total, 93 passed, 0 failed.
- Passed: RuntimeBuildingEntityLink.cs, BuildingRuntimeCreationSystem.cs, and MainMenuPlayUI.cs have no direct BuildingPlacementSystem or _buildingPlacementController references.
- Passed: focused diff whitespace check for Step 17 files.
- Note: repository-wide git diff --check remains blocked by unrelated trailing whitespace in Assets/Game/Scenes/GameUI.unity from another lane.

Known gaps
- BuildingPlacementSystem still exists as the compatibility facade and is currently 2474 lines.
- GameBootstrap still publishes BuildingPlacementRuntimeComponent with a managed BuildingPlacementSystem reference for remaining compatibility/test paths.
- BuildingRuntimeCreationSystem still receives interaction context from BuildingPlacementSystem until facade retirement continues.

Cross-lane impacts
- MenuStartupSystem now calls MainMenuPlayUI.Init without passing BuildingPlacementSystem.
- Runtime building visual links now depend on BuildingPlacementInteractionSystem for building-placement destruction callbacks.
- UI lane has unrelated dirty GameUI scene/shell files in the worktree; this Gameplay step did not modify or revert them.

Next recommended task
- Step 18: migrate BuildingPlacementRuntimeComponent away from carrying a managed BuildingPlacementSystem facade reference. Replace remaining consumers/tests with BuildingRuntimeBoundaryTag buffers or narrower runtime systems so GameBootstrap no longer publishes the facade through ECS.

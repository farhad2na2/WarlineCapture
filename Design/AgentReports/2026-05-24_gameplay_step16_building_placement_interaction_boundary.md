Lane
Gameplay

Task
Step 16 - move RoadBuildSystem and RTSSelectionSystem building-placement peer calls behind a narrow BuildingPlacementInteractionSystem boundary.

Files changed
- Assets/Game/Scripts/Systems/BuildingPlacementInteractionSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementInteractionSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/RoadBuildSystem.cs
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs
- Assets/Game/Scripts/Systems/MenuStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay_step16_building_placement_interaction_boundary.md

Contracts touched
- Added the gameplay architecture contract that RoadBuildSystem and RTSSelectionSystem building-placement peer interactions must go through BuildingPlacementInteractionSystem instead of holding or calling BuildingPlacementSystem directly.
- Added GameplayArchitectureContractTests coverage to prevent RoadBuildSystem and RTSSelectionSystem from regaining direct BuildingPlacementSystem references.

User-visible behavior
- No intended behavior change. Road building, RTS selection, placement commands, building selection clearing, runtime building destroy handling, and breach-target resolution keep the same external behavior through the new interaction boundary.

Validation run
- Mirrored changed gameplay files to /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter GameplayArchitectureContractTests.
- Ran focused grep checks confirming RoadBuildSystem.cs and RTSSelectionSystem.cs no longer reference BuildingPlacementSystem or _buildingPlacementController.
- Ran focused git diff whitespace check for Step 16 files.

Validation result
- Passed: GameplayArchitectureContractTests, 93 total, 93 passed, 0 failed.
- Passed: RoadBuildSystem.cs and RTSSelectionSystem.cs have no direct BuildingPlacementSystem or _buildingPlacementController references.
- Passed: focused diff whitespace check for Step 16 files.
- Note: repository-wide git diff --check is currently blocked by unrelated trailing whitespace in Assets/Game/Scenes/GameUI.unity, which belongs to another lane's in-flight GameUI scene work.

Known gaps
- BuildingPlacementSystem still exists as the compatibility facade and is currently 2473 lines.
- BuildingPlacementSystem still owns the interaction context factory until the remaining callers are migrated to narrower ECS/runtime boundaries.

Cross-lane impacts
- Startup wiring changed in ManagedGameplayStartupSystem, GameplayFeatureStartupSystem, and MenuStartupSystem to pass the interaction boundary/context.
- UI lane has unrelated dirty GameUI scene and shell files in the worktree; this Gameplay step did not modify or revert them.

Next recommended task
- Step 17: migrate any remaining non-UI runtime callers that still depend on BuildingPlacementSystem facade wrappers to their owning narrow systems, then review whether BuildingPlacementSystem can be renamed or retired as a facade.

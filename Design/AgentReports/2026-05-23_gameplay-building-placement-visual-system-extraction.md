Lane
Gameplay

Task
Extract placement visual instantiation, positioning, prefab bounds, and transformed bounds helpers from BuildingPlacementSystem into BuildingPlacementVisualSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-placement-visual-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so placement visual instance creation, placement visual positioning, prefab model bounds, and transformed bounds helpers belong in BuildingPlacementVisualSystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate placement visual work to BuildingPlacementVisualSystem and preventing Instantiate, CombinedMesh child selection, SetPositionAndRotation, TryGetPrefabModelBounds, and TransformBounds helpers from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Building placement preview visuals and runtime visual positioning should behave the same.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementVisualSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-placement-visual-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 81/81.
- Unity log included an initial licensing handshake warning, then resolved entitlement details and completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still large at 4468 lines.
- Small facade wrappers remain for delegate compatibility: CreateBuildingVisualInstance and PositionBuildingObject.
- BuildingPlacementVisualSystem owns prefab model bounds helpers, but they are currently not called by BuildingPlacementSystem after this extraction because the prior helpers were already unused in the facade.

Cross-lane impacts
- No scene, art, UI prefab, or ECS component data changes.
- Architecture test expectations changed for the building placement visual boundary.

Next recommended task
Extract runtime/manual building spawn into BuildingRuntimeSpawnSystem or extend BuildingSpawnSystem, keeping existing public TrySpawnRuntimeBuilding/TrySpawnRuntimeWallRun/TrySpawnRuntimeWallSegment facade methods stable.

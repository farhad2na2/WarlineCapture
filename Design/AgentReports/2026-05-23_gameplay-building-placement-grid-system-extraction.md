Lane
Gameplay

Task
Extract placement/grid math from BuildingPlacementSystem into BuildingPlacementGridSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-placement-grid-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so placement/grid math, footprint center projection, center-screen placement origin resolution, screen-to-grid raycasts, placement footprint rotation, and placement focus bounds belong in BuildingPlacementGridSystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate grid math to BuildingPlacementGridSystem and preventing screen raycast, ray-to-grid conversion, grid plane construction, placement focus bounds, and footprint rotation math from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Building placement preview movement, drag focus, rotated footprints, and center-screen placement origin should behave the same.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementGridSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-placement-grid-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 80/80.
- Unity log included an initial licensing handshake warning, then resolved entitlement details and completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still large at 4560 lines.
- Small facade wrappers remain for delegate compatibility: GetFootprintCenter, GetCenterScreenPlacementOrigin, GetPlacementFootprint, and TryGetGridCell.
- Remaining major shrink targets are visual instance positioning/bounds, runtime/manual building spawn, placement redirect, hauler bridge, and combat/blocker entity creation.

Cross-lane impacts
- No scene, art, UI prefab, or ECS component data changes.
- Architecture test expectations changed for the building placement grid boundary.

Next recommended task
Extract BuildingPlacementVisualSystem next: move CreateBuildingVisualInstance, PositionBuildingObject, prefab model bounds, transformed bounds, and related visual placement helpers out of BuildingPlacementSystem while keeping facade delegates stable.

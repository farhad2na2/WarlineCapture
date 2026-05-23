Lane
Gameplay

Task
Extract runtime building blocker/combat entity creation from BuildingPlacementSystem into BuildingRuntimeEntitySystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-runtime-entity-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so runtime blocker entity creation, runtime building combat entity creation, path-blocking policy, and runtime building combat component setup belong in BuildingRuntimeEntitySystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate blocker/combat entity creation and preventing runtime combat tag setup and blocker-size setup from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Runtime building blocker entities and runtime building combat entities should be created with the same components, health, faction, footprint, transform, display info, and threat detector setup as before.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-runtime-entity-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 84/84.
- Unity log included an initial licensing handshake warning, then completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still large at 4226 lines.
- BuildingPlacementSystem still keeps delegate-compatible wrappers for CreateBlockerEntity, ShouldRuntimeBuildingBlockPathing, and CreateBuildingCombatEntity because BuildingRuntimeCreationSystem consumes those delegates.
- BuildingRuntimeEntitySystem still receives EntityManager/grid/footprint-center access through BuildingPlacementSystem context until the remaining placement facade responsibilities are migrated.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture test expectations changed for runtime blocker/combat entity setup ownership.

Next recommended task
Extract placement redirect/deferred redirect footprints into BuildingPlacementRedirectSystem, because runtime creation still delegates redirect side effects back through BuildingPlacementSystem.

Lane
Gameplay

Task
Extract the resource-hauler bridge from BuildingPlacementSystem into BuildingResourceHaulerBridgeSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs
- Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-resource-hauler-bridge-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so resource-hauler update orchestration, selected-hauler assignment bridging, hauler move-order/path request bridging, building approach checks, and building approach-cell search belong in BuildingResourceHaulerBridgeSystem.
- Updated GameplayArchitectureContractTests to require BuildingPlacementSystem to delegate hauler bridge work and to prevent nearest-building lookup, hauler move-order/path request bridging, building approach search, scoring, and distance math from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Selected resource haulers should still accept building click orders for oil/fuel hauling.
- Active resource haulers should still move to source/destination approach cells, load, unload, and loop using the same resource-hauler state rules.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-hauler-bridge-editmode-rerun.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still a compatibility facade at 3491 lines.
- BuildingPlacementSystem still supplies EntityManager, grid data, runtime building lookup, and query access to the hauler bridge through a context.
- The hauler bridge still coordinates managed ECS access and should eventually move closer to fully ECS-owned hauling and movement systems.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture ownership changed for resource-hauler bridge behavior.

Next recommended task
Extract remaining placement redirect-adjacent combat/blocker or placement session compatibility wrappers only after confirming the facade has no direct mutable ownership left for that slice.

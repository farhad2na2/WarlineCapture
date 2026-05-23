Lane
Gameplay

Task
Extract production transport ECS bridge callbacks from BuildingPlacementSystem into BuildingProductionTransportBridgeSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-production-transport-bridge-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so production transport ground-cell conversion, produced-unit movement orders, produced-unit rotation alignment, and transport-spawn bridging belong in BuildingProductionTransportBridgeSystem.
- Updated GameplayArchitectureContractTests to require BuildingPlacementSystem to delegate the bridge and to prevent transport ground-cell conversion, UnitPathRequest movement order mutation, rotation alignment, and direct BuildingSpawnSystem transport spawn calls from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Production transport drops, plane/self-arrival spawns, produced-unit move orders, and produced-unit facing alignment should behave as before.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs.meta Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-production-transport-bridge-editmode-rerun.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still a compatibility facade at 3143 lines.
- BuildingPlacementSystem still owns wrapper delegates for BuildingProductionTransportSystem until transport context creation moves out of the facade.
- Spawn-cell perimeter helper algorithms remain in BuildingPlacementSystem and are still used by BuildingSpawnSystem through context delegates.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture ownership changed for production transport ECS bridge behavior.

Next recommended task
Extract spawn-cell perimeter helper algorithms into BuildingSpawnSystem or a narrow BuildingSpawnCellSystem, then reassess whether BuildingPlacementSystem is mostly context construction and public API wrappers.

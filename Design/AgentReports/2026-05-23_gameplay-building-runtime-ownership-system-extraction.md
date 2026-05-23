Lane
Gameplay

Task
Extract runtime owner-faction assignment and gate friendly-pass blocker updates from BuildingPlacementSystem into BuildingRuntimeOwnershipSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-runtime-ownership-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so runtime building owner-faction assignment, combat Faction component projection, owner marker color projection, and gate friendly-pass blocker updates belong in BuildingRuntimeOwnershipSystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate owner-faction assignment and preventing FriendlyPassGridBlocker, combat Faction projection, direct owner field assignment, and owner marker color projection from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Runtime buildings, runtime walls, and wall gates should keep the same owner faction, marker color, combat faction, and friendly-pass behavior.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-runtime-ownership-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 83/83.
- Unity log included an initial licensing handshake warning, then completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still large at 4272 lines.
- BuildingPlacementSystem still keeps a small SetRuntimeBuildingOwnerFaction wrapper because BuildingRuntimeSpawnSystem uses it as a stable delegate.
- Runtime ownership still depends on BuildingPlacementSystem for EntityManager access and marker visual dependencies through a context.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture test expectations changed for runtime ownership/faction behavior.

Next recommended task
Extract remaining runtime production spawn point query or building marker visibility/resource visual update glue out of BuildingPlacementSystem, depending whether the next priority is line reduction or reducing per-frame visual/update responsibilities.

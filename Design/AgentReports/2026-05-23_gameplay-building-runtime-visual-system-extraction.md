Lane
Gameplay

Task
Extract runtime building visual initialization, resource animation updates, and marker visibility projection from BuildingPlacementSystem into BuildingRuntimeVisualSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeVisualSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeVisualSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-runtime-visual-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so runtime building visual initialization, runtime resource animation updates, and runtime marker visibility projection belong in BuildingRuntimeVisualSystem.
- Updated GameplayArchitectureContractTests to require BuildingPlacementSystem to delegate runtime visual work and to prevent marker discovery, animated-part assignment, resource animation projection, marker visibility projection, and door visual angle normalization from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Runtime buildings should still initialize faction/selection/destroyed markers, door visual state, alive visual roots, and animated parts the same way.
- Resource-producing building animations and selected/destroyed marker visibility should still update as before.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeVisualSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeVisualSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-runtime-visual-editmode-rerun.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still a compatibility facade at 3225 lines.
- BuildingRuntimeVisualSystem still receives active-building id, faction visuals, marker property block, and low-level visual/barrier systems through a context.
- Runtime visual refresh callbacks still pass through BuildingPlacementSystem wrappers while dependent systems are migrated.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture ownership changed for runtime building visual state projection.

Next recommended task
Extract initial placement origin search into BuildingRuntimeSpawnSystem, then review remaining production transport bridge callbacks.

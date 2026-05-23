Lane
Gameplay

Task
Extract placement redirect side effects, deferred redirect footprints, pending marker-refresh deferral, and placed-building unit redirect scans from BuildingPlacementSystem into BuildingPlacementRedirectSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRedirectSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRedirectSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-placement-redirect-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so placement redirect side-effect deferral, deferred redirect footprints, pending marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and redirect movement component mutation belong in BuildingPlacementRedirectSystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate redirect deferral/flush/scan behavior and preventing redirect mutable state and redirect helper algorithms from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Units should still be moved or retargeted away from newly placed runtime building footprints using the same redirect rules.
- Deferred runtime building side effects should still batch redirects and marker refreshes.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRedirectSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRedirectSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-placement-redirect-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity log included an initial licensing handshake warning, then completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still large at 3928 lines.
- BuildingPlacementSystem still supplies EntityManager, grid data, and redirect unit query access through a context.
- Runtime creation still calls back into BuildingPlacementSystem wrappers for several compatibility delegates while the facade migration continues.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture test expectations changed for placement redirect ownership.

Next recommended task
Extract the hauler bridge/resource-hauler movement glue or runtime production spawn point query next; hauler bridge is the larger cross-system coupling, while spawn point query is the lower-risk line reduction.

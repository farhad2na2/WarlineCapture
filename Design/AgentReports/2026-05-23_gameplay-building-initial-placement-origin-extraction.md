Lane
Gameplay

Task
Extract active placement initial origin search from BuildingPlacementSystem into BuildingRuntimeSpawnSystem, then review remaining production transport bridge callbacks.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-building-initial-placement-origin-extraction.md

Contracts touched
- GameplayArchitectureContractTests now requires BuildingPlacementSystem to delegate active placement initial origin resolution to BuildingRuntimeSpawnSystem.
- GameplayArchitectureContractTests now blocks active placement radius search and full-grid origin fallback from returning to BuildingPlacementSystem.

User-visible behavior
- No intended gameplay behavior change.
- Beginning building placement should still choose the same initial valid origin near the screen-center preferred origin, including radius search and full-grid fallback when needed.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-initial-origin-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still a compatibility facade at 3176 lines.
- BuildingPlacementSystem still owns production transport bridge callbacks:
  - ResolveProductionGroundGoalCell
  - MoveNewestProducedUnitToCell
  - AlignNewestProducedUnitRotation
  - TrySpawnPlayerUnitNearBuilding wrapper overloads
- BuildingPlacementSystem still owns spawn-cell perimeter helper algorithms used by BuildingSpawnSystem through the current spawn context.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture ownership tightened for active placement initial origin search.

Next recommended task
Extract the production transport bridge callbacks into a narrow BuildingProductionTransportBridgeSystem or fold them into BuildingProductionTransportSystem with an injected EntityManager/grid/spawn context boundary.

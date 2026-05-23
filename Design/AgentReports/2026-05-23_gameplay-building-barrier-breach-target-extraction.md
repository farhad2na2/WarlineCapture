Lane
Gameplay

Task
Extract base-breach target resolution and breach approach-cell search from BuildingPlacementSystem into BuildingBarrierSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-barrier-breach-target-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so breach-target resolution and breach approach-cell search belong in BuildingBarrierSystem.
- Updated GameplayArchitectureContractTests to require BuildingPlacementSystem to delegate breach target resolution and to prevent breach final-target lookup, breach approach search, perimeter outside-direction selection, approach scoring, and perimeter-side checks from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Units should still redirect attacks to the nearest valid enemy wall/gate breach target before attacking protected targets inside an enemy perimeter.
- Breach approach-cell selection should still prefer an outside-perimeter attack cell and fall back to the generic runtime building approach cell.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-barrier-breach-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still a compatibility facade at 3284 lines.
- BuildingBarrierSystem still receives grid access and generic runtime-building approach fallback through injected context delegates.
- The generic runtime building approach-cell fallback is still owned by BuildingResourceHaulerBridgeSystem and routed through BuildingPlacementSystem compatibility wrappers.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture ownership changed for breach target and approach policy.

Next recommended task
Extract runtime building visual initialization, resource visual animation, and marker visibility updates into BuildingVisualSystem or a narrower BuildingRuntimeVisualSystem.

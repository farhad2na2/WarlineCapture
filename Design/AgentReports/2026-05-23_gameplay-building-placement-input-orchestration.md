Lane
Gameplay

Task
Move active-placement pointer event orchestration into BuildingPlacementInputSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-placement-input-orchestration.md

Contracts touched
- Updated the Building Domain Migration contract so active placement pointer event orchestration belongs in BuildingPlacementInputSystem.
- Updated GameplayArchitectureContractTests to require BuildingPlacementSystem to call UpdateActivePlacementPointer and prevent direct active-placement TryBeginDrag, HandlePointerRelease, and HandlePointerNotPressed orchestration from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Active building placement drag/press/release behavior should remain the same.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-placement-input-editmode-rerun.log

Validation result
- Focused diff check passed.
- First Unity validation caught CS0165 in BuildingPlacementInputSystem; fixed by default-initializing the input grid before delegate evaluation.
- Rerun passed: GameplayArchitectureContractTests 79/79.

Known gaps
- BuildingPlacementSystem still owns non-placement pointer click behavior for building selection.
- BuildingPlacementInputSystem is now 379 lines and remains cohesive; no split recommended yet.

Cross-lane impacts
- No scene, art, UI prefab, or ECS component data changes.
- Architecture test expectations changed for the building placement input boundary.

Next recommended task
Extract BuildingPlacementGridSystem for remaining placement/grid math, including center-origin and placement focus/grid helper behavior still sitting in BuildingPlacementSystem.

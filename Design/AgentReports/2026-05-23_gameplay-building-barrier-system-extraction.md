Lane
Gameplay

Task
Extract BuildingBarrierSystem from BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to assign road barrier gate classification, base-breach memory, enemy wall/gate perimeter lookup, breach-building target selection, barrier door proximity checks, and barrier door visual open-state updates to BuildingBarrierSystem.
- Updated GameplayArchitectureContractTests so those barrier and breach responsibilities cannot drift back into BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Existing road barrier doors should still open for nearby friendly faction units and close when no friendly unit is nearby.
- Existing base-breach targeting should still redirect attackers toward an intact enemy gate first, then wall, unless a matching breach is already open.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-barrier-architecture.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 76/76.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151547507790010.xml
- BuildingPlacementSystem is now 5734 lines after this extraction.

Known gaps
- TryResolveBaseBreachTarget still lives in BuildingPlacementSystem because it also owns pathing and final breach approach-cell resolution. A later AI/pathing slice can move that orchestration once the approach-cell helpers have a clear ECS owner.
- Wall/gate definition classification is still exposed in BuildingPlacementSystem for placement alignment and other legacy callers; BuildingBarrierSystem receives it through explicit context instead of static/singleton access.

Cross-lane impacts
- No art, scene, UI prefab, or AI files were intentionally modified by this slice.
- Existing unrelated dirty UI/art files were left untouched.

Next recommended task
Extract building deletion/destruction orchestration or continue shrinking selected-building resource/capacity query facades.

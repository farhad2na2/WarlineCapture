Lane
Gameplay

Task
Extract BuildingSelectionSystem from BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs
- Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to assign building selection clearing, select-and-focus behavior, selected-building focus position resolution, and runtime building click hit-test/routing to BuildingSelectionSystem.
- Added GameplayArchitectureContractTests coverage that keeps click selection routing, hauler-order routing, and transport-click guard behavior out of BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Building selection by world click, selected-building clearing, post-placement select/focus, selected-building focus position, hauler-order building clicks, and move-order-to-building clicks should behave as before.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-selection-tests.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 75/75.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151536487663760.xml

Known gaps
- BuildingPlacementSystem still has thin facades for ResolveBuildingFocusWorldPosition and SelectAndFocusBuilding because multiple remaining legacy slices call those methods.
- Production request focus still receives selection callbacks through BuildingPlacementSystem context; a later slice can route that directly through BuildingSelectionSystem after production request dependencies are narrowed further.
- BuildingPlacementSystem remains large at 6092 lines.

Cross-lane impacts
- No art, scene, UI prefab, or AI files were intentionally modified by this slice.
- Existing unrelated dirty Armory/POP05/UI files were left untouched.

Next recommended task
Extract selected-building read model/query methods or building deletion/destruction orchestration next. The query slice will reduce facade API clutter; the deletion slice will move more gameplay state mutation out of BuildingPlacementSystem.

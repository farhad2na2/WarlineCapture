Lane
Gameplay

Task
Extract BuildingRuntimeCreationSystem from BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to assign runtime building data creation, runtime registry insertion, blocker/combat entity hookup, runtime link attachment, initial production collections, produced-unit slot array setup, placement redirect side effects, and marker refresh policy to BuildingRuntimeCreationSystem.
- Updated GameplayArchitectureContractTests so runtime building data construction and RuntimeBuildingEntityLink attachment cannot drift back into BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Building placement, wall placement, initial runtime building spawn, marker refresh, blocker creation, combat entity creation, and unit redirect side effects should behave as before.
- BuildingPlacementSystem.RegisterRuntimeBuilding now delegates to BuildingRuntimeCreationSystem.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeCreationSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-runtime-creation-tests.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 74/74.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151529347172170.xml

Known gaps
- BuildingRuntimeCreationSystem still receives callbacks for blocker/combat entity creation, pathing redirect, placement rect calculation, and building visuals because those behaviors belong to neighboring systems or still-existing legacy placement seams.
- BuildingPlacementSystem remains large at 6123 lines.

Cross-lane impacts
- No art, scene, UI prefab, or AI files were intentionally modified by this slice.
- Existing unrelated dirty Armory/POP05/UI files were left untouched.

Next recommended task
Extract runtime building selection/focus facade behavior or runtime building delete/destroy orchestration, depending on whether the next priority is reducing placement UI facade methods or combat/destruction ownership debt.

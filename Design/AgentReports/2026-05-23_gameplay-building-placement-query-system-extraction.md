Lane
Gameplay

Task
Extract BuildingPlacementQuerySystem from BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to assign placement status text, selected-building labels/descriptions, selected-building preview prefab lookup, selected-building health lookup, and selected-building production prefab read models to BuildingPlacementQuerySystem.
- Updated GameplayArchitectureContractTests so selected-building scalar query formatting and health lookup cannot drift back into BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Selected-building labels, descriptions, preview prefab, health display, production prefab lookup, and placement status text should return the same values through existing BuildingPlacementSystem public APIs.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementQuerySystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-placement-query-tests.log

Validation result
- git diff --check passed.
- First Unity test run failed because the new architecture guard regex was too broad and matched unrelated code. The guard was tightened to the exact old formatting line.
- Rerun passed: GameplayArchitectureContractTests 75/75.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151541315200100.xml

Known gaps
- Produced-unit and pending-production list read models remain in BuildingUiQuerySystem.
- Capacity info queries still call FactionResourceSystem directly from BuildingPlacementSystem; those can move in a resource-query slice if we want to keep shrinking selected-building UI facades.
- BuildingPlacementSystem remains large at 6043 lines.

Cross-lane impacts
- No art, scene, UI prefab, or AI files were intentionally modified by this slice.
- Existing unrelated dirty Armory/POP05/UI files were left untouched.

Next recommended task
Extract selected-building resource/capacity query facades or building deletion/destruction orchestration next.

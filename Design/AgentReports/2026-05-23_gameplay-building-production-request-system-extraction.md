Lane
Gameplay

Task
Extract BuildingProductionRequestSystem from BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to assign selected-building unit production request routing, camp item request failure policy, UI production arm consumption, friendly producer lookup, production request focus, and last camp production focus memory to BuildingProductionRequestSystem.
- Added GameplayArchitectureContractTests coverage that prevents those responsibilities from drifting back into BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Existing public BuildingPlacementSystem APIs remain as facades for UI and gameplay callers.
- Unit production requests, camp item requests, production focus, and UI production arming should behave the same through the extracted system.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-production-request-tests.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 74/74.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151515625014450.xml

Known gaps
- TryQueuePlayerUnitFromBuilding remains in BuildingPlacementSystem as the queue mutation bridge. It should be moved into the existing BuildingProductionSystem ownership boundary in a separate focused slice.
- Faction producer lookup remains in BuildingPlacementSystem because this slice targeted the player/UI production request path.
- BuildingPlacementSystem is still large at 6180 lines after this extraction.

Cross-lane impacts
- UI callers keep the same BuildingPlacementSystem public methods.
- No art, scene, or AI source files were modified by this slice.
- Existing unrelated dirty POP05/UI generated files were left untouched.

Next recommended task
Move TryQueuePlayerUnitFromBuilding into BuildingProductionSystem so pending production queue mutation no longer lives in BuildingPlacementSystem.

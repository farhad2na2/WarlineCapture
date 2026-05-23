Lane
Gameplay

Task
Extract BuildingRuntimeQuerySystem from BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to assign runtime building read/query facades, faction building/unit/production counts, building role/id lists, owner/destroyed/city/refugee flags, combat entity info, focus-position queries, and building approach-cell query routing to BuildingRuntimeQuerySystem.
- Updated GameplayArchitectureContractTests so these read/query methods cannot drift back into BuildingPlacementSystem.

User-visible behavior
- Intended no behavior change.
- Existing public BuildingPlacementSystem query APIs remain available for AI, UI, population, and tests.
- Produced-unit counting still prunes stale produced units through BuildingProductionSystem before counting.
- Building approach-cell queries still use the existing pathing helpers through explicit context delegates.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-runtime-query-rerun.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 77/77.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151564363866010.xml
- BuildingPlacementSystem is now 5524 lines after this extraction.

Known gaps
- BuildingPlacementSystem still owns building definition/prefab metadata, configured spawnable lookups, and visual template/bounds helpers.
- TryResolveBaseBreachTarget and lower-level building approach path scoring helpers remain in BuildingPlacementSystem.
- Resource hauler update logic remains a large slice despite several policy helpers already living in ResourceHaulerSystem.

Cross-lane impacts
- No art, scene, UI prefab, or AI files were intentionally modified by this slice.
- Existing unrelated dirty files were left untouched.

Next recommended task
Extract building definition/prefab metadata into a dedicated BuildingDefinitionSystem or BuildingPrefabSystem.

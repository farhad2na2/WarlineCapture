Lane
Gameplay

Task
Step 5: move the backing dollar pool and citizen unit-prefab registry composition out of BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/RuntimeResourceSystem.cs
- Assets/Game/Scripts/Systems/RuntimeResourceSystem.cs.meta
- Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs
- Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs.meta
- Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay-runtime-resource-unit-prefab-boundary.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to require dollar backing storage in RuntimeResourceSystem and unit-prefab composition in RuntimeUnitPrefabSystem.
- Updated GameplayArchitectureContractTests so BuildingPlacementSystem cannot regain _resourceDollars, CreateCitizenResourceContext, or CreateCitizenPrefabContext.
- Updated ManagedGameplayStartupSystem contract coverage to require explicit RuntimeResourceSystem and RuntimeUnitPrefabSystem wiring.

User-visible behavior
- Intended no behavior change.
- CurrentDollars, GetResourceTotals, TrySpendDollars, SetInitialResourceTotals, and existing configured unit prefab facade calls remain available for current UI/gameplay callers.
- Citizen upkeep and citizen prefab loading now receive runtime resource/prefab boundaries from startup composition instead of asking BuildingPlacementSystem to manufacture those contexts.

Validation run
- git diff --check
- Unity EditMode in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step5-runtime-resource-prefab-default-results.log

Validation result
- Passed.
- GameplayArchitectureContractTests: 90 total, 90 passed, 0 failed.
- Result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152089133667230.xml
- Note: explicit -testResults path did not emit XML in this CodexUnity1 run; reran with the project's default TestResults output pattern and confirmed the generated result above.

Known gaps
- BuildingPlacementSystem still exposes compatibility resource and configured-unit facade methods for current callers.
- CitizenPopulationSystem still receives BuildingPlacementSystem for the runtime building query context; only citizen resource and prefab composition moved in this step.
- No PlayMode validation was run because this was a focused architecture boundary/refactor step.

Cross-lane impacts
- UI still reads CurrentDollars through BuildingPlacementSystem compatibility facade, so no UI integration change is required from this step.
- No art, map, or scene contract changed.

Next recommended task
- Step 6: migrate remaining resource/UI read paths and production request dollar hooks toward RuntimeResourceSystem-owned contexts, then continue retiring BuildingPlacementSystem facade access where direct runtime systems are available.

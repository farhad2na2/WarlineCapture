Lane
Gameplay

Task
Move building deletion and destruction orchestration from BuildingPlacementSystem into BuildingCombatSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingCombatSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md to state that building deletion orchestration, destroyed-entity callbacks, destroyed visual toggling, and destroyed building finalization belong in BuildingCombatSystem.
- Updated GameplayArchitectureContractTests so deletion, destroyed callback handling, destroyed cleanup, and selection clearing during destruction are guarded under BuildingCombatSystem ownership.

User-visible behavior
- Intended no behavior change.
- Deleting a selected building should still enter the destroyed state, clear selection through RuntimeBuildingSystem, destroy blocker/combat entities, notify population/minimap, and clean up final visuals after the destroyed lifetime.
- Runtime destroyed-entity callbacks should still remove runtime buildings, clear ECS entity links, and destroy the associated GameObject when needed.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingCombatSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 EditMode test run in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-combat-destruction-rerun2.log

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 76/76.
- Unity test result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151557686907150.xml
- BuildingPlacementSystem is now 5628 lines after this extraction.
- dotnet solution/csproj validation was attempted but is not a reliable gate for this Unity project: the solution has duplicate Unity.Timeline projects, and direct csproj build fails in Unity render-pipeline package code before validating the game assembly.

Known gaps
- BuildingPlacementSystem still owns building visual initialization and owner-faction gate pass updates.
- TryResolveBaseBreachTarget and building approach-cell/pathing helpers remain in BuildingPlacementSystem.
- Resource hauler update logic and building definition/prefab metadata remain large remaining slices.

Cross-lane impacts
- No art, scene, UI prefab, or AI files were intentionally modified by this slice.
- Existing unrelated dirty UI/art files were left untouched.

Next recommended task
Extract BuildingRuntimeQuerySystem for public read/query facade methods, then extract building definition/prefab metadata into a dedicated BuildingDefinitionSystem or BuildingPrefabSystem.

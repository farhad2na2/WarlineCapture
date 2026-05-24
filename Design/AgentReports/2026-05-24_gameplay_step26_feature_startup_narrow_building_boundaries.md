Lane
Gameplay

Task
Step 26 - migrate GameplayFeatureStartupSystem off BuildingPlacementSystem facade access.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/Systems/MenuStartupSystem.cs
- Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to require GameplayFeatureStartupSystem to receive BuildingRuntimeCitySpawnSystem, BuildingPlacementInteractionSystem, and their contexts from managed composition instead of BuildingPlacementSystem.
- Updated GameplayArchitectureContractTests to reject BuildingPlacementSystem/buildingPlacement reach-throughs in GameplayFeatureStartupSystem.

User-visible behavior
- No intended gameplay or UI behavior change.
- RuntimeCitySpawnerSystem still receives the same building runtime city spawn boundary.
- RoadBuildSystem still receives the same building placement interaction boundary.
- BuildingPlacementSystem still receives the same main menu, selection, runtime blocker, runtime city, and citizen dependencies through managed composition callbacks.

Validation run
- git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs Assets/Game/Scripts/Systems/MenuStartupSystem.cs Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step26-feature-startup-architecture.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step26-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingGameplayCompositionSystem and ManagedGameplayStartupSystem still create and carry the temporary BuildingPlacementSystem facade while narrow boundaries finish migrating.
- BuildingPlacementSystem remains 2475 lines.
- GameBootstrap still stores a private BuildingPlacement facade reference for remaining runtime/update compatibility.

Cross-lane impacts
- UI lane menu and gameplay feature startup should use explicit building UI/interaction/runtime city boundaries rather than reaching through BuildingPlacementSystem.
- Existing unrelated UI-lane scene/report changes were left untouched.

Next recommended task
- Step 27: migrate the remaining private GameBootstrap/managed update path off the BuildingPlacementSystem facade by routing runtime updates and remaining composition-owned building callbacks through narrow systems directly, then reassess whether BuildingPlacementSystem can be renamed or deleted as a compatibility shell.

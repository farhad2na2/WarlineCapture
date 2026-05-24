Lane
Gameplay

Task
Step 25 - migrate MenuStartupSystem off BuildingPlacementSystem facade access.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/Systems/MenuStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to require MenuStartupSystem to receive BuildingUiCommandSystem, BuildingUiQuerySystem, BuildingPlacementInteractionSystem, and their contexts from managed composition instead of BuildingPlacementSystem.
- Updated GameplayArchitectureContractTests to reject BuildingPlacementSystem/buildingPlacement reach-throughs in MenuStartupSystem.

User-visible behavior
- No intended gameplay or UI behavior change.
- MenuView still receives the same building UI command/query systems and contexts.
- MainMenuPlayUI, RoadBuildSystem, and RTSSelectionSystem still receive the same building interaction boundary.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/MenuStartupSystem.cs Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step25-menu-startup-architecture.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step25-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingGameplayCompositionSystem still wraps BuildingPlacementSystem to produce UI/interaction contexts and a BindMainMenu callback.
- GameplayFeatureStartupSystem still receives BuildingPlacementSystem and calls buildingPlacement?.* for runtime city spawn and interaction binding.
- BuildingPlacementSystem remains 2475 lines.

Cross-lane impacts
- UI lane should not bind menu startup through BuildingPlacementSystem. Building UI command/query bindings now flow through explicit startup fields.
- Existing unrelated UI-lane scene/shell/editor changes were left untouched.

Next recommended task
- Step 26: migrate GameplayFeatureStartupSystem off BuildingPlacementSystem by passing RuntimeCitySpawnSystem/context, BuildingPlacementInteractionSystem/context, and a feature binding callback from BuildingGameplayCompositionSystem.Result.

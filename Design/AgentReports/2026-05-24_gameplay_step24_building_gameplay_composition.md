Lane
Gameplay

Task
Step 24 - introduce a building gameplay composition boundary so managed startup no longer reaches through BuildingPlacementSystem for child systems and contexts.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs.meta
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to assign managed building gameplay composition to BuildingGameplayCompositionSystem.
- Updated GameplayArchitectureContractTests to require ManagedGameplayStartupSystem to consume BuildingGameplayCompositionSystem.Result instead of calling buildingPlacement.* for child systems, contexts, runtime update delegates, citizen resource contexts, or prefab contexts.

User-visible behavior
- No intended gameplay or UI behavior change.
- GameBootstrap public surface is unchanged from Step 23.
- Managed startup still creates the same building, road, selection, citizen, impostor, and runtime update systems, but building child-system/context extraction is centralized behind BuildingGameplayCompositionSystem.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step24-building-composition-architecture.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step24-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingGameplayCompositionSystem still creates and wraps the legacy BuildingPlacementSystem facade. This is an intermediate composition boundary, not final deletion.
- MenuStartupSystem and GameplayFeatureStartupSystem still receive BuildingPlacementSystem and call buildingPlacement?.* for UI/feature contexts.
- BuildingPlacementSystem remains 2475 lines.

Cross-lane impacts
- UI lane should continue using the narrow UI command/query and selection click boundaries where available. MenuStartup still has facade debt to remove next.
- No unrelated UI/editor files were modified.

Next recommended task
- Step 25: migrate MenuStartupSystem off BuildingPlacementSystem by passing BuildingUiCommandSystem, BuildingUiQuerySystem, BuildingPlacementInteractionSystem, and their contexts from BuildingGameplayCompositionSystem.Result through GameBootstrap/ManagedGameplayStartupSystem.

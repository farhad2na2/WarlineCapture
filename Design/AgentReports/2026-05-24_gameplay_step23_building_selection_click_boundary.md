Lane
Gameplay

Task
Step 23 - extract building selection screen-click routing out of BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs
- Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs
- Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to assign building selection screen-click guards and screen-to-grid click routing to BuildingSelectionClickSystem.
- Updated GameplayArchitectureContractTests to require BuildingSelectionClickSystem and reject a private HandleBuildingSelectionClick method returning to BuildingPlacementSystem.

User-visible behavior
- No intended gameplay behavior change.
- Runtime building-selection clicks still flow through the same cell-level BuildingSelectionSystem behavior.
- GameBootstrap now exposes the narrow BuildingSelectionClickSystem boundary instead of requiring tests to reflect into the private BuildingPlacementSystem facade.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingSelectionClickSystem.cs Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step23-selection-click-architecture.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter GameSceneTransportBoardingPlayModeTests.GameScene_NearbySoldierClickingTransportHelipadArea_WalksAndBoards -logFile /private/tmp/warline-step23-transport-boarding-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- BLOCKED/INCONCLUSIVE: focused PlayMode transport boarding regression failed before reaching the changed click boundary. Failure was "Initial player base must spawn a transport helicopter. Expected: True But was: False" at GameSceneTransportBoardingPlayModeTests.cs:81, after batchmode render target / Entities Graphics root errors during BeginGameplay.

Known gaps
- BuildingPlacementSystem still creates BuildingSelectionClickSystem and its context. This is narrower than the previous private click method, but context ownership still needs to move out of the facade in a later step.
- BuildingPlacementSystem remains 2475 lines.
- Focused PlayMode transport validation needs a stable batchmode setup or a lower-level test that seeds the transport/building state without depending on full initial base spawning.

Cross-lane impacts
- UI/input code should use GameBootstrap.BuildingSelectionClick plus BuildingSelectionClickContext for building click routing, not GameBootstrap.BuildingPlacement.
- Existing unrelated UI-lane files were left untouched.

Next recommended task
- Step 24: introduce a building gameplay composition boundary that creates and exposes narrow building systems/contexts directly, so ManagedGameplayStartupSystem and GameBootstrap stop using BuildingPlacementSystem as the internal service locator for child systems.

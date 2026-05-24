Lane
Gameplay

Task
Step 22 - remove public GameBootstrap exposure of the BuildingPlacementSystem facade while preserving current startup behavior.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs
- Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to state that GameBootstrap must not expose a public BuildingPlacementSystem facade property.
- Added architecture test coverage that rejects public BuildingPlacementSystem exposure from GameBootstrap.

User-visible behavior
- No intended gameplay or UI behavior change.
- BuildingPlacementSystem remains private managed composition debt inside GameBootstrap until the remaining startup/click callers migrate to narrower systems.

Validation run
- git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step22-bootstrap-private-building-placement.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.

Known gaps
- GameSceneTransportBoardingPlayModeTests still reflects into GameBootstrap private BuildingPlacement state to exercise the old private building selection click path. This should be removed when building selection input is extracted behind a narrow public testable boundary.
- BuildingPlacementSystem remains 2474 lines and still exists as a private compatibility facade.

Cross-lane impacts
- UI lane should not bind to GameBootstrap.BuildingPlacement; the public property is gone.
- Existing unrelated UI scene/meta changes were left untouched.

Next recommended task
- Step 23: extract the remaining building selection click/input surface behind a narrow boundary so PlayMode tests and future UI/input code no longer need any BuildingPlacementSystem facade access, public or reflective.

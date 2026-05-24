Lane
Gameplay

Task
Step 27 - remove the private BuildingPlacementSystem facade surface from GameBootstrap and ManagedGameplayStartupSystem.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to require GameBootstrap to hold no public or private BuildingPlacementSystem facade.
- Updated gameplay_solid_ecs_contract.md to isolate temporary BuildingPlacementSystem facade ownership inside BuildingGameplayCompositionSystem until the remaining facade update body is split.
- Updated GameplayArchitectureContractTests to reject GameBootstrap/ManagedGameplayStartupSystem facade fields, assignments, and direct composition-result reach-throughs.

User-visible behavior
- No intended gameplay or UI behavior change.
- GameBootstrap still receives the same building selection click, runtime city spawn, UI command/query, interaction, and runtime update boundaries.
- BuildingPlacementSystem disposal now flows through a managed composition callback instead of a private GameBootstrap facade field.

Validation run
- git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step27-private-facade-architecture.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step27-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingGameplayCompositionSystem still owns the temporary BuildingPlacementSystem facade internally.
- BuildingRuntimeUpdateSystem.Context still invokes the facade update delegate supplied by BuildingGameplayCompositionSystem.
- BuildingPlacementSystem remains 2475 lines because this step removed caller ownership rather than extracting the update body.

Cross-lane impacts
- UI and bootstrap callers should not reintroduce a BuildingPlacementSystem field. Use the narrow building systems/contexts supplied by managed composition.
- Existing unrelated UI-lane scene/report changes were left untouched.

Next recommended task
- Step 28: split BuildingPlacementSystem.Update into a narrow runtime tick boundary so BuildingRuntimeUpdateSystem can invoke explicit update phases instead of a facade Update delegate.

Lane
Gameplay

Task
Move BuildingPlacementRuntimeTickSystem.Context construction out of BuildingPlacementSystem and remove the BuildingPlacementSystem.Update compatibility wrapper.

Files changed
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs
- Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to require runtime tick context assembly in BuildingPlacementRuntimeTickContextSystem.
- Updated GameplayArchitectureContractTests to reject BuildingPlacementSystem.Update and CreateBuildingPlacementRuntimeTickContext.
- Updated runtime-boundary tests/helpers to call the runtime tick path without using BuildingPlacementSystem.Update.

User-visible behavior
- No intended gameplay or UI behavior change.
- Runtime update still flows through BuildingRuntimeUpdateSystem -> BuildingPlacementRuntimeTickSystem with the same per-frame work order.
- BuildingPlacementSystem no longer exposes a runtime Update method.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs.meta Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step29-runtime-tick-context-architecture.log
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges -logFile /private/tmp/warline-step29-runtime-boundary-validation.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step29-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BuildingRuntimeBoundaryValidationTests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges passed 1/1.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingGameplayCompositionSystem still owns the temporary BuildingPlacementSystem facade internally.
- BuildingPlacementSystem still exposes internal runtime tick hooks used by BuildingPlacementRuntimeTickContextSystem.
- A test-only TickRuntimeForTests hook remains so editor tests can tick the runtime boundary without exposing internal systems publicly.
- BuildingPlacementSystem is 2390 lines after this step.

Cross-lane impacts
- Tests and gameplay callers should not call BuildingPlacementSystem.Update; that method is removed.
- Existing unrelated UI-lane editor/capture changes were left untouched.

Next recommended task
- Move runtime tick hook ownership out by extracting the remaining tick-phase methods from BuildingPlacementSystem into narrower systems/contexts, starting with runtime boundary publish and resource/production tick phases.

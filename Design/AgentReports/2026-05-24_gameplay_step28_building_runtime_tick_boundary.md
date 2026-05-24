Lane
Gameplay

Task
Split BuildingPlacementSystem.Update into a narrow runtime tick boundary so BuildingRuntimeUpdateSystem stops invoking a facade Update delegate.

Files changed
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to require BuildingRuntimeUpdateSystem to invoke a narrow runtime tick callback instead of BuildingPlacementSystem.Update.
- Added BuildingPlacementRuntimeTickSystem ownership for per-frame building runtime orchestration, runtime boundary publish tick, diagnostics timing, and placement pointer/click frame flow.
- Updated GameplayArchitectureContractTests to reject placementFacade.Update as the runtime update callback.

User-visible behavior
- No intended gameplay or UI behavior change.
- Runtime update order is preserved: production, resources, haulers, resource visuals, spawn reservation cleanup, destroyed building sync, road barrier doors, marker refresh, ECS boundary publish, then placement pointer/building click handling.
- Existing BuildingPlacementSystem.Update remains as a compatibility wrapper for tests/manual callers, but BuildingRuntimeUpdateSystem no longer receives or invokes that facade method.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs.meta Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step28-runtime-tick-architecture.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step28-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingPlacementSystem still creates the BuildingPlacementRuntimeTickSystem.Context and keeps a compatibility Update wrapper.
- BuildingGameplayCompositionSystem still owns the temporary BuildingPlacementSystem facade internally.
- BuildingPlacementSystem reduced from 2475 to 2378 lines in this step; more extraction is still needed before the facade can be deleted.

Cross-lane impacts
- Runtime/update callers should route through BuildingRuntimeUpdateSystem and BuildingPlacementRuntimeTickSystem, not BuildingPlacementSystem.Update.
- Existing unrelated UI-lane editor changes were left untouched.

Next recommended task
- Split the BuildingPlacementRuntimeTickSystem.Context construction out of BuildingPlacementSystem by moving runtime tick context assembly into BuildingGameplayCompositionSystem or a narrower context factory, then remove the BuildingPlacementSystem.Update compatibility wrapper after editor tests stop calling it directly.

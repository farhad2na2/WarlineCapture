Lane
Gameplay

Task
Extract tick-phase hooks by domain, starting with runtime boundary publish and resource/production tick phases.

Files changed
- Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated gameplay_solid_ecs_contract.md to assign production progress ticking, resource production ticking, resource hauler ticking, and spawn reservation cleanup to BuildingProductionRuntimeTickSystem.
- Updated gameplay_solid_ecs_contract.md to assign runtime boundary publish ticking to BuildingRuntimeBoundaryPublishSystem.
- Updated GameplayArchitectureContractTests to reject production/resource/boundary tick ownership in BuildingPlacementSystem.

User-visible behavior
- No intended gameplay or UI behavior change.
- Building runtime frame order remains unchanged: production, resources, haulers, resource visuals, spawn reservation cleanup, destroyed building sync, destroyed building update, barrier doors, marker refresh, runtime boundary publish, then pointer/click handling.
- Runtime spawn request processing still completes through BuildingRuntimeBoundarySystem via the new boundary publish tick system.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickSystem.cs.meta Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryPublishSystem.cs.meta Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step30-tick-domain-architecture.log
- Unity EditMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges -logFile /private/tmp/warline-step30-runtime-boundary-validation.log
- Unity PlayMode in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest -logFile /private/tmp/warline-step30-bootstrap-awake-playmode.log

Validation result
- PASS: git diff --check produced no issues.
- PASS: GameplayArchitectureContractTests passed 93/93.
- PASS: BuildingRuntimeBoundaryValidationTests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges passed 1/1.
- PASS: BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest passed 1/1.

Known gaps
- BuildingPlacementSystem still exposes lower-level internal systems/contexts to BuildingPlacementRuntimeTickContextSystem while the temporary facade exists.
- BuildingPlacementSystem still owns resource/production fields and context factories; this step moved the tick orchestration, not all data ownership.
- BuildingPlacementSystem is 2348 lines after this step.

Cross-lane impacts
- Runtime tick code should use BuildingProductionRuntimeTickSystem and BuildingRuntimeBoundaryPublishSystem instead of adding new per-frame work to BuildingPlacementSystem.
- Existing unrelated UI-lane report changes were left untouched.

Next recommended task
- Continue domain extraction by moving runtime visual/destroyed-building tick hooks out of BuildingPlacementSystem: UpdateBuildingResourceVisuals, SyncDestroyedRuntimeBuildingCombatEntities, and UpdateDestroyedBuildings.

Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 7: Move disposal ownership.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayDisposalSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayDisposalSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step7_disposal_ownership.md

Contracts touched
- BuildingGameplay roadmap now marks step 7 complete and records the 2041-line transition ceiling.
- Gameplay SOLID/ECS contract now requires building disposal ownership to route through BuildingGameplayDisposalSystem, not BuildingGameplaySystem.Dispose.
- Architecture tests now guard that production composition does not call building.Dispose and that disposal logic lives in BuildingGameplayDisposalSystem.

User-visible behavior
- No intended gameplay behavior change.
- Production disposal still exits build mode, destroys runtime building objects/entities, clears the runtime building registry, and disposes placement startup/preview state.

Validation run
- Unity batch architecture validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation -logFile /private/tmp/warlinecapture-building-gameplay-arch-step7.log
- Unity editmode focused test in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -testResults /private/tmp/warlinecapture-building-runtime-boundary-step7.xml -logFile /private/tmp/warlinecapture-building-runtime-boundary-step7.log
- Scoped git diff check for files touched by this step.

Validation result
- Passed: [BuildingGameplayArchitectureValidation] result=Passed methods=9.
- Passed: BuildingRuntimeBoundaryValidationTests total=1 passed=1 failed=0.
- Passed: scoped git diff --check for this step's files.

Known gaps
- BuildingGameplaySystem remains a temporary broad shell at 2041 lines.
- BuildingGameplaySystem.Dispose remains as a temporary compatibility wrapper for tests/legacy callers until test harness migration.
- ECS query ownership still remains in BuildingGameplaySystem and is the next planned extraction.

Cross-lane impacts
- None expected for UI, AI, Art, or Road. The public composition dispose action remains stable.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 8: Extract ECS query ownership into BuildingGameplayEcsQuerySystem.

Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 6: Move placement startup/config wiring.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeObjectSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeObjectSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step6_startup_config.md

Contracts touched
- BuildingGameplay roadmap now marks step 6 complete and records the 2049-line transition ceiling.
- Gameplay SOLID/ECS contract now requires building placement startup/config wiring to route directly from composition into BuildingPlacementStartupSystem and BuildingGameplayDependencySystem, not through BuildingGameplaySystem.Init.
- Architecture tests now guard composition startup wiring, road-footprint query ownership, and the temporary BuildingGameplaySystem line ceiling.

User-visible behavior
- No intended gameplay behavior change.
- Production composition still initializes the same placement config, world camera, runtime root, road footprint query/context, faction visuals, and day/night dependencies, but does so through narrow systems before runtime contexts are created.

Validation run
- Unity batch architecture validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation -logFile /private/tmp/warlinecapture-building-gameplay-arch-step6.log
- Unity batch road-build architecture regression in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-road-build-arch-after-building-step6.log
- Unity editmode focused test in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -testResults /private/tmp/warlinecapture-building-runtime-boundary-step6.xml -logFile /private/tmp/warlinecapture-building-runtime-boundary-step6.log
- Scoped git diff check for files touched by this step.

Validation result
- Passed: [BuildingGameplayArchitectureValidation] result=Passed methods=8.
- Passed: [RoadBuildArchitectureValidation] result=Passed methods=31.
- Passed: BuildingRuntimeBoundaryValidationTests total=1 passed=1 failed=0.
- Passed: scoped git diff --check for this step's files.
- Full git diff --check is currently blocked by unrelated pre-existing trailing whitespace in Assets/Game/Scenes/Game_Terrain4.unity.

Known gaps
- BuildingGameplaySystem remains a temporary broad shell at 2049 lines.
- BuildingGameplaySystem.Init remains as temporary compatibility debt for tests/legacy callers, but production composition no longer calls it.
- Disposal still routes through the shell and is the next planned ownership extraction.

Cross-lane impacts
- Road-build architecture assertions were updated to reflect road footprint query ownership moving into BuildingPlacementStartupSystem while still using the same RoadFootprintQuerySystem boundary.
- No expected UI, AI, or Art behavior change.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 7: Move disposal ownership so composition disposes runtime objects through owning systems directly and BuildingGameplaySystem.Dispose stops being the disposal gateway.

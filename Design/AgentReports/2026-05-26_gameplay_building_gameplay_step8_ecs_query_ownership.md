Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 8: Extract ECS query ownership.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayEcsQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayEcsQuerySystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step8_ecs_query_ownership.md

Contracts touched
- BuildingGameplay roadmap now marks step 8 complete and records the 1982-line transition ceiling.
- Gameplay SOLID/ECS contract now requires building ECS query caching to live in BuildingGameplayEcsQuerySystem, not BuildingGameplaySystem.
- Architecture tests now guard that BuildingGameplaySystem does not declare the query cache fields or create ECS queries directly.

User-visible behavior
- No intended gameplay behavior change.
- Existing runtime contexts still receive the same query handles/delegates, but those handles are now owned by BuildingGameplayEcsQuerySystem.

Validation run
- Unity batch architecture validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation -logFile /private/tmp/warlinecapture-building-gameplay-arch-step8.log
- Unity editmode focused test in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -testResults /private/tmp/warlinecapture-building-runtime-boundary-step8.xml -logFile /private/tmp/warlinecapture-building-runtime-boundary-step8.log
- Scoped git diff check for files touched by this step.

Validation result
- Passed: [BuildingGameplayArchitectureValidation] result=Passed methods=10.
- Passed: BuildingRuntimeBoundaryValidationTests total=1 passed=1 failed=0.
- Passed: scoped git diff --check for this step's files.

Known gaps
- BuildingGameplaySystem remains a temporary broad shell at 1982 lines.
- BuildingGameplaySystem still owns grid-data helper methods and passes query handles/delegates into context factories.
- Step 9 should move grid data access into explicit query/input systems and reduce the shell further.

Cross-lane impacts
- None expected for UI, AI, Art, or Road. Existing query handles and runtime behavior are preserved.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 9: Extract grid data access.

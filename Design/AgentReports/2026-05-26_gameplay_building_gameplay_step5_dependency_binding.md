Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 5: Extract building dependency binding.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayDependencySystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayDependencySystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step5_dependency_binding.md

Contracts touched
- BuildingGameplaySystem refactor roadmap now marks step 5 complete and records the 2071-line transition ceiling.
- Gameplay architecture contract tests now require dependency references to live in BuildingGameplayDependencySystem instead of direct BuildingGameplaySystem fields.

User-visible behavior
- No intended gameplay behavior change.
- Building dependency references for menu, selection camera, selection building interaction, runtime grid blockers, runtime city, citizen population, faction visuals, and day/night are still bound through the existing public startup methods, but storage and callbacks now route through BuildingGameplayDependencySystem.

Validation run
- Unity batch architecture validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation -logFile /private/tmp/warlinecapture-building-gameplay-arch-step5.log
- Unity editmode focused test in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BuildingRuntimeBoundaryValidationTests -testResults /private/tmp/warlinecapture-building-runtime-boundary-step5.xml -logFile /private/tmp/warlinecapture-building-runtime-boundary-step5.log
- git diff --check

Validation result
- Passed: [BuildingGameplayArchitectureValidation] result=Passed methods=7.
- Passed: BuildingRuntimeBoundaryValidationTests total=1 passed=1 failed=0.
- Passed: git diff --check.

Known gaps
- BuildingGameplaySystem remains a temporary broad shell at 2071 lines.
- Road footprint query storage and placement startup/config gateway still remain in the shell until roadmap step 6.
- Runtime/context factories still read dependency callbacks through the shell and dependency system until later extraction phases remove those factories.

Cross-lane impacts
- None expected for UI, Art, or AI. Public startup methods and runtime behavior are preserved.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 6: Move placement startup/config wiring into BuildingPlacementStartupSystem plus dependency systems directly from composition.

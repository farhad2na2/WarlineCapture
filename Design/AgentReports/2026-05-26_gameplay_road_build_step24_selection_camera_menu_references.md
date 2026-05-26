Lane
Gameplay

Task
RoadBuildSystem refactor step 24: migrate selection/camera/menu references away from the broad RoadBuildSystem shell.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs
- Assets/Game/Scripts/Systems/MenuStartupSystem.cs
- Assets/Game/Scripts/UI/MainMenuPlayUI.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- Road build architecture roadmap now marks step 24 complete.
- Road build architecture batch validation now includes SelectionCameraMenuRuntimeCallersMustUseRoadBoundaries.
- MainMenuPlayUI no longer accepts RoadBuildSystem in Init.
- GameplayRuntimeUpdateSystem receives narrow road update/gui actions instead of RoadBuildSystem.
- MenuStartupSystem receives a narrow road menu binding action instead of RoadBuildSystem.

User-visible behavior
No intended behavior change. Road build runtime update, delete prompt GUI, menu binding, selection camera read state, and main menu startup remain wired through existing bootstrap composition.

Validation run
- git diff --check for changed step 24 files.
- Unity batch validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step24-architecture.log

Validation result
Passed. Log reports: [RoadBuildArchitectureValidation] result=Passed methods=26.

Known gaps
- GameBootstrap and ManagedGameplayStartupSystem still hold/construct RoadBuildSystem as temporary composition debt.
- GameplayFeatureStartupSystem still receives RoadBuildSystem for remaining road runtime generation and dependency binding until the composition phase.
- Runtime update still invokes RoadBuildSystem.Update/OnGui through temporary delegates; step 27 replaces those with narrow road systems.

Cross-lane impacts
None expected. This is gameplay architecture wiring only.

Next recommended task
RoadBuildSystem refactor step 25: create temporary RoadBuildCompositionSystem so extracted road-system construction and wiring move out of ManagedGameplayStartupSystem before deleting the shell.

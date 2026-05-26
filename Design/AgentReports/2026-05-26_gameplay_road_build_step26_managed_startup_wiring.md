Lane
Gameplay

Task
RoadBuildSystem refactor step 26: move managed startup wiring off RoadBuildSystem.

Files changed
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- Road build architecture roadmap now marks step 26 complete.
- Road build architecture batch validation now includes RoadBuildManagedStartupWiringMustUseRoadCompositionBoundaries.
- GameBootstrap no longer stores RoadBuildSystem.
- GameplayFeatureStartupSystem now receives RoadRuntimeGenerationSystem plus context and a narrow road gameplay bind action instead of RoadBuildSystem.
- BuildingGameplayCompositionSystem now receives RoadFootprintQuerySystem plus context instead of RoadBuildSystem.
- ManagedGameplayStartupSystem now consumes road composition boundaries/actions instead of passing RoadBuildSystem through startup wiring.

User-visible behavior
No intended behavior change. Road runtime update, road IMGUI, road disposal, runtime-city road generation, building footprint validation, and road menu/runtime binding still route to the same road implementation through temporary composition actions.

Validation run
- git diff --check for changed step 26 files.
- Unity batch validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step26-architecture.log

Validation result
Passed. Log reports: [RoadBuildArchitectureValidation] result=Passed methods=28.

Known gaps
- RoadBuildCompositionSystem still temporarily constructs and owns the RoadBuildSystem shell.
- Runtime update and IMGUI still run through temporary actions backed by RoadBuildSystem.Update/OnGui until step 27 replaces them with narrow road systems.
- RoadBuildSystem.cs remains until step 28.

Cross-lane impacts
None expected. This change is gameplay architecture wiring only.

Next recommended task
RoadBuildSystem refactor step 27: replace runtime update and GUI delegates with narrow road input/session/projection/delete-prompt systems so the runtime loop no longer depends on RoadBuildSystem.Update or RoadBuildSystem.OnGui, even through actions.

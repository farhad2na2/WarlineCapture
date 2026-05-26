Lane
Gameplay

Task
RoadBuildSystem refactor step 27: replace runtime update and GUI delegates.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- Road build architecture roadmap now marks step 27 complete.
- Road build architecture batch validation now includes RoadBuildRuntimeUpdateAndGuiMustUseNarrowSystems.
- RoadBuildSystem exposes internal RoadBuildInputSystem/RoadBuildInputContext/RoadBuildInputCamera and RoadDeletePromptSystem/RoadDeletePromptContext boundaries.
- RoadBuildCompositionSystem runtime update action now calls RoadBuildInputSystem.Update directly.
- RoadBuildCompositionSystem GUI action now calls RoadDeletePromptSystem.OnGui directly.

User-visible behavior
No intended behavior change. Road pointer input and delete-road modal behavior remain routed to the same extracted systems.

Validation run
- git diff --check for changed step 27 files.
- Unity batch validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step27-architecture.log

Validation result
Passed. Log reports: [RoadBuildArchitectureValidation] result=Passed methods=29.

Known gaps
- RoadBuildSystem.cs still exists and still has temporary public Update/OnGui wrappers until the delete step.
- RoadBuildCompositionSystem still constructs the temporary shell and uses it as the state/context source.

Cross-lane impacts
None expected. This is gameplay architecture wiring only.

Next recommended task
RoadBuildSystem refactor step 28: delete RoadBuildSystem.cs and fix remaining production/test references.

Lane
Gameplay

Task
RoadBuildSystem refactor step 28: delete RoadBuildSystem.cs and fix remaining compile references.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadBuildRuntimeStateSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildRuntimeStateSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- Road build architecture roadmap now marks step 28 complete.
- Road build architecture batch validation now includes RoadBuildSystemSourceMustBeDeletedAndRuntimeStateRenamed.
- RoadBuildSystem.cs and RoadBuildSystem.cs.meta are deleted.
- Temporary road state moved to RoadBuildRuntimeStateSystem.cs.
- Production source no longer references the RoadBuildSystem type. RoadBuildSystemConfig remains unchanged as serialized config compatibility debt.

User-visible behavior
No intended behavior change. Road state, input, delete prompt, runtime generation, footprint queries, visuals, and disposal are preserved through the renamed runtime state holder and existing road composition wiring.

Validation run
- git diff --check for changed step 28 files.
- Exact production type scan: no `RoadBuildSystem` type references under Assets/Game/Scripts.
- Unity batch validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step28-architecture.log

Validation result
Passed. Log reports: [RoadBuildArchitectureValidation] result=Passed methods=30.

Known gaps
- RoadBuildRuntimeStateSystem is still a temporary state holder and remains broad. Step 29 should remove temporary architecture allowances and harden the guard so RoadBuildSystem.cs cannot return.
- RoadBuildSystemConfig naming remains by design as serialized config compatibility debt.

Cross-lane impacts
Tests that used the old BuildingGameplaySystem road-build compatibility overload now route through an object compatibility overload with no RoadBuildSystem type dependency.

Next recommended task
RoadBuildSystem refactor step 29: remove temporary architecture allowances, add hard guards against RoadBuildSystem.cs restoration, and update contract wording around the remaining RoadBuildRuntimeStateSystem debt.

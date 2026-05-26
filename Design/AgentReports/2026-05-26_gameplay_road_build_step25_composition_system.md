Lane
Gameplay

Task
RoadBuildSystem refactor step 25: create temporary RoadBuildCompositionSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs.meta
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- Road build architecture roadmap now marks step 25 complete.
- Road build architecture batch validation now includes RoadBuildCompositionSystemMustOwnTemporaryRoadShellConstruction.
- Temporary RoadBuildSystem construction is now allowed only in RoadBuildCompositionSystem.
- ManagedGameplayStartupSystem now consumes RoadBuildCompositionSystem.Result instead of constructing RoadBuildSystem/RoadBuildReadModelSystem directly.

User-visible behavior
No intended behavior change. Road build initialization, read-model projection, selection startup, and building interaction binding remain wired with the same inputs.

Validation run
- git diff --check for changed step 25 files.
- Unity batch validation in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step25-architecture.log

Validation result
Passed. Log reports: [RoadBuildArchitectureValidation] result=Passed methods=27.

Known gaps
- RoadBuildCompositionSystem is intentionally temporary and still constructs the RoadBuildSystem shell.
- GameBootstrap and feature startup still hold/pass the shell until step 26 migrates managed startup wiring to extracted road boundaries.
- Runtime update and IMGUI still delegate through the shell until step 27 replaces those delegates.

Cross-lane impacts
None expected. This change is gameplay architecture wiring only.

Next recommended task
RoadBuildSystem refactor step 26: move managed startup wiring off RoadBuildSystem by exposing extracted road boundary results from RoadBuildCompositionSystem and updating ManagedGameplayStartupSystem/GameBootstrap/feature startup consumers.

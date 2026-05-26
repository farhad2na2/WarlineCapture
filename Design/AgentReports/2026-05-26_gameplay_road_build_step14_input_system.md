Lane
Gameplay

Task
RoadBuildSystem refactor roadmap step 14: extract road pointer/input processing into RoadBuildInputSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildInputSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildInputSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- RoadBuild architecture validation now includes RoadBuildInputMustLiveInRoadBuildInputSystem.
- RoadPathPlanning and RoadBuildSession contract expectations now allow their runtime consumers to be RoadBuildInputSystem instead of requiring those calls to stay on RoadBuildSystem.
- RoadBuild roadmap step 14 marked Complete.

User-visible behavior
- No intended user-visible behavior change.
- Road drawing, drag-axis selection, clicked-road delete prompt selection, and building-placement drag handoff should behave as before through the existing RoadBuildSystem public entry point.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadBuildInputSystem.cs Assets/Game/Scripts/Systems/RoadBuildInputSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md
- rg forbidden RoadBuild input ownership tokens in Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step14-architecture.log

Validation result
- Passed.
- Unity RoadBuild architecture validation: [RoadBuildArchitectureValidation] result=Passed methods=16.
- RoadBuildSystem.cs reduced to 1568 lines; RoadBuildInputSystem.cs added at 218 lines.

Known gaps
- RoadBuildSystem.Update() is now only a thin input delegation wrapper, but GameplayRuntimeUpdateSystem still reaches the RoadBuildSystem shell until later roadmap steps remove the shell.
- Pointer-over-UI handling remains a placeholder returning false, preserved from previous behavior.

Cross-lane impacts
- None expected. This is a gameplay architecture extraction with no serialized scene or prefab changes.

Next recommended task
RoadBuild roadmap step 15: create RoadBuildCommandSystem for public road build commands and the static SetBuildMode replacement path.

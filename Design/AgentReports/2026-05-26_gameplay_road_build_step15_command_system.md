Lane
Gameplay

Task
RoadBuildSystem refactor roadmap step 15: extract road build command behavior into RoadBuildCommandSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- RoadBuild architecture validation now includes RoadBuildCommandsMustLiveInRoadBuildCommandSystem.
- RoadBuildSession contract expectations now allow road session command calls through RoadBuildCommandSystem.
- RoadBuild roadmap step 15 marked Complete.

User-visible behavior
- No intended user-visible behavior change.
- Road build activation, road session confirm/cancel, exit build mode, and mission-gated build-mode enable behavior should remain the same.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs Design/Architecture/road_build_system_refactor_roadmap.md
- rg direct legacy road command/static build-mode ownership tokens in Assets/Game/Scripts and the campaign mission test.
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step15-architecture.log

Validation result
- Passed.
- Unity RoadBuild architecture validation: [RoadBuildArchitectureValidation] result=Passed methods=17.
- RoadBuildSystem.cs is 1581 lines; RoadBuildCommandSystem.cs added at 59 lines.

Known gaps
- RoadBuildSystem still keeps compatibility wrapper methods for road commands until later shell-deletion steps migrate callers fully away.
- The legacy static RoadBuildSystem.SetBuildMode(bool) still exists as a compatibility facade, but its logic now delegates to RoadBuildCommandSystem and the campaign mission test uses RoadBuildCommandSystem directly.

Cross-lane impacts
- None expected. This is a gameplay command-boundary extraction with no scene, prefab, or serialized data changes.

Next recommended task
RoadBuild roadmap step 16: create RoadDeletePromptSystem for delete-road modal state and result handling.

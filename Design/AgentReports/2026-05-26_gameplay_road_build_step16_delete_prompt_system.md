Lane
Gameplay

Task
RoadBuildSystem refactor roadmap step 16: extract delete-road modal state and result handling into RoadDeletePromptSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadDeletePromptSystem.cs
- Assets/Game/Scripts/Systems/RoadDeletePromptSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- RoadBuild architecture validation now includes RoadDeletePromptMustLiveInRoadDeletePromptSystem.
- RoadBuildSession contract expectations now allow delete-prompt session calls through RoadDeletePromptSystem.
- RoadBuild roadmap step 16 marked Complete.

User-visible behavior
- No intended user-visible behavior change.
- The delete-road confirmation modal still uses the same IMGUI layout, title, copy, Delete button, and Cancel button.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadDeletePromptSystem.cs Assets/Game/Scripts/Systems/RoadDeletePromptSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md
- rg forbidden delete-prompt IMGUI/session tokens in Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step16-architecture.log

Validation result
- Passed.
- Unity RoadBuild architecture validation: [RoadBuildArchitectureValidation] result=Passed methods=18.
- RoadBuildSystem.cs is 1552 lines; RoadDeletePromptSystem.cs added at 73 lines.

Known gaps
- RoadBuildSystem.OnGui() remains as a temporary wrapper because GameplayRuntimeUpdateSystem still calls the road shell OnGui path.
- The delete prompt still uses legacy IMGUI; this step moved ownership, it did not replace the UI surface.

Cross-lane impacts
- None expected. This is a gameplay architecture extraction with no scene, prefab, or serialized data changes.

Next recommended task
RoadBuild roadmap step 17: move soldier-base placement commands to building gameplay.

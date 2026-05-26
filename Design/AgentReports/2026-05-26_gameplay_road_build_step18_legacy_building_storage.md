Lane
Gameplay

Task
RoadBuildSystem refactor roadmap step 18: move legacy runtime building storage out of RoadBuildSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingRoadLegacyStorageSystem.cs
- Assets/Game/Scripts/Systems/BuildingRoadLegacyStorageSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- RoadBuild architecture validation now includes RoadBuildLegacyBuildingStorageMustLiveInBuildingRoadLegacyStorageSystem.
- RoadBuild roadmap step 18 marked Complete.

User-visible behavior
- No intended user-visible behavior change.
- Legacy road-shell building compatibility still exists, but its runtime building storage, selected-building id, active placement state, and soldier-base definition are owned by BuildingRoadLegacyStorageSystem.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/BuildingRoadLegacyStorageSystem.cs Assets/Game/Scripts/Systems/BuildingRoadLegacyStorageSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md
- rg forbidden legacy building storage tokens in Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step18-architecture.log

Validation result
- Passed.
- Unity RoadBuild architecture validation: [RoadBuildArchitectureValidation] result=Passed methods=20.
- RoadBuildSystem.cs is 1472 lines; BuildingRoadLegacyStorageSystem.cs added at 99 lines.

Known gaps
- RoadBuildSystem still contains legacy building ECS creation/helper behavior; that is the next roadmap step.
- RuntimeBuildingEntityLink still has the old RoadBuildSystem Configure overload for now, but RoadBuildSystem no longer uses it.

Cross-lane impacts
- Building-road compatibility storage now uses the existing building-domain data contracts: BuildingDefinition, RuntimeBuildingData, BuildingPlacementLifecycleSystem.PlacementState, and RuntimeBuildingSystem<RuntimeBuildingData>.

Next recommended task
RoadBuild roadmap step 19: move building ECS creation helpers out of road build.

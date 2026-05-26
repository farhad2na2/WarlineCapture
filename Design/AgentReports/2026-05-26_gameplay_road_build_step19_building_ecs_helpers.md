Lane: Gameplay

Task: RoadBuildSystem refactor step 19 - move building ECS creation helpers out of road build.

Files changed:
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/BuildingRoadLegacyEcsSystem.cs
- Assets/Game/Scripts/Systems/BuildingRoadLegacyEcsSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step19_building_ecs_helpers.md

Contracts touched:
- Added RoadBuild architecture contract coverage for step 19.
- RoadBuildSystem must not own legacy building blocker/combat entity creation, runtime link attachment, or spawn-near-building helper logic.
- Legacy road building ECS compatibility is now owned by BuildingRoadLegacyEcsSystem.

User-visible behavior:
- No intended gameplay behavior change.
- Legacy road building placement still creates blocker/combat ECS entities and runtime entity links through the compatibility path.

Validation run:
- git diff --check for RoadBuild step 19 files.
- Local forbidden-token scan for direct building ECS helper ownership in RoadBuildSystem.
- Unity batch validation in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step19-architecture.log

Validation result:
- Passed.
- Unity log: [RoadBuildArchitectureValidation] result=Passed methods=21
- RoadBuildSystem line count after step 19: 1328.

Known gaps:
- RoadBuildSystem still contains legacy building selection/delete fallback code and road-to-building destruction callback compatibility.
- The next roadmap step should remove road-to-building compatibility callbacks.

Cross-lane impacts:
- Building lane now owns the legacy ECS creation helper boundary used by road compatibility code.
- No art, UI, scene, or runtime-city behavior was changed.

Next recommended task:
- Step 20: Remove road-to-building compatibility callbacks, especially HandleRuntimeBuildingEntityDestroyed road callback and RuntimeBuildingEntityLink road reach-through.

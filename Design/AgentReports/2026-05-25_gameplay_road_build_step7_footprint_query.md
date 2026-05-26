# WarlineCapture Handoff

Lane
Gameplay

Task
RoadBuildSystem refactor step 7: extract road footprint query ownership into RoadFootprintQuerySystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadFootprintQuerySystem.cs
- Assets/Game/Scripts/Systems/RoadFootprintQuerySystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-25_gameplay_road_build_step7_footprint_query.md

Contracts touched
- RoadBuild refactor roadmap step 7 marked complete.
- Gameplay architecture test batch now requires RoadFootprintQuerySystem to own road footprint queries, footprint visitors, dirt/sidewalk classification, grid center bounds checks, and bounds transforms.
- RoadBuildSystem is guarded against re-owning private footprint helper classes, footprint visitors, marker classification, or bounds/grid helper methods.

User-visible behavior
No intended behavior change. Building road-overlap checks, road footprint masks, ECS road/sidewalk/dirt projection footprint visiting, and runtime blocker cleanup should keep the same footprint behavior.

Validation run
- git diff --check for touched RoadBuild step 7 files.
- RoadBuildSystem/RoadFootprintQuerySystem line count check.
- Ownership token audit for private footprint helpers and marker classification.
- Unity batchmode in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step7-architecture.log

Validation result
Passed. RoadBuildArchitectureValidation result=Passed methods=9. RoadBuildSystem is 3299 lines; RoadFootprintQuerySystem is 401 lines.

Known gaps
- Road-to-ECS projection loops still live in RoadBuildSystem until RoadGridProjectionSystem.
- Combined road visual data is temporarily nested in RoadFootprintQuerySystem and still consumed by chunk rendering until RoadVisualVariantSystem/RoadChunkVisualSystem.
- BuildingGameplaySystem still calls RoadBuildSystem compatibility wrappers until roadmap step 23 migrates callers to RoadFootprintQuerySystem directly.

Cross-lane impacts
- Building gameplay should see no API break because HasRoadInFootprint and FillRoadFootprintMask wrappers remain.
- RoadGridProjectionSystem can now reuse RoadFootprintQuerySystem for footprint visiting instead of re-implementing bounds math.

Next recommended task
Step 8: create RoadGridProjectionSystem for EntityQuery caching, GridRoad/GridRoadSidewalk/GridRoadDirt buffer writes, deferred road ECS sync, and invalidated-handle safety.

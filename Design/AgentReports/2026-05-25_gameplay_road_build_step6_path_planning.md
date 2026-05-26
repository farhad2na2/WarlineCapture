# WarlineCapture Handoff

Lane
Gameplay

Task
RoadBuildSystem refactor step 6: extract path planning and preview mask planning into RoadPathPlanningSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadPathPlanningSystem.cs
- Assets/Game/Scripts/Systems/RoadPathPlanningSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-25_gameplay_road_build_step6_path_planning.md

Contracts touched
- RoadBuild refactor roadmap step 6 marked complete.
- Gameplay architecture test batch now requires RoadPathPlanningSystem to own drag-axis resolution, path building, preview proposed-edge planning, endpoint preview expansion, and preview mask construction.
- RoadBuildSystem is guarded against re-owning DragFirstAxis, BuildPath, AppendStraightSegment, endpoint preview expansion, preview mask construction, or preview-edge checks.

User-visible behavior
No intended behavior change. Road drag previews and released road strokes should use the same straight/L-shaped path rules and endpoint connection behavior as before.

Validation run
- git diff --check for touched RoadBuild step 6 files.
- RoadBuildSystem/RoadPathPlanningSystem line count check.
- Ownership token audit for path-planning and preview-mask helpers.
- Unity batchmode in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step6-architecture.log

Validation result
Passed. RoadBuildArchitectureValidation result=Passed methods=8. RoadBuildSystem is 3602 lines; RoadPathPlanningSystem is 163 lines.

Known gaps
- Preview GameObject pooling/materials/placement still live in RoadBuildSystem until RoadPreviewSystem.
- Runtime-city callers still go through RoadBuildSystem compatibility methods until RoadRuntimeGenerationSystem and bridge migration steps.
- Unity batch log still includes unrelated licensing noise, but compilation and the focused architecture batch passed.

Cross-lane impacts
- No API break expected for runtime city, building, or selection lanes.
- Future RoadBuildInputSystem and RoadPreviewSystem can call RoadPathPlanningSystem directly instead of reaching into RoadBuildSystem.

Next recommended task
Step 7: create RoadFootprintQuerySystem for road footprint masks, road-world footprint visitors, footprint kind detection, and bounds transform helpers.

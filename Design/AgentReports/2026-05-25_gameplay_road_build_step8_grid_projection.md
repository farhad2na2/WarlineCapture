# WarlineCapture Handoff

Lane
Gameplay

Task
RoadBuildSystem refactor step 8: extract ECS road projection and road grid-query ownership into RoadGridProjectionSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs
- Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-25_gameplay_road_build_step8_grid_projection.md

Contracts touched
- RoadBuild refactor roadmap step 8 marked complete.
- Gameplay architecture test batch now requires RoadGridProjectionSystem to own EntityQuery caching, road buffer lookup, road/sidewalk/dirt projection writes, clear projection, deferred road ECS sync, and grid-data lookup.
- RoadBuildSystem is guarded against re-owning projection EntityQuery fields, deferred sync state, road buffer lookup helpers, or GridRoad/GridRoadSidewalk/GridRoadDirt writes.

User-visible behavior
No intended behavior change. Road build strokes should still project to ECS road, sidewalk, and dirt buffers; road data clear/restore and runtime blocker cleanup should keep existing behavior.

Validation run
- git diff --check for touched RoadBuild step 8 files.
- RoadBuildSystem/RoadGridProjectionSystem line count check.
- Ownership token audit for projection query fields, deferred sync state, road buffer lookup helpers, and road buffer writes.
- Unity batchmode in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step8-architecture-rerun.log

Validation result
Passed on rerun. RoadBuildArchitectureValidation result=Passed methods=10. RoadBuildSystem is 3088 lines; RoadGridProjectionSystem is 287 lines. The first validation run compiled but failed a stale architecture assertion from step 7; the guard was corrected because footprint visitors now flow through RoadGridProjectionSystem.

Known gaps
- RoadBuildSystem still has wrapper methods for SyncRoadCellsToEcs, ClearRoadDataInEcs, and TryGetGridData until later caller migration/removal steps.
- Road visual variant parsing and chunk rendering still live in RoadBuildSystem until steps 9 and 10.
- Existing Unity batch log still includes unrelated licensing/Xcode noise, but the focused architecture batch passed.

Cross-lane impacts
- Building placement callers should see no API break because the TryGetGridData wrapper remains.
- Runtime blocker cleanup is now routed through RoadGridProjectionSystem using RoadFootprintQuerySystem footprints.

Next recommended task
Step 9: create RoadVisualVariantSystem for prefab variant cache, combined visual data, marker layout cache, mask normalization, variant lookup, and prefab-to-mask mapping.

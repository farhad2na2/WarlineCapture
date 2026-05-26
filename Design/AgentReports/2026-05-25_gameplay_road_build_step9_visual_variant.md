Lane
Gameplay

Task
RoadBuildSystem refactor step 9: extract road visual variant/cache ownership into RoadVisualVariantSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs
- Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-25_gameplay_road_build_step9_visual_variant.md

Contracts touched
- RoadBuild roadmap step 9 is marked complete.
- RoadBuild architecture validation now requires variant data, connector marker data, marker layout data, prefab mapping, visual cache construction/disposal, variant lookup, autobahn mask normalization, and axis/direction mask helpers to live in RoadVisualVariantSystem.
- RoadBuildSystem is guarded against owning the old visual variant cache dictionaries, nested variant/marker types, marker parsing, combined visual data construction, and variant mask algorithms.
- Road footprint consumer validation now includes RoadVisualVariantSystem because visual cache construction still consumes footprint marker classification and transformed bounds.

User-visible behavior
- No intended gameplay or visual behavior change.
- Road placement visuals should continue to use the same prefabs, marker parsing, rotations, scales, and footprint data as before.

Validation run
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs Design/Architecture/road_build_system_refactor_roadmap.md`
- `wc -l Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs`
- `rg` ownership checks for RoadVisualVariantSystem and roadmap step 9.
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step9-architecture-rerun.log`

Validation result
- Passed.
- Unity log: `[RoadBuildArchitectureValidation] result=Passed methods=11`.
- RoadBuildSystem line count after step 9: 2764.
- RoadVisualVariantSystem line count after extraction: 425.

Known gaps
- RoadBuildSystem still owns chunk rendering, preview object pooling, special road object placement, and debug straight road placement.
- RoadBuildSystem still has temporary read-through properties for visual data, marker layouts, and autobahn connector marker data until later visual extraction steps remove those consumers.

Cross-lane impacts
- None expected.
- Validation workspace sync was required because the first step 9 Unity run used a stale architecture guard copy.

Next recommended task
RoadBuild step 10: create RoadChunkVisualSystem for chunk mesh rebuild/render ownership.

Lane
Gameplay

Task
RoadBuildSystem refactor step 10: extract normal road chunk rendering ownership into RoadChunkVisualSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadChunkVisualSystem.cs
- Assets/Game/Scripts/Systems/RoadChunkVisualSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-25_gameplay_road_build_step10_chunk_visual.md

Contracts touched
- RoadBuild roadmap step 10 is marked complete.
- RoadBuild architecture validation now requires chunk membership, dirty chunk queues, chunk mesh build/rebuild/dispose, chunk mesh lifetime, and normal road placement transform math to live in RoadChunkVisualSystem.
- RoadBuildSystem is guarded against re-owning chunk render dictionaries, dirty chunk state, chunk mesh combining, chunk coordinate calculation, or RoadChunk GameObject creation.

User-visible behavior
- No intended gameplay or visual behavior change.
- Normal road chunks should continue to rebuild with the same mesh/material grouping, transform placement, and special-road cell exclusion behavior.

Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadChunkVisualSystem.cs Assets/Game/Scripts/Systems/RoadChunkVisualSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md`
- `wc -l Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadChunkVisualSystem.cs`
- `rg` ownership check for chunk-rendering tokens in RoadBuildSystem.
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step10-architecture.log`

Validation result
- Passed.
- Unity log: `[RoadBuildArchitectureValidation] result=Passed methods=12`.
- RoadBuildSystem line count after step 10: 2547.
- RoadChunkVisualSystem line count after extraction: 290.

Known gaps
- RoadBuildSystem still owns preview object pooling and preview path visuals until step 11.
- RoadBuildSystem still owns autobahn/special-road object placement, marker alignment, connector visuals, and debug straight visuals until step 12.
- RoadBuildSystem still remains a broad shell with build-session, input, delete prompt, runtime-city road generation, and legacy building compatibility responsibilities.

Cross-lane impacts
- None expected.
- Runtime city and building systems continue to use the existing RoadBuildSystem public surface for now.

Next recommended task
RoadBuild step 11: create RoadPreviewSystem for preview object pooling, preview rebuild, preview alpha/material setup, and preview cleanup.

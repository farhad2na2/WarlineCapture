Lane
Gameplay

Task
RoadBuildSystem refactor step 12: extract autobahn/special-road visual ownership into RoadSpecialVisualSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs
- Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step12_special_visual.md

Contracts touched
- RoadBuild roadmap step 12 is marked complete.
- RoadBuild architecture validation now requires special-road object registries, debug straight-road object ownership, autobahn/connector rebuilds, marker-to-marker alignment, connector road-cell lookup, connector marker logging, standalone straight chains, standalone chain end lookup, debug city-road network creation, and connector variant selection to live in RoadSpecialVisualSystem.
- RoadBuildSystem is guarded against re-owning special-road visual registries, marker alignment helpers, connector variant helpers, special-road object creation/destruction, and debug straight-road branch creation.

User-visible behavior
- No intended gameplay or visual behavior change.
- Autobahn connectors, autobahn pieces, standalone debug straight chains, and runtime-city connector helper behavior should continue through the same public RoadBuildSystem surface.

Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md`
- `wc -l Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs`
- `rg` ownership check for special-road visual tokens in RoadBuildSystem.
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step12-architecture-rerun.log`

Validation result
- Passed.
- Unity log: `[RoadBuildArchitectureValidation] result=Passed methods=14`.
- RoadBuildSystem line count after step 12: 1704.
- RoadSpecialVisualSystem line count after extraction: 780.

Known gaps
- RoadBuildSystem still owns build-session state, pointer input flow, delete prompt, runtime-city road generation public API, and legacy building compatibility.
- RoadSpecialVisualSystem exposes the special-road object registry so existing footprint queries keep working until later query callers move off the shell.

Cross-lane impacts
- Runtime city bridge callers keep using the existing RoadBuildSystem public API; internals now route to RoadSpecialVisualSystem.
- Building footprint queries should see the same special-road object dictionary through the existing footprint context.

Next recommended task
RoadBuild step 13: create RoadBuildSessionSystem for build-mode activation, road session begin/confirm/cancel, delete-road prompt state handoff, session snapshot handoff, and minimap dirty event publication.

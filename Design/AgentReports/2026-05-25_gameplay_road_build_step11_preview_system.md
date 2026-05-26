Lane
Gameplay

Task
RoadBuildSystem refactor step 11: extract road preview visual ownership into RoadPreviewSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadPreviewSystem.cs
- Assets/Game/Scripts/Systems/RoadPreviewSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-25_gameplay_road_build_step11_preview_system.md

Contracts touched
- RoadBuild roadmap step 11 is marked complete.
- RoadBuild architecture validation now requires road preview object pools, preview object creation/release, preview rebuild loops, preview material alpha setup, and preview cleanup to live in RoadPreviewSystem.
- RoadBuildSystem is guarded against re-owning road preview GameObject lists, preview pools, preview object type maps, preview rebuild, preview object creation/release, and preview material setup.
- The RoadPathPlanningSystem contract now validates preview-plan consumers across RoadBuildSystem and RoadPreviewSystem, because step 11 moved preview planning calls out of the shell.

User-visible behavior
- No intended gameplay or visual behavior change.
- Road drag previews should continue to use the same path planning, masks, variant lookup, placement transform, names, and alpha material behavior.

Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadPreviewSystem.cs Assets/Game/Scripts/Systems/RoadPreviewSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md`
- `wc -l Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadPreviewSystem.cs`
- `rg` ownership check for road preview tokens in RoadBuildSystem.
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step11-architecture-rerun.log`

Validation result
- Passed.
- Unity log: `[RoadBuildArchitectureValidation] result=Passed methods=13`.
- RoadBuildSystem line count after step 11: 2409.
- RoadPreviewSystem line count after extraction: 276.

Known gaps
- RoadBuildSystem still owns autobahn/special-road object placement, marker alignment, connector visuals, and debug straight visuals until step 12.
- RoadBuildSystem still owns build-session/input/delete prompt, runtime-city road generation, and legacy building compatibility responsibilities.

Cross-lane impacts
- None expected.
- Runtime city and building systems continue to use the existing RoadBuildSystem public surface for now.

Next recommended task
RoadBuild step 12: create RoadSpecialVisualSystem for autobahn/special-road object creation, marker alignment, connector visuals, standalone debug straight visuals, and connector marker logging.

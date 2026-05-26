Lane
Gameplay

Task
RoadBuildSystem refactor step 13: extract road build session lifecycle into RoadBuildSessionSystem.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildSessionSystem.cs
- Assets/Game/Scripts/Systems/RoadBuildSessionSystem.cs.meta
- Assets/Game/Scripts/Systems/RoadMinimapEventSystem.cs
- Assets/Game/Scripts/Systems/RoadMinimapEventSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step13_session_system.md

Contracts touched
- RoadBuild roadmap step 13 is marked complete.
- RoadBuild architecture validation now requires road build session state, active tool mode, road session snapshot storage, delete-prompt state, build-click skip frames, road/soldier-base build-mode activation, confirm/cancel session commands, exit-build-mode command flow, and delete-prompt mutation to live in RoadBuildSessionSystem.
- Road minimap invalidation now has a dedicated RoadMinimapEventSystem boundary; RoadBuildSystem is guarded against directly invoking `MainMenuPlayUI.NotifyStaticMinimapChanged`.
- RoadBuildSystem is guarded against reintroducing old direct session fields: `_pendingDeleteStrokeId`, `_pendingDeleteMessage`, `_skipBuildClickFrames`, `_activeBuildTool`, and `_roadBuildSessionSnapshot`.

User-visible behavior
- No intended gameplay behavior change.
- Road build activation, confirm/cancel, delete-road prompt behavior, exit build mode, and minimap refresh should continue through the same public RoadBuildSystem API.

Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadBuildSessionSystem.cs Assets/Game/Scripts/Systems/RoadBuildSessionSystem.cs.meta Assets/Game/Scripts/Systems/RoadMinimapEventSystem.cs Assets/Game/Scripts/Systems/RoadMinimapEventSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md`
- `wc -l Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/RoadBuildSessionSystem.cs Assets/Game/Scripts/Systems/RoadMinimapEventSystem.cs`
- `rg` ownership check for old direct session fields and direct minimap notification in RoadBuildSystem.
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step13-architecture.log`

Validation result
- Passed.
- Unity log: `[RoadBuildArchitectureValidation] result=Passed methods=15`.
- RoadBuildSystem line count after step 13: 1684.
- RoadBuildSessionSystem line count: 196.
- RoadMinimapEventSystem line count: 31.

Known gaps
- RoadBuildSystem still owns pointer input flow, road delete modal drawing, runtime-city road generation public API, and legacy building compatibility.
- `SetBuildMode(bool)` remains as temporary static compatibility until RoadBuildCommandSystem.

Cross-lane impacts
- Runtime city/building callers keep using the existing RoadBuildSystem public API.
- Static minimap refresh now routes through RoadMinimapEventSystem.

Next recommended task
RoadBuild step 14: create RoadBuildInputSystem for pointer-state processing, pointer-over-UI checks, pressed/released/drag handling, drag-axis updates, pending start cell, and clicked-road delete selection.

# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
RoadBuildSystem refactor step 3: create `RoadBuildConfigSystem` and move `RoadBuildSystemConfig` projection out of the broad shell.

## Files changed
- `Assets/Game/Scripts/Systems/RoadBuildConfigSystem.cs`
- `Assets/Game/Scripts/Systems/RoadBuildConfigSystem.cs.meta`
- `Assets/Game/Scripts/Systems/RoadBuildSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/road_build_system_refactor_roadmap.md`
- `Design/AgentReports/2026-05-25_gameplay_road_build_step3_config_system.md`

## Contracts touched
- Added `RoadBuildConfigSystem` with immutable `Snapshot` projection from `RoadBuildSystemConfig`.
- `RoadBuildSystem` now calls `RoadBuildConfigSystem.TryCreateSnapshot` and applies the snapshot to existing compatibility fields.
- Added architecture guard `RoadBuildConfigProjectionMustLiveInRoadBuildConfigSystem`.
- Marked roadmap step 3 complete.

## User-visible behavior
No intended gameplay behavior change. Existing road prefabs, grid settings, preview alpha, soldier-base compatibility fields, and placement colors are still applied from the same config asset.

## Validation run
- `git diff --check --` on touched files.
- Unity batchmode in `WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation`.

## Validation result
- Diff check passed.
- RoadBuild architecture validation passed: `[RoadBuildArchitectureValidation] result=Passed methods=5`.
- `RoadBuildSystem.cs` is now 4040 lines, one line below the original 4041-line baseline.

## Known gaps
- `RoadBuildSystem` still applies the config snapshot into its own private compatibility fields. Later steps should move ownership of those fields into the extracted systems.
- Serialized `RoadBuildSystemConfig` naming remains documented compatibility debt.

## Cross-lane impacts
- Architecture lane: RoadBuild config projection now has an explicit owner and guard.
- Runtime city/building lanes: no behavior change expected; they still receive the same configured road data through existing road APIs.

## Next recommended task
Step 4: create `RoadRuntimeRootSystem` and move runtime road root creation/disposal out of `RoadBuildSystem`.

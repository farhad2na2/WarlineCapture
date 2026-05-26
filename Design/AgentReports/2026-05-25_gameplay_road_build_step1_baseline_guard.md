# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
RoadBuildSystem refactor step 1: add roadmap and baseline architecture guard.

## Files changed
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/road_build_system_refactor_roadmap.md`
- `Design/AgentReports/2026-05-25_gameplay_road_build_step1_baseline_guard.md`

## Contracts touched
- Added the dedicated `RoadBuildSystem` refactor roadmap with 30 tracked steps.
- Added road-build ownership contract wording for read model, config, roots, graph, path planning, footprint query, ECS projection, visuals, input/session, commands, legacy building responsibility, runtime-city road generation, and serialized config naming debt.
- Added focused architecture validation entry point `GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation`.
- Added baseline guards for roadmap presence, current shell size, broad shell responsibilities, static command spread, singleton access, and construction spread.

## User-visible behavior
No intended gameplay behavior change. This was a contract/test/documentation step only.

## Validation run
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/road_build_system_refactor_roadmap.md`
- Unity batchmode in `WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation`

## Validation result
- Diff check passed.
- RoadBuild architecture validation passed: `[RoadBuildArchitectureValidation] result=Passed methods=3`.

## Known gaps
- `RoadBuildSystem.cs` remains the broad temporary shell at 4041 lines until later extraction steps.
- `RoadBuildSystem.SetBuildMode` remains as documented static compatibility debt until `RoadBuildCommandSystem` replaces it.
- `RoadBuildSystemConfig` naming remains serialized data compatibility debt until a separate asset migration.

## Cross-lane impacts
- Architecture lane: new roadmap and guards prevent the road refactor from drifting or adding new static/singleton debt.
- Runtime city lane: later steps must preserve `RuntimeCityRoadBuildBridgeSystem` behavior while migrating it off the broad road shell.
- Building lane: later steps will move legacy building-placement responsibility out of road build and into building gameplay/interaction boundaries.

## Next recommended task
Step 2: create `RoadBuildReadModelSystem` and move read-only road interaction state toward that boundary without changing behavior.

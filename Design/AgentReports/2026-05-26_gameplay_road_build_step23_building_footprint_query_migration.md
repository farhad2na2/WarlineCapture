Lane: Gameplay

Task: RoadBuildSystem refactor step 23 - migrate BuildingGameplaySystem road queries to RoadFootprintQuerySystem.

Files changed:
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step23_building_footprint_query_migration.md

Contracts touched:
- BuildingGameplaySystem placement validation now depends on RoadFootprintQuerySystem plus RoadFootprintQuerySystem.Context.
- BuildingGameplaySystem must not store or query through _roadBuildController.
- RoadBuildSystem exposes the narrow road footprint query boundary for composition while the broader shell is still being retired.

User-visible behavior:
- No intended gameplay behavior change.
- Building placement validation still rejects road footprint overlaps through the same road footprint query logic.

Validation run:
- git diff --check for RoadBuild step 23 files.
- Local forbidden-token scan for _roadBuildController and direct RoadBuildSystem road query calls in BuildingGameplaySystem.
- Unity RoadBuild architecture batch:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step23-architecture.log

Validation result:
- Passed.
- Unity log: [RoadBuildArchitectureValidation] result=Passed methods=25

Known gaps:
- BuildingGameplaySystem keeps a compatibility Init overload that accepts RoadBuildSystem only to extract RoadFootprintQuerySystem and context for older tests/callers.
- RoadBuildSystem still exposes HasRoadInFootprint and FillRoadFootprintMask wrappers until remaining callers and tests migrate.
- RoadBuildSystem is still passed into some startup paths for non-footprint responsibilities.

Cross-lane impacts:
- Building gameplay validation is now wired through the narrow road footprint boundary.
- No runtime-city, art, or UI behavior was changed.

Next recommended task:
- Step 24: Migrate selection/camera/menu references off RoadBuildSystem using RoadBuildReadModelSystem, RoadBuildCommandSystem, and narrow update systems.

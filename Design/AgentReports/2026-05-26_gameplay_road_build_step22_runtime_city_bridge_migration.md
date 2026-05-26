Lane: Gameplay

Task: RoadBuildSystem refactor step 22 - migrate RuntimeCityRoadBuildBridgeSystem to RoadRuntimeGenerationSystem.

Files changed:
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityRoadBuildBridgeSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs
- Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step22_runtime_city_bridge_migration.md

Contracts touched:
- RuntimeCityRoadBuildBridgeSystem now stores RoadRuntimeGenerationSystem plus RoadRuntimeGenerationSystem.Context.
- RuntimeCityRoadBuildBridgeSystem must not reference RoadBuildSystem.
- RuntimeCityCompositionSystem configures the road-generation boundary instead of a broad RoadBuildSystem.
- Runtime city startup readiness uses HasRoadRuntimeGenerationSystem.

User-visible behavior:
- No intended gameplay behavior change.
- Runtime city road creation still calls the same bridge methods, now routed through RoadRuntimeGenerationSystem.

Validation run:
- git diff --check for RoadBuild step 22 files.
- Local forbidden-token scan for RoadBuildSystem references in RuntimeCityRoadBuildBridgeSystem, RuntimeCityCompositionSystem, and RuntimeCityStartupSystem.
- Unity RoadBuild architecture batch:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step22-architecture.log
- Unity RuntimeCity architecture batch:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step22-runtimecity-architecture.log

Validation result:
- Passed.
- Unity log: [RoadBuildArchitectureValidation] result=Passed methods=24
- Unity log: [RuntimeCityArchitectureValidation] result=Passed methods=28

Known gaps:
- RoadBuildSystem still exposes road footprint query wrappers used by BuildingGameplaySystem.
- RoadBuildSystem is still passed into feature startup for other road/build dependencies until later roadmap steps.

Cross-lane impacts:
- Runtime city no longer depends on RoadBuildSystem for road generation bridge ownership.
- Building gameplay road footprint query migration remains for step 23.

Next recommended task:
- Step 23: Migrate BuildingGameplaySystem road queries from RoadBuildSystem wrappers to RoadFootprintQuerySystem.

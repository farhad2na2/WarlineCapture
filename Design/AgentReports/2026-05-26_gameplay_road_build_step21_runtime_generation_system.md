Lane: Gameplay

Task: RoadBuildSystem refactor step 21 - create RoadRuntimeGenerationSystem.

Files changed:
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/RoadRuntimeGenerationSystem.cs
- Assets/Game/Scripts/Systems/RoadRuntimeGenerationSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step21_runtime_generation_system.md

Contracts touched:
- Added RoadBuild architecture contract coverage for step 21.
- Runtime-city-facing road generation commands now belong to RoadRuntimeGenerationSystem.
- RoadBuildSystem must not own runtime-city path copy/validation or standalone/special runtime road generation command wrappers.

User-visible behavior:
- No intended gameplay behavior change.
- Runtime-city road generation still goes through existing RoadBuildSystem wrappers for now, but those wrappers delegate to RoadRuntimeGenerationSystem.

Validation run:
- git diff --check for RoadBuild step 21 files.
- Local forbidden-token scan for runtime road path copy/validation and special standalone generation calls in RoadBuildSystem.
- Unity batch validation in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step21-architecture.log

Validation result:
- Passed.
- Unity log: [RoadBuildArchitectureValidation] result=Passed methods=23
- RoadBuildSystem line count after step 21: 1327.

Known gaps:
- RuntimeCityRoadBuildBridgeSystem still configures RoadBuildSystem directly.
- Step 22 should migrate the bridge to RoadRuntimeGenerationSystem plus required context/read helpers.
- RoadBuildSystem remains a temporary broad shell until later composition and caller migration steps.

Cross-lane impacts:
- Runtime city lane has a new road-generation boundary to migrate onto next.
- No scene, art, UI, or building behavior was changed.

Next recommended task:
- Step 22: Migrate RuntimeCityRoadBuildBridgeSystem from RoadBuildSystem to RoadRuntimeGenerationSystem.
